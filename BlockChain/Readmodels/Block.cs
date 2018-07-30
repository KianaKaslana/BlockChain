using System;
using BlockChain.ExtensionMethods;

namespace BlockChain.Readmodels
{
    /// <summary>
    /// Blockchain block
    /// </summary>
    public class Block
    {
        /// <summary>
        /// Construct a block with data
        /// </summary>
        /// <param name="index">Index of the block</param>
        /// <param name="previousHash">Hash of the previous block</param>
        /// <param name="nextHash">Hash of the next block</param>
        /// <param name="timeStamp">TimeStamp when block was created</param>
        /// <param name="data">Data contained in the block</param>
        /// <param name="hash">Hash of blocks data</param>
        public Block(int index, string previousHash, string nextHash, DateTime timeStamp, byte[] data, string hash)
        {
            Index = index;
            PreviousHash = previousHash;
            NextHash = nextHash;
            TimeStamp = timeStamp;
            Data = data;

            if (string.IsNullOrEmpty(hash))
            {
                Hash = this.CalculateHash();
            }
            else
            {
                var expectedHash = this.CalculateHash();
                if (hash == expectedHash)
                {
                    Hash = hash;
                }
                else
                {
                    throw new InvalidOperationException("Block hash does not match computed hash");       
                }
            }
        }

        /// <summary>
        /// Index of the Block
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Hash of the previous Block
        /// </summary>
        public string PreviousHash { get; }

        /// <summary>
        /// Hash of the next block
        /// </summary>
        public string NextHash { get; private set; }

        /// <summary>
        /// TimeStamp indicating when the Block was generated
        /// </summary>
        public DateTime TimeStamp { get; }

        /// <summary>
        /// Data stored by the block
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// SHA256 Hash of the block covering the Index, previous hash, timestamp and data
        /// </summary>
        public string Hash { get; }

        /// <summary>
        /// Set the next block
        /// </summary>
        /// <param name="nextBlock">Block that comes after this block</param>
        public void SetNextBlock(Block nextBlock)
        {
            NextHash = nextBlock.Hash;
            if (!nextBlock.CheckBlockValidity(this))
            {
                NextHash = null;
            }
        }
    }
}