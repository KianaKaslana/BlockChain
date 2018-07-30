using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;

namespace BlockChain
{
    public class UserController
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public UserController(BlockManager blockManager, PeerToPeerController peerToPeerController, int port)
        {
            _logger = Log.Logger.ForContext<UserController>();
            _blockManager = blockManager;
            _peerToPeerController = peerToPeerController;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/blocks/");
            _listener.Prefixes.Add($"http://localhost:{port}/mineblock/");
            _listener.Prefixes.Add($"http://localhost:{port}/addpeer/");
            _listener.Prefixes.Add($"http://localhost:{port}/getpeers/");
            Task.Run(RunServerAsync);
        }

        /// <summary>
        /// Start the WebServer
        /// </summary>
        private async Task RunServerAsync()
        {
            _listener.Start();
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
#pragma warning disable CS4014 // Do not await this - Otherwise we are stuck until we have dealt with the connection
                Task.Run(() => HandleConnectionAsync(context));
#pragma warning restore CS4014
            }
        }

        /// <summary>
        /// Handle an inbound connection
        /// </summary>
        /// <param name="context">The context for the connection to handle</param>
        private async Task HandleConnectionAsync(HttpListenerContext context)
        {
            try
            {
                switch (context.Request.RawUrl)
                {
                    case "/blocks":
                        HandleBlocksRequest(context);
                        break;
                    case "/mineblock":
                        MineBlock(context);
                        break;
                    case "/addpeer":
                        await AddPeerAsync(context);
                        break;
                    case "/getpeers":
                        await GetPeersAsync(context);
                        break;
                }
            }
            catch (Exception exception)
            {
                using (LogContext.PushProperty("Request", context.Request, true))
                {
                    _logger.Error(exception, "An error occured while handling web request");
                }
            }
            finally
            {
                context.Response.OutputStream.Close();
            }

        }

        /// <summary>
        /// Retrieve collection of all known peers
        /// </summary>
        /// <param name="context">The context of the connection to which we are responding</param>
        private async Task GetPeersAsync(HttpListenerContext context)
        {
            var jsonData = JsonConvert.SerializeObject(_peerToPeerController.GetPeers());
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData)))
            {
                await memStream.CopyToAsync(context.Response.OutputStream);
                context.Response.StatusCode = 200;
                context.Response.ContentType = @"text\json";
                context.Response.Close();
            }
        }

        /// <summary>
        /// Add a peer to the p2p network
        /// </summary>
        /// <param name="context">The context of the connection to which we are responding</param>
        private async Task AddPeerAsync(HttpListenerContext context)
        {
            using (var stringReader = new StreamReader(context.Request.InputStream))
            {
                var data = await stringReader.ReadLineAsync();
                if (await _peerToPeerController.AddPeerAsync(data))
                {
                    context.Response.StatusCode = 200;
                }
                else
                {
                    context.Response.StatusCode = 500;
                }

                context.Response.Close();
            }
        }

        /// <summary>
        /// Handle request for all blocks on the chain
        /// </summary>
        /// <param name="context">The context of the connection to which we are responding</param>
        private void HandleBlocksRequest(HttpListenerContext context)
        {
            var jsonBlocks = JsonConvert.SerializeObject(_blockManager.GetBlocks());
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBlocks)))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = @"text\json";
                memStream.CopyTo(context.Response.OutputStream);
                context.Response.Close();
            }
        }

        /// <summary>
        /// Mine a new block
        /// </summary>
        /// <param name="context">The context of the connection to which we are responding</param>
        private void MineBlock(HttpListenerContext context)
        {
            byte[] data;
            using (var inMemStream = new MemoryStream())
            {
                context.Request.InputStream.CopyTo(inMemStream);
                data = inMemStream.ToArray();
            }

            var resultBlock = JsonConvert.SerializeObject(_blockManager.GenerateBlock(data));
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(resultBlock)))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = @"text\json";
                memStream.CopyTo(context.Response.OutputStream);
                context.Response.Close();
            }
        }

        /// <summary>
        /// HttpListener instance
        /// </summary>
        private readonly HttpListener _listener;

        /// <summary>
        /// BlockManager instance
        /// </summary>
        private readonly BlockManager _blockManager;

        /// <summary>
        /// Peer to peer controller instance
        /// </summary>
        private readonly PeerToPeerController _peerToPeerController;

        /// <summary>
        /// Serilog Logger instance
        /// </summary>
        private readonly ILogger _logger;
    }
}