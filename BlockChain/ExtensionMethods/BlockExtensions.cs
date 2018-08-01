using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BlockChain.Readmodels;
using Newtonsoft.Json;
using Serilog;

namespace BlockChain.ExtensionMethods
{
    /// <summary>
    /// Extension methods for <see cref="Block"/>
    /// </summary>
    public static class BlockExtensions
    {
        /// <summary>
        /// Calculate the SHA256 hash of a block
        /// </summary>
        /// <param name="block">Block for which the hash should be calculated</param>
        /// <returns>SHA256 hash of Index, PreviousHash, TimeStamp and Data</returns>
        public static string CalculateHash(this Block block)
        {
            var blockData = new List<byte>();
            blockData.AddRange(Encoding.UTF8.GetBytes(block.Index.ToString()));
            blockData.AddRange(Encoding.UTF8.GetBytes(block.PreviousHash ?? String.Empty));
            blockData.AddRange(Encoding.UTF8.GetBytes(block.TimeStamp.ToBinary().ToString()));
            blockData.AddRange(Encoding.UTF8.GetBytes(block.Nonce.ToString()));
            blockData.AddRange(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(block.Transactions)));

            var shaer = new SHA256Managed();
            var hashArr = shaer.ComputeHash(blockData.ToArray());
            return string.Join("", hashArr.Select(x => x.ToString("x2")));
        }

        /// <summary>
        /// Mine a block by computing hashes
        /// </summary>
        /// <param name="block">The block to be mined</param>
        /// <param name="difficulty">How hard should the mining be?</param>
        /// <param name="miningCancellationToken">Token used to stop mining</param>
        public static string Mine(this Block block, int difficulty, CancellationToken miningCancellationToken)
        {
            while (true)
            {
                var hashResult = block.CalculateHash();
                var leadingZero = new string('0', difficulty);
                if (hashResult.StartsWith(leadingZero))
                {
                    block.Hash = hashResult;
                    Log.Logger.Information("Generated valid hash {Hash} for new block", hashResult);
                    return hashResult;
                }

                block.Nonce++;

                if (miningCancellationToken.IsCancellationRequested)
                {
                    Log.Logger.Information("Mining of Block was cancelled");
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Check if a Block being added to the end of the chain is valid
        /// </summary>
        /// <param name="newBlock">New block to add to the chain</param>
        /// <param name="previousBlock">Block directly before the current block</param>
        /// <returns>Indicate if block being added is valid</returns>
        public static bool CheckBlockValidity(this Block newBlock, Block previousBlock)
        {
            if (newBlock.Transactions.Count(x => x.Inputs == null) > 1)
            {
                Log.Logger.Error("A block may not contain more than a signle reward transaction!");
                return false;
            }

            if (newBlock.Index != (previousBlock?.Index + 1 ?? 0))
            {
                return false;
            }

            if (newBlock.PreviousHash != previousBlock?.Hash)
            {
                return false;
            }

            if (newBlock.Hash != newBlock.CalculateHash())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if a chain is valid
        /// </summary>
        /// <param name="chain">Block that need to be checked</param>
        /// <returns>Indicates if the received chain is valid</returns>
        public static bool IsValidChain(this List<Block> chain)
        {
            var orderedChain = chain.OrderBy(x => x.Index).ToList();
            var prevBlock = orderedChain[0];
            for (var r = 1; r < orderedChain.Count; r++)
            {
                var currentBlock = orderedChain[r];
                if (!currentBlock.CheckBlockValidity(prevBlock))
                {
                    return false;
                }

                prevBlock = currentBlock;
            }

            return true;
        }
    }
}