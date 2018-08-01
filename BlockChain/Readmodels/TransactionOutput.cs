using System.Linq;
using System.Text;
using SecurityDriven.Inferno.Hash;

namespace BlockChain.Readmodels
{
    /// <summary>
    /// Output transactions
    /// </summary>
    public class TransactionOutput
    {
        /// <summary>
        /// Construct the Output
        /// </summary>
        /// <param name="recipient">Reciepient of the transaction</param>
        /// <param name="value">Value of the transaction</param>
        /// <param name="parentTransactionId">Transaction that contains this output</param>
        public TransactionOutput(byte[] recipient, double value, string parentTransactionId)
        {
            Recipient = recipient;
            Value = value;
            ParentTransactionId = parentTransactionId;
            using (var shaer = HashFactories.SHA256.Invoke())
            {
                var dataToHash = $"{Recipient}{Value}{ParentTransactionId}";
                var hashBytes = shaer.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                Id = string.Join("", hashBytes.Select(x => x.ToString("x2")));
            }
        }

        /// <summary>
        /// Transactions Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Recipient of the transaction
        /// </summary>
        public byte[] Recipient { get; set; }

        /// <summary>
        /// Value of the transaction
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Parent that contains the transaction
        /// </summary>
        public string ParentTransactionId { get; set; }

        /// <summary>
        /// Check if the transaction belongs to a specific public key
        /// </summary>
        /// <param name="publicKey">Public key to check</param>
        /// <returns>Indicate if output belongs to the key</returns>
        public bool IsMine(byte[] publicKey)
        {
            return Recipient.SequenceEqual(publicKey);
        }
    }
}