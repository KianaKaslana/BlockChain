namespace BlockChain.Enumerations
{
    /// <summary>
    /// Enums to indicate the type of message that can be received via P2P
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Indicates that a new block has been mined
        /// </summary>
        NewBlockMined,

        /// <summary>
        /// Used to request the last known block in the chain from a peer
        /// </summary>
        RequestLastBlock,

        /// <summary>
        /// Response to <see cref="RequestLastBlock"/>
        /// </summary>
        LastKnownBlock,

        /// <summary>
        /// Used to request the full chain from a peer
        /// </summary>
        RequestFullChain,

        /// <summary>
        /// Response to <see cref="RequestFullChain"/>
        /// </summary>
        FullChain,

        /// <summary>
        /// Data for the next block that should be mined
        /// </summary>
        BlockToMine
    }
}