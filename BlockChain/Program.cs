using System;
using System.Text;
using BlockChain.Readmodels;
using Serilog;

namespace BlockChain
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupLogger();
            var genesisBlock = new Block(0, null, null, DateTime.UtcNow, Encoding.UTF8.GetBytes("Genesis block desu"), "");
            var blockManager = new BlockManager(genesisBlock);
            var controller = new UserController(blockManager);

            Log.Logger.Information("BlockChain is running...");

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
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
