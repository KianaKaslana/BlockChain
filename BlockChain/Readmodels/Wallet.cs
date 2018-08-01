using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using SecurityDriven.Inferno.Extensions;
using Serilog;

namespace BlockChain.Readmodels
{
    /// <summary>
    /// Wallet to store user keypairs
    /// </summary>
    public class Wallet
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Wallet(BlockManager blockManager, string walletName)
        {
            PrivateKeyPath = $"{walletName}.key";
            _blockManager = blockManager;
            if (!LoadKeys())
            {
                GenerateKeys();
            }
        }

        /// <summary>
        /// The KeyPair for the wallet
        /// </summary>
        public CngKey KeyPair { get; private set; }

        /// <summary>
        /// Get the public key from the <see cref="KeyPair"/> as a <see cref="CngKeyBlobFormat.EccFullPublicBlob"/>
        /// </summary>
        public byte[] GetPublicKey { get; set; }

        /// <summary>
        /// Get the private key from <see cref="KeyPair"/> as a <see cref="CngKeyBlobFormat.EccFullPrivateBlob"/>
        /// </summary>
        public byte[] GetPrivateKey { get; set; }

        /// <summary>
        /// Returns the balance of the Wallet
        /// </summary>
        /// <returns></returns>
        public double GetBalance()
        {
            var unspentBlocks = _blockManager.GetUnspentOutputsForKey(GetPublicKey);
            return unspentBlocks.Sum(x => x.Value);
        }

        /// <summary>
        /// Create a transaction to send funds to an address
        /// </summary>
        /// <param name="recipientKey">Public key of the recipient</param>
        /// <param name="value">Value to transfer</param>
        /// <returns>Transaction that was created</returns>
        public Transaction SendFunds(byte[] recipientKey, double value)
        {
            var balance = GetBalance();
            if (balance < value)
            {
                Log.Logger.Error("Invalid transaction - Transfer value {Value} is higher than available balance {Balance}", value, balance);
                return null;
            }

            var valueOfOutputs = 0d;
            var inputsToTransaction = new List<TransactionInput>();
            foreach (var output in _blockManager.GetUnspentOutputsForKey(GetPublicKey).OrderBy(x => x.Value))
            {
                inputsToTransaction.Add(new TransactionInput(output.Id));
                valueOfOutputs += output.Value;

                if (valueOfOutputs >= value)
                {
                    break;
                }
            }

            var transaction = new Transaction(GetPublicKey, recipientKey, value, inputsToTransaction);
            transaction.SignTransaction(GetPrivateKey);

            return transaction;
        }

        /// <summary>
        /// Generate KeyPair
        /// </summary>
        private void GenerateKeys()
        {
            // Create and export key
            KeyPair = CngKeyExtensions.CreateNewDsaKey();
            // TODO - How do we secure the key when we save it to disk?
            GetPrivateKey = KeyPair.Export(CngKeyBlobFormat.EccFullPrivateBlob);
            GetPublicKey = KeyPair.Export(CngKeyBlobFormat.EccFullPublicBlob);
            using (var file = File.OpenWrite(PrivateKeyPath))
            {
                file.Write(GetPrivateKey);
            }
        }

        /// <summary>
        /// Load key material from disk
        /// </summary>
        private bool LoadKeys()
        {
            if (!File.Exists(PrivateKeyPath))
            {
                return false;
            }

            using (var file = File.OpenRead(PrivateKeyPath))
            {
                var privateKey = new byte[file.Length];
                file.Read(privateKey, 0, (int)file.Length);
                // Import the key
                KeyPair = CngKey.Import(privateKey, CngKeyBlobFormat.EccFullPrivateBlob);
                GetPrivateKey = privateKey;
                GetPublicKey = KeyPair.Export(CngKeyBlobFormat.EccFullPublicBlob);
            }

            return true;
        }

        /// <summary>
        /// File used to store the user's private wallet key
        /// </summary>
        private readonly string PrivateKeyPath; 

        /// <summary>
        /// BlockManager instance
        /// </summary>
        private readonly BlockManager _blockManager;
    }
}