using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlockChain.Dto;
using BlockChain.Enumerations;
using BlockChain.Readmodels;
using Newtonsoft.Json;
using P2PNET.TransportLayer;
using P2PNET.TransportLayer.EventArgs;
using Serilog;

namespace BlockChain
{
    public class PeerToPeerController
    {
        /// <summary>
        /// Fired when a Block was received from a peer
        /// </summary>
        public event EventHandler<Block> BlockReceivedFromNetwork; 

        /// <summary>
        /// Default Constructor
        /// </summary>
        public PeerToPeerController(int port)
        {
            _logger = Log.Logger.ForContext<PeerToPeerController>();
            _transportManager = new TransportManager(port);
            _transportManager.PeerChange += TransportManagerOnPeerChange;
            _transportManager.MsgReceived += TransportManagerOnMsgReceived;
            Task.Run(RunServerAsync);
        }

        /// <summary>
        /// Handle new blocks mined that was broadcast by a peer
        /// </summary>
        /// <param name="message">Message that was broadcast</param>
        private void HandleNewMinedBlock(MessageContainer message)
        {
            var block = JsonConvert.DeserializeObject<Block>(message.JsonPayload);
            BlockReceivedFromNetwork?.Invoke(this, block);
        }

        /// <summary>
        /// Handle request for our current last block
        /// </summary>
        /// <param name="remoteIp">IP of the peer that requested the block</param>
        private void HandleLastBlockRequest(string remoteIp)
        {
            var lastBlock = _getLastBlockFunc.Invoke();
            var blockJson = JsonConvert.SerializeObject(lastBlock);
            var container = new MessageContainer
            {
                MessageType = MessageType.LastKnownBlock,
                JsonPayload = blockJson
            };

            var peerToRespondTo = _transportManager.KnownPeers.FirstOrDefault(x => x.IpAddress == remoteIp);
            peerToRespondTo?.SendMsgTCPAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(container)));
        }

        /// <summary>
        /// Handle the receipt of a block from a peer
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="remoteIp">IP of the peer the Block was received from</param>
        private void HandleLastBlockResponse(MessageContainer message, string remoteIp)
        {
            var receivedBlock = JsonConvert.DeserializeObject<Block>(message.JsonPayload);
            if (!_funcToCheckBlocks.Invoke(receivedBlock))
            {
                var fullChainRequest = new MessageContainer
                {
                    MessageType = MessageType.RequestFullChain
                };

                var fullRequestPeer = _transportManager.KnownPeers.FirstOrDefault(x => x.IpAddress == remoteIp);
                fullRequestPeer?.SendMsgTCPAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fullChainRequest)));
            }
            else
            {
                _logger.Information("Last block in chain matches {IpAddress} peer", remoteIp);
                _directRequestSemaphore.Release();
            }
        }

        /// <summary>
        /// Handle request for our full chain from a peer
        /// </summary>
        /// <param name="peerIp">IP of the peer</param>
        private void HandleRequestForFullChain(string peerIp)
        {
            var chain = _getFullChain.Invoke();
            var chainJson = JsonConvert.SerializeObject(chain);
            var chainContainer = new MessageContainer
            {
                MessageType = MessageType.FullChain,
                JsonPayload = chainJson
            };

            var chainPeer = _transportManager.KnownPeers.FirstOrDefault(x => x.IpAddress == peerIp);
            chainPeer?.SendMsgTCPAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(chainContainer)));
        }

        /// <summary>
        /// Handle the receipt of a full chain from a peer
        /// </summary>
        /// <param name="message">Message that was received</param>
        private void HandleReceiptOfFullChain(MessageContainer message)
        {
            var newChain = JsonConvert.DeserializeObject<List<Block>>(message.JsonPayload);
            _updateChainFunc.Invoke(newChain);
            _directRequestSemaphore.Release();
        }

        /// <summary>
        /// Occurs when a message was received from a peer
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void TransportManagerOnMsgReceived(object sender, MsgReceivedEventArgs e)
        {
          var dataString = Encoding.UTF8.GetString(e.Message);
            var obj = JsonConvert.DeserializeObject<MessageContainer>(dataString);
            _logger.Information("Received {MessageType} message from {IpAddress}", obj.MessageType, e.RemoteIp);

            Task.Run(() =>
            {
                switch (obj.MessageType)
                {
                    case MessageType.NewBlockMined:
                        HandleNewMinedBlock(obj);
                        break;
                    case MessageType.RequestLastBlock:
                        HandleLastBlockRequest(e.RemoteIp);
                        break;
                    case MessageType.LastKnownBlock:
                        HandleLastBlockResponse(obj, e.RemoteIp);
                        break;
                    case MessageType.RequestFullChain:
                        HandleRequestForFullChain(e.RemoteIp);
                        break;
                    case MessageType.FullChain:
                        HandleReceiptOfFullChain(obj);
                        break;
                    case MessageType.BlockToMine:
                        HandleBlockToMine(obj, e.RemoteIp);
                        break;
                }
            });
        }

        /// <summary>
        /// Next block that needs to be mined - Should be forwarded to the BlockManager
        /// </summary>
        /// <param name="container">Container with details of block to be mined</param>
        /// <param name="senderIp">IP of the peer that sent the block</param>
        private void HandleBlockToMine(MessageContainer container, string senderIp)
        {
            _logger.Information("Received a new block to mine form {IpAddress}", senderIp);
            var blockToMine = JsonConvert.DeserializeObject<Block>(container.JsonPayload);
            _mineBlockFunc?.Invoke(blockToMine);
        }

        /// <summary>
        /// Set the function that can be used to check if Block is the current last block
        /// </summary>
        /// <param name="funcToCheckBlocks">Function used to check if Block is last block</param>
        /// <param name="getLastBlockFunc">Function used to retrieve the last block in our chain</param>
        /// <param name="getFullChain">Retrieve all blocks currently in the BlockChain</param>
        /// <param name="updateChainFunc">Update the current chain with a newly received chain</param>
        /// <param name="mineBlockFunc">Func used to request BlockManager to mine a block</param>
        public void SetBlockCheckFunction(Func<Block, bool> funcToCheckBlocks, Func<Block> getLastBlockFunc, 
            Func<List<Block>> getFullChain, Action<List<Block>> updateChainFunc, Action<Block> mineBlockFunc)
        {
            _funcToCheckBlocks = funcToCheckBlocks;
            _getLastBlockFunc = getLastBlockFunc;
            _getFullChain = getFullChain;
            _updateChainFunc = updateChainFunc;
            _mineBlockFunc = mineBlockFunc;
        }

        /// <summary>
        /// Occurs when the peers change - We need to check what our peer has for the blockchain
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Peer change arguments</param>
        private async void TransportManagerOnPeerChange(object sender, PeerChangeEventArgs e)
        {
            // TODO - We need to get the latest block from each peer, if the blocks do not match we need to get the latest chain
            foreach (var peer in e.Peers)
            {
                await _directRequestSemaphore.WaitAsync();
                var container = new MessageContainer
                {
                    MessageType = MessageType.RequestLastBlock
                };
                await peer.SendMsgTCPAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(container)));
            }
        }

        /// <summary>
        /// Broadcast changes to peers
        /// </summary>
        /// <param name="blockToSend">The data block that should be sent to peers</param>
        public async Task BroadcastNewBlockAsync(Block blockToSend)
        {
            var blockJson = JsonConvert.SerializeObject(blockToSend);
            var container = new MessageContainer
            {
                MessageType = MessageType.NewBlockMined,
                JsonPayload = blockJson
            };
            await _transportManager.SendToAllPeersAsyncTCP(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(container)));
        }

        /// <summary>
        /// Notify peers that a new block requiring mining has been requested
        /// </summary>
        /// <param name="blockToSend">The block that needs to be mined</param>
        public async Task BroadCastNextBlockToMineAsync(Block blockToSend)
        {
            var blockJson = JsonConvert.SerializeObject(blockToSend);
            var container = new MessageContainer
            {
                MessageType = MessageType.BlockToMine,
                JsonPayload = blockJson
            };

            await _transportManager.SendToAllPeersAsyncTCP(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(container)));
        }

        /// <summary>
        /// Add a new Peer
        /// </summary>
        /// <param name="ipAddress">IP Address of the peer to add</param>
        /// <returns></returns>
        public async Task<bool> AddPeerAsync(string ipAddress)
        {
            try
            {
                // TODO - We cannot add our own or the loop-back address
                if (_transportManager.KnownPeers.Any(x => x.IpAddress == ipAddress))
                {
                    _logger.Warning("Peer with {IpAddress} has already been added", ipAddress);
                    return false;
                }
                await _transportManager.DirrectConnectAsyncTCP(ipAddress);
                return true;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occured while trying to add {IpAddress} to peers", ipAddress);
                return false;
            }
        }

        /// <summary>
        /// Retrieve a list of all Peers that are connected
        /// </summary>
        /// <returns>List of Peer IP addressess</returns>
        public List<string> GetPeers()
        {
            return _transportManager.KnownPeers.Select(x => x.IpAddress).ToList();
        }

        /// <summary>
        /// Starts the Peer-to-peer server
        /// </summary>
        /// <returns></returns>
        private async Task RunServerAsync()
        {
            await _transportManager.StartAsync();
            _logger.Information("Peer to peer started on {IpAddress}", await _transportManager.GetIpAddress());
        }

        /// <summary>
        /// HttpListener instance
        /// </summary>
        private readonly TransportManager _transportManager;

        /// <summary>
        /// Serilog logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Function to check if received block is the last block in our chain
        /// </summary>
        private Func<Block, bool> _funcToCheckBlocks;

        /// <summary>
        /// Function used to retrieve the last block in the chain
        /// </summary>
        private Func<Block> _getLastBlockFunc;

        /// <summary>
        /// Get the full chain
        /// </summary>
        private Func<List<Block>> _getFullChain;

        /// <summary>
        /// Update the chain with a different chain
        /// </summary>
        private Action<List<Block>> _updateChainFunc;

        /// <summary>
        /// Function used to request the BlockManager to mine a block
        /// </summary>
        private Action<Block> _mineBlockFunc;

        /// <summary>
        /// Semaphore used to lock peer communication
        /// </summary>
        private readonly SemaphoreSlim _directRequestSemaphore = new SemaphoreSlim(1);
    }
}