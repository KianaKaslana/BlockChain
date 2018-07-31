using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BlockChain.ExtensionMethods;
using BlockChain.Readmodels;
using Newtonsoft.Json;
using Serilog;

namespace BlockChain
{
    /// <summary>
    /// Used to manage persistence of the chain
    /// </summary>
    public class ChainPersistence
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ChainPersistence()
        {
            _logger = Log.Logger.ForContext<ChainPersistence>();
        }

        /// <summary>
        /// Persist the BlockChain to a file
        /// </summary>
        /// <param name="blocks">The blocks to persist</param>
        public void PersistChain(List<Block> blocks)
        {
            if (!blocks.IsValidChain())
            {
                _logger.Warning("Chain is not valid and won't be persisted");
                return;
            }

            var chainJson = JsonConvert.SerializeObject(blocks);
            using (var file = File.Open(ChainFile, FileMode.OpenOrCreate, FileAccess.Write))
            using (var streamWriter = new StreamWriter(file, Encoding.UTF8))
            {
                streamWriter.Write(chainJson);
            }

            _logger.Information("Chain persisted to disk");
        }

        /// <summary>
        /// Load a persisted chain from disk
        /// </summary>
        /// <returns>Loaded chain</returns>
        public List<Block> LoadChain()
        {
            if (!File.Exists(ChainFile))
            {
                _logger.Information("There is no persisted chain");
                return null;
            }

            using (var file = File.Open(ChainFile, FileMode.Open, FileAccess.Read))
            using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                var jsonData = streamReader.ReadToEnd();
                var chain = JsonConvert.DeserializeObject<List<Block>>(jsonData);
                if (!chain.IsValidChain())
                {
                    _logger.Warning("Persisted chain is invalid - Ignoring");
                    return null;
                }

                return chain;
            }
        }

        /// <summary>
        /// File where chain is persisted
        /// </summary>
        private const string ChainFile = "chain.json";

        /// <summary>
        /// Serilog logger instance
        /// </summary>
        private readonly ILogger _logger;
    }
}