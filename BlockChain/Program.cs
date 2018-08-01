using System;
using System.Collections.Generic;
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
            var peerController = new PeerToPeerController(8081);
            var blockManager = new BlockManager(peerController);
            var wallet = new Wallet(blockManager, "myWallet");
            using (new UserController(blockManager, wallet, peerController, 8080))
            {
                GenerateGenesisBlock(blockManager, wallet);
                Log.Logger.Information("BlockChain is running...");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Generate a genesis block and transfer coins to our wallet
        /// </summary>
        /// <param name="blockManager">BlockManager instance</param>
        /// <param name="myWallet">Miner's wallet</param>
        private static void GenerateGenesisBlock(BlockManager blockManager, Wallet myWallet)
        {
            var coinbase = new Wallet(blockManager, "Coinbase");
            var genesisTransaction = new Transaction(coinbase.GetPublicKey, myWallet.GetPublicKey, 100, null);
            genesisTransaction.Outputs.Add(new TransactionOutput(myWallet.GetPublicKey, 100, genesisTransaction.TransactionId));
            genesisTransaction.SignTransaction(coinbase.GetPrivateKey);
            var genesisBlock = new Block(0, null, null, DateTime.UtcNow, new List<Transaction>{ genesisTransaction }, string.Empty, 1);
            blockManager.SetupChain(genesisBlock);
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
