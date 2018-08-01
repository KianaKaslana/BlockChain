using System;
using System.Linq;

namespace BlockChain.ExtensionMethods
{
    /// <summary>
    ///     Extension methods for <see cref="string" />
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Convert a Hex string to a byte array
        /// </summary>
        /// <param name="hex">Hex string to convert</param>
        /// <returns>Byte array for the string</returns>
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}