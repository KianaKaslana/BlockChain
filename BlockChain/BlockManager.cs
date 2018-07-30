using System;
using System.Collections.Generic;
using System.Linq;
using BlockChain.ExtensionMethods;
using BlockChain.Readmodels;
using Serilog;
using Serilog.Context;

namespace BlockChain
{
    /// <summary>
    /// Manage the Blocks on the chain
    /// </summary>
    public class BlockManager
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="genesisBlock">The first block in the chain</param>
        public BlockManager(Block genesisBlock)
        {
            _blockChain = new List<Block>
            {
                genesisBlock
            };
            _logger = Log.Logger.ForContext<BlockManager>();
        }

        /// <summary>
        /// Get the last block on the chain
        /// </summary>
        /// <returns>Last Block on the chain</returns>
        public Block GetLastBlock()
        {
            return _blockChain.First(x => string.IsNullOrEmpty(x.NextHash));
        }

        /// <summary>
        /// Generate a new block on the chain
        /// </summary>
        /// <param name="data">Data to add to the block</param>
        /// <returns>Generated block</returns>
        public Block GenerateBlock(byte[] data)
        {
            var lastBlock = GetLastBlock();
            var newBlock = new Block(lastBlock.Index + 1, lastBlock.Hash, null, DateTime.Now, data, string.Empty);
            lastBlock.SetNextBlock(newBlock);
            _blockChain.Add(newBlock);
            using (LogContext.PushProperty("Block", newBlock, true))
            {
                _logger.Information("New block was generated with {Hash}. Chain now contains {BlockCount} blocks", newBlock.Hash, _blockChain.Count);
            }

            return newBlock;
        }
        
        /// <summary>
        /// Replace the current chain with the new chain if the new chain is valid
        /// </summary>
        /// <param name="newChain">Chain to consider for replacement</param>
        public void ReplaceChain(List<Block> newChain)
        {
            if (newChain.IsValidChain() && newChain.Count > _blockChain.Count)
            {
                _logger.Information("Replaced current chain with {BlockCount} blocks with longer chain containing {LongerBlockCount} blocks", _blockChain.Count, newChain.Count);
                _blockChain = newChain;
            }
            else
            {
                _logger.Warning("Received chain was invalid - Current chain was not replaced");
            }
        }

        /// <summary>
        /// Get all Blocks in the BlockChain
        /// </summary>
        /// <returns>BlockChain</returns>
        public List<Block> GetBlocks()
        {
            return _blockChain.ToList();
        }

        /// <summary>
        /// Collection of all Blocks in the chain
        /// </summary>
        private List<Block> _blockChain;

        /// <summary>
        /// Serilog Logger instance
        /// </summary>
        private readonly ILogger _logger;
    }
}