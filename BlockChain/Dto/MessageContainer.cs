using BlockChain.Enumerations;

namespace BlockChain.Dto
{
    /// <summary>
    /// Container for sending requests to peers
    /// </summary>
    public class MessageContainer
    {
        /// <summary>
        /// The type of the message received
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// The payload as a JSON object
        /// </summary>
        public string JsonPayload { get; set; }
    }
}