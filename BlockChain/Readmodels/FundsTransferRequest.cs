namespace BlockChain.Readmodels
{
    /// <summary>
    /// Request to transfer funds between accounts
    /// </summary>
    public class FundsTransferRequest
    {
        /// <summary>
        /// Address of the user receiving the funds
        /// </summary>
        public string RecipientAddress { get; set; }

        /// <summary>
        /// The value that is to be transferred
        /// </summary>
        public double Value { get; set; }
    }
}