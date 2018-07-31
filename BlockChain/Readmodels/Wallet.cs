using System;
using System.Security.Cryptography;
using SecurityDriven.Inferno.Extensions;

namespace BlockChain.Readmodels
{
    /// <summary>
    /// Wallet to store user keypairs
    /// </summary>
    public class Wallet
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Wallet()
        {
            GenerateKeys();
        }

        /// <summary>
        /// The KeyPair for the wallet
        /// </summary>
        private CngKey KeyPair { get; set; }

        /// <summary>
        /// Generate KeyPair
        /// </summary>
        private void GenerateKeys()
        {
            // Create and export key
            KeyPair = CngKeyExtensions.CreateNewDsaKey();
            // TODO - How do we secure the key when we save it to disk?
            var privateKey = KeyPair.Export(CngKeyBlobFormat.EccFullPrivateBlob);


            // Import the key
            var newKey = CngKey.Import(privateKey, CngKeyBlobFormat.EccFullPrivateBlob);

        }
    }
}