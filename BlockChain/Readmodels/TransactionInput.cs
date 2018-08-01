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
        /// <param name="output">Output transaction</param>
        public TransactionInput(string transactionOutputId, TransactionOutput output)
        {
            TransactionOutputId = transactionOutputId;
            TransactionOutput = output;
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