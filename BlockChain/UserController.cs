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
        public UserController(BlockManager blockManager)
        {
            _logger = Log.Logger.ForContext<UserController>();
            _blockManager = blockManager;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8080/blocks/");
            _listener.Prefixes.Add("http://localhost:8080/mineblock/");
            Task.Run(RunServerAsync);
        }

        public async Task RunServerAsync()
        {
            _listener.Start();
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
#pragma warning disable CS4014 // Do not await this - Otherwise we are stuck until we have dealt with the connection
                Task.Run(() => HandleConnection(context));
#pragma warning restore CS4014
            }
        }

        /// <summary>
        /// Handle an inbound connection
        /// </summary>
        /// <param name="context">The context for the connection to handle</param>
        private void HandleConnection(HttpListenerContext context)
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
        /// Serilog Logger instance
        /// </summary>
        private readonly ILogger _logger;
    }
}