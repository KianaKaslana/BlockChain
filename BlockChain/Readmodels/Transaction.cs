using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlockChain.ExtensionMethods;
using SecurityDriven.Inferno.Hash;
using Serilog;

namespace BlockChain.Readmodels
{
    public class Transaction
    {
        /// <summary>
        /// Construct a complete transaction
        /// </summary>
        /// <param name="sender">Sender of the transaction</param>
        /// <param name="receiver">Recipient of the transaction</param>
        /// <param name="value">Value of the transaction</param>
        /// <param name="inputs">Inputs to the transaction</param>
        public Transaction(byte[] sender, byte[] receiver, double value, List<TransactionInput> inputs)
        {
            Sender = sender;
            Receiver = receiver;
            Value = value;
            Inputs = inputs;
            _sequence = 0;
            Outputs = new List<TransactionOutput>();

            CalculateHash();
        }

        /// <summary>
        /// Id of the transaction (Hash of Sender, Recipient, Value and _sequence)
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Address of the Sender of the transaction
        /// </summary>
        public byte[] Sender { get; set; }

        /// <summary>
        /// Address of the Receiver of the transaction
        /// </summary>
        public byte[] Receiver { get; set; }

        /// <summary>
        /// Value of the transaction
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Signature of the Sender to prove authenticity
        /// </summary>
        public byte[] Signature { get; set; }

        /// <summary>
        /// Inputs to the transaction
        /// </summary>
        public List<TransactionInput> Inputs { get; set; }

        /// <summary>
        /// Output to the transaction
        /// </summary>
        public List<TransactionOutput> Outputs { get; set; }

        /// <summary>
        /// Process the transaction if possible
        /// </summary>
        /// <returns>Indicate if the transaction could be processed</returns>
        public bool ProcessTransaction(BlockManager blockManager)
        {
            var logger = Log.Logger.ForContext<Transaction>();
            foreach (var input in Inputs)
            {
                // TODO - Ensure inputs are not spent...
            }

            if (GetInputTotal() < blockManager.MinumumTransactionValue)
            {
                logger.Error("Could not process transaction - Requested amount is less than the minimum transaction value of {MinimumTransactionValue}", blockManager.MinumumTransactionValue);
                return false;
            }

            var leftOver = GetInputTotal() - Value;
            CalculateHash();
            Outputs.Add(new TransactionOutput(Receiver, Value, TransactionId));
            Outputs.Add(new TransactionOutput(Sender, leftOver, TransactionId));

            // TODO - Add outputs to unspent list

            // TODO - Remove inputs from unspent list

            return true;
        }

        /// <summary>
        /// Get the total value of inputs
        /// </summary>
        /// <returns>Total value</returns>
        private double GetInputTotal()
        {
            return Inputs.Select(x => x.TransactionOutput.Value).Sum();
        }

        /// <summary>
        /// Sign the transaction to prevent tampering
        /// </summary>
        /// <param name="privateKey">Private key to use for signing</param>
        public void SignTransaction(byte[] privateKey)
        {
            var dataToSign = GetDataToSign();
            Signature = dataToSign.Sign(privateKey);
        }

        /// <summary>
        /// Verify the signature
        /// </summary>
        /// <returns>Indicate if signature is valid</returns>
        public bool VerifySignature()
        {
            var dataToVerify = GetDataToSign();
            return dataToVerify.VerifySignature(Signature, Sender);
        }

        /// <summary>
        /// Retrieve data that we want to have signed
        /// </summary>
        /// <returns></returns>
        private string GetDataToSign()
        {
            // TODO - We probably want to sign the inputs as well
            return $"{Encoding.UTF8.GetString(Sender)}{Encoding.UTF8.GetString(Receiver)}{Value}";
        }

        // TODO - We need a method that can be used to validate the transaction

        /// <summary>
        /// Calcualte the hash for the transaction
        /// </summary>
        /// <returns></returns>
        private void CalculateHash()
        {
            _sequence++;
            using (var hasher = HashFactories.SHA256.Invoke())
            {
                var hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes($"{Sender}{Receiver}{Value}{_sequence}"));
                TransactionId = string.Join("", hashBytes.Select(x => x.ToString("x2")));
            }
        }

        /// <summary>
        /// Rough number of how many transactions has been generated
        /// </summary>
        private static int _sequence;

    }
}