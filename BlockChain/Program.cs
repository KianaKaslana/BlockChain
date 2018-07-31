using System;
using System.Text;
using BlockChain.ExtensionMethods;
using BlockChain.Readmodels;
using Serilog;

namespace BlockChain
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupLogger();
            var genesisBlock = new Block(0, null, null, new DateTime(2018, 7, 31, 5, 48, 6), Encoding.UTF8.GetBytes("Genesis block desu"), "500db07a6ba0b6ce49cf0535be9743a719339366e05f736c675b330c16f36e1e", 1);
            genesisBlock.CheckBlockValidity(null);
            var peerController = new PeerToPeerController(8081);
            var blockManager = new BlockManager(peerController, genesisBlock);
            using (new UserController(blockManager, peerController, 8080))
            {
                var wallet = new Wallet();


                Log.Logger.Information("BlockChain is running...");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Set up Serilog
        /// </summary>
        private static void SetupLogger()
        {
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Console()
                .Enrich.FromLogContext();
            Log.Logger = loggerConfig.CreateLogger();
        }
    }
}
