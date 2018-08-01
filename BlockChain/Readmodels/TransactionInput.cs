namespace BlockChain.Readmodels
{
    /// <summary>
    /// Inputs to a transaction
    /// </summary>
    public class TransactionInput
    {
        /// <summary>
        /// Construct with OutputId
        /// </summary>
        /// <param name="transactionOutputId">The output Id</param>
        public TransactionInput(string transactionOutputId)
        {
            TransactionOutputId = transactionOutputId;
        }

        /// <summary>
        /// Reference to TransactionOutputs
        /// </summary>
        public string TransactionOutputId { get; set; }

        /// <summary>
        /// Unspent transaction output
        /// </summary>
        public TransactionOutput TransactionOutput { get; set; }
    }
}