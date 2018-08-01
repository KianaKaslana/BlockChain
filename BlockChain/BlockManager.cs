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
        public BlockManager(PeerToPeerController peerToPeerController)
        {
            _blockChain = new List<Block>();
            _pendingTransactions = new List<Transaction>();
            _logger = Log.Logger.ForContext<BlockManager>();

            _difficulty = 5;
            MinumumTransactionValue = 0.5;

            _chainPersistence = new ChainPersistence();
            _peerToPeerController = peerToPeerController;
            _peerToPeerController.SetBlockCheckFunction(FuncToCheckBlocks, GetLastBlock, GetBlocks, ReplaceChain, MineBlock);
            _peerToPeerController.BlockReceivedFromNetwork += PeerToPeerControllerOnBlockReceivedFromNetwork;
            _peerToPeerController.TransactionReceivedFromNetwork += PeerToPeerControllerOnTransactionReceivedFromNetwork;
        }

        /// <summary>
        /// Function that should be called to allocate mining reward
        /// </summary>
        public Func<Transaction> FuncToGenerateGenerationTransaction { get; set; }

        /// <summary>
        /// Occurs when a transaction was received from the network
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Received transaction</param>
        private void PeerToPeerControllerOnTransactionReceivedFromNetwork(object sender, Transaction e)
        {
            Task.Run(() => AddTransaction(e, false));
        }

        /// <summary>
        /// Minumum transaction allowed
        /// </summary>
        public double MinumumTransactionValue { get; }

        /// <summary>
        /// Attempt to load the
        /// </summary>
        public void SetupChain(Block genesisBlock)
        {
            var persistedChain = _chainPersistence.LoadChain();
            if (persistedChain == null)
            {
                _logger.Information("Starting new chain");
                lock (_blockChainLock)
                {
                    MineBlock(genesisBlock);
                }
            }
            else
            {
                ReplaceChain(persistedChain);
            }
        }

        /// <summary>
        /// Add a new transaction that should be processed
        /// </summary>
        /// <param name="transactionToAdd">The transaction that should be added</param>
        /// <param name="broadcastTransaction">Indicate if transaction should be broadcast to peers. Should be true unless the transaction was received from a peer</param>
        public void AddTransaction(Transaction transactionToAdd, bool broadcastTransaction)
        {
            lock (_pendingTransactionLock)
            {
                if (_pendingTransactions.Contains(transactionToAdd))
                {
                    _logger.Warning(
                        "Transaction {TransactionId} already exists in the pending transaction list - Discarding",
                        transactionToAdd.TransactionId);
                    return;
                }

                if (_currentlyMiningBlock?.Transactions.Contains(transactionToAdd) ?? false)
                {
                    _logger.Warning(
                        "Transaction {TransactionId} is in the block that is currently being mined - Discarding",
                        transactionToAdd.TransactionId);
                    return;
                }

                _pendingTransactions.Add(transactionToAdd);

                if (broadcastTransaction)
                {
                    Task.Run(() => _peerToPeerController.BroadcastTransactionAsync(transactionToAdd));
                }
            }
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

                    Task.Run(() => PersistChain());
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
        /// Mine a block
        /// </summary>
        /// <param name="block">The Block to mine</param>
        private void MineBlock(Block block)
        {
            try
            {
                _miningSemaphore.Wait();
                _currentlyMiningBlock = block;
                // TODO - Validate transactions
                _miningCancellationTokenSource = new CancellationTokenSource();

                _logger.Information("Mining new block...");
                block.Mine(_difficulty, _miningCancellationTokenSource.Token);

                //This is the genesis block
                if (block.PreviousHash == null
                    && block.NextHash == null)
                {
                    lock (_blockChainLock)
                    {
                        _blockChain.Add(block);
                        Task.Run(() => PersistChain());
                        var rewardTransaction = FuncToGenerateGenerationTransaction.Invoke();
                        Task.Run(() =>GenerateNewBlock(block, rewardTransaction));
                        return;
                    }
                }

                var lastBlock = GetLastBlock();
                if (block.CheckBlockValidity(lastBlock))
                {
                    lastBlock.SetNextBlock(block);

                    lock (_blockChainLock)
                    {
                        _blockChain.Add(block);
                        Task.Run(() => PersistChain());

                        using (LogContext.PushProperty("Block", block, true))
                        {
                            _logger.Information(
                                "New block was generated with {Hash}. Chain now contains {BlockCount} blocks",
                                block.Hash, _blockChain.Count);
                        }
                    }

                    Task.Run(() => _peerToPeerController.BroadcastNewBlockAsync(block));
                    var rewardTransaction = FuncToGenerateGenerationTransaction.Invoke();
                    Task.Run(() => GenerateNewBlock(block, rewardTransaction));
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
                _currentlyMiningBlock = null;
                _miningSemaphore.Release();
            }
        }

        /// <summary>
        /// Generates a new block to mine if there are transactions available
        /// </summary>
        private void GenerateNewBlock(Block lastBlock, Transaction rewardTransaction)
        {
            // TODO - Let peers know which transactions had been removed to prevent double mining them!!
            var transactionsToAppend = new List<Transaction>{ rewardTransaction };
            lock (_pendingTransactionLock)
            {
                transactionsToAppend.AddRange(_pendingTransactions.ToList());
                _pendingTransactions.Clear();
            }

            _logger.Information("Generating block with {PendingTransactions} pending transactions...", transactionsToAppend.Count);
            var newBlock = new Block(lastBlock.Index + 1, lastBlock.Hash, null, DateTime.UtcNow, transactionsToAppend, string.Empty, 1);
            Task.Run(() => MineBlock(newBlock));
            Task.Run(() => _peerToPeerController.BroadCastNextBlockToMineAsync(newBlock));
        }

        /// <summary>
        /// Replace the current chain with the new chain if the new chain is valid
        /// </summary>
        /// <param name="newChain">Chain to consider for replacement</param>
        public void ReplaceChain(List<Block> newChain)
        {
            var replaceEmptyChain = _blockChain == null;
            lock (_blockChainLock)
            {
                if (newChain.IsValidChain() && newChain.Count > (_blockChain?.Count ?? 0))
                {
                    _logger.Information(
                        "Replaced current chain with {BlockCount} blocks with longer chain containing {LongerBlockCount} blocks",
                        _blockChain?.Count ?? 0, newChain.Count);
                    _blockChain = newChain;

                    if (!replaceEmptyChain)
                    {
                        Task.Run(() => PersistChain());
                    }
                }
                else
                {
                    _logger.Warning("Received chain was invalid - Current chain was not replaced");
                }
            }
        }

        /// <summary>
        /// Gather input transactions for a specific public key
        /// </summary>
        /// <param name="publicKey">PublicKey for which inputs should be gathered</param>
        /// <returns>Unspent inputs</returns>
        public List<TransactionOutput> GetUnspentOutputsForKey(byte[] publicKey)
        {
            var currentBlocks = GetBlocks();

            var outputs = currentBlocks
                .SelectMany(x => x.Transactions)
                .SelectMany(x => x.Outputs)
                .Where(x => x.IsMine(publicKey));

            var spentInputs = currentBlocks
                .SelectMany(x => x.Transactions)
                .SelectMany(x => x.Inputs ?? new List<TransactionInput>())
                .Select(x => x.TransactionOutputId)
                .ToList();

            return outputs
                .Where(x => !spentInputs.Contains(x.Id))
                .ToList();
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
        /// Persist the chain
        /// </summary>
        private void PersistChain()
        {
            _chainPersistence.PersistChain(_blockChain);
        }

        /// <summary>
        /// List containing pending transactions
        /// </summary>
        private readonly List<Transaction> _pendingTransactions;

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
        
        /// <summary>
        /// ChainPersistence instance
        /// </summary>
        private readonly ChainPersistence _chainPersistence;

        /// <summary>
        /// Object used to lock the <see cref="_pendingTransactions"/> collection
        /// </summary>
        private readonly object _pendingTransactionLock = new object();

        /// <summary>
        /// The block that is currently being mined
        /// </summary>
        private Block _currentlyMiningBlock;
    }
}