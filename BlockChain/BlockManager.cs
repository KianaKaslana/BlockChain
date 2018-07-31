using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        /// <param name="peerToPeerController">PeerController instance</param>
        /// <param name="genesisBlock">The first block in the chain</param>
        public BlockManager(PeerToPeerController peerToPeerController, Block genesisBlock)
        {
            _difficulty = 5;
            _peerToPeerController = peerToPeerController;
            _peerToPeerController.SetBlockCheckFunction(FuncToCheckBlocks, GetLastBlock, GetBlocks, ReplaceChain, MineBlock);
            _peerToPeerController.BlockReceivedFromNetwork += PeerToPeerControllerOnBlockReceivedFromNetwork;
            lock (_blockChainLock)
            {
                _blockChain = new List<Block>
                {
                    genesisBlock
                };
            }

            _logger = Log.Logger.ForContext<BlockManager>();
        }
        
        /// <summary>
        /// Function that can be invoked to check if a provided block is the last block in our chain
        /// </summary>
        /// <param name="arg">The block to check</param>
        /// <returns>Indicates if block is last block in our chain</returns>
        private bool FuncToCheckBlocks(Block arg)
        {
            Block lastBlock;
            lock (_blockChainLock)
            {
                lastBlock = _blockChain.Last();
            }

            return arg.Index == lastBlock.Index
                   && arg.Hash == lastBlock.Hash;
        }

        /// <summary>
        /// Occurs when a Block was received from a peer
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Received Block</param>
        private void PeerToPeerControllerOnBlockReceivedFromNetwork(object sender, Block e)
        {
            var lastBlock = GetLastBlock();
            if (e.CheckBlockValidity(lastBlock))
            {
                lock (_blockChainLock)
                {
                    lastBlock.SetNextBlock(e);
                    _blockChain.Add(e);
                }

                _miningCancellationTokenSource?.Cancel();
                _logger.Information("Added block {Hash} to the blockchain", e.Hash);
            }
            else
            {
                _logger.Warning("Received {Hash} which is not a valid block", e.Hash);
            }
        }

        /// <summary>
        /// Get the last block on the chain
        /// </summary>
        /// <returns>Last Block on the chain</returns>
        public Block GetLastBlock()
        {
            lock (_blockChainLock)
            {
                return _blockChain.First(x => string.IsNullOrEmpty(x.NextHash));
            }
        }

        /// <summary>
        /// Generate a new block on the chain
        /// </summary>
        /// <param name="data">Data to add to the block</param>
        /// <returns>Generated block</returns>
        public void GenerateBlock(byte[] data)
        {
            var lastBlock = GetLastBlock();
            var newBlock = new Block(lastBlock.Index + 1, lastBlock.Hash, null, DateTime.Now, data, string.Empty, 1);
            Task.Run(() => _peerToPeerController.BroadCastNextBlockToMineAsync(newBlock));
            Task.Run(() => MineBlock(newBlock));
        }

        /// <summary>
        /// Mine a block
        /// </summary>
        /// <param name="block">The Block to mine</param>
        private void MineBlock(Block block)
        {
            try
            {
                _miningSemaphore.Wait();
                _miningCancellationTokenSource = new CancellationTokenSource();

                _logger.Information("Mining new block...");
                block.Mine(_difficulty, _miningCancellationTokenSource.Token);

                var lastBlock = GetLastBlock();
                if (block.CheckBlockValidity(lastBlock))
                {
                    lastBlock.SetNextBlock(block);

                    lock (_blockChainLock)
                    {
                        _blockChain.Add(block);


                        using (LogContext.PushProperty("Block", block, true))
                        {
                            _logger.Information(
                                "New block was generated with {Hash}. Chain now contains {BlockCount} blocks",
                                block.Hash, _blockChain.Count);
                        }
                    }

                    Task.Run(() => _peerToPeerController.BroadcastNewBlockAsync(block));
                }
                else
                {
                    _logger.Warning("Block {Hash} that was mined is invalid - discarding", block.Hash);
                }
            }
            finally
            {
                _miningCancellationTokenSource.Dispose();
                _miningCancellationTokenSource = null;
                _miningSemaphore.Release();
            }
        }

        /// <summary>
        /// Replace the current chain with the new chain if the new chain is valid
        /// </summary>
        /// <param name="newChain">Chain to consider for replacement</param>
        public void ReplaceChain(List<Block> newChain)
        {
            lock (_blockChainLock)
            {
                if (newChain.IsValidChain() && newChain.Count > _blockChain.Count)
                {
                    _logger.Information(
                        "Replaced current chain with {BlockCount} blocks with longer chain containing {LongerBlockCount} blocks",
                        _blockChain.Count, newChain.Count);
                    _blockChain = newChain;
                }
                else
                {
                    _logger.Warning("Received chain was invalid - Current chain was not replaced");
                }
            }
        }

        /// <summary>
        /// Get all Blocks in the BlockChain
        /// </summary>
        /// <returns>BlockChain</returns>
        public List<Block> GetBlocks()
        {
            lock (_blockChainLock)
            {
                return _blockChain.ToList();
            }
        }

        /// <summary>
        /// Collection of all Blocks in the chain
        /// </summary>
        private List<Block> _blockChain;

        /// <summary>
        /// Serilog Logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// PeerToPeerController instance
        /// </summary>
        private readonly PeerToPeerController _peerToPeerController;

        /// <summary>
        /// Difficulty used for mining
        /// </summary>
        private readonly int _difficulty;

        /// <summary>
        /// Semaphore used to lock the mining method
        /// </summary>
        private readonly SemaphoreSlim _miningSemaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Lock used to lock access to <see cref="_blockChain"/>
        /// </summary>
        private readonly object _blockChainLock = new object();

        /// <summary>
        /// Token source used to cancel mining of a block
        /// </summary>
        private CancellationTokenSource _miningCancellationTokenSource;
    }
}