using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BlockChain.Readmodels;

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
            blockData.AddRange(block.Data);

            var shaer = new SHA256Managed();
            var hashArr = shaer.ComputeHash(blockData.ToArray());
            return String.Join("", hashArr.Select(x => x.ToString("x2")));
        }

        /// <summary>
        /// Check if a Block being added to the end of the chain is valid
        /// </summary>
        /// <param name="newBlock">New block to add to the chain</param>
        /// <param name="previousBlock">Block directly before the current block</param>
        /// <returns>Indicate if block being added is valid</returns>
        public static bool CheckBlockValidity(this Block newBlock, Block previousBlock)
        {
            if (newBlock.Index != previousBlock.Index + 1)
            {
                return false;
            }

            if (newBlock.PreviousHash != previousBlock.Hash)
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