using System.Security.Cryptography;
using System.Text;

namespace BlockChain.ExtensionMethods
{
    /// <summary>
    ///     Extension methods for signing and verifying signatures
    /// </summary>
    public static class SigningExtensions
    {
        /// <summary>
        /// Sign the string using a private key
        /// </summary>
        /// <param name="stringToSign">The string that should be signed</param>
        /// <param name="privateKey">The private key to use for signing</param>
        /// <returns>Signature</returns>
        public static byte[] Sign(this string stringToSign, byte[] privateKey)
        {
            var key = CngKey.Import(privateKey, CngKeyBlobFormat.EccFullPrivateBlob);
            using (var ecdsa = new ECDsaCng(key) {HashAlgorithm = CngAlgorithm.Sha384})
            {
                return ecdsa.SignData(Encoding.UTF8.GetBytes(stringToSign));
            }
        }

        /// <summary>
        /// Verify a signature using a private key
        /// </summary>
        /// <param name="data">Data that the signature is for</param>
        /// <param name="signature">Signature to verify</param>
        /// <param name="publicKey">Public key used for verification</param>
        /// <returns>Indicates if signature is valid</returns>
        public static bool VerifySignature(this string data, byte[] signature, byte[] publicKey)
        {
            var key = CngKey.Import(publicKey, CngKeyBlobFormat.EccFullPublicBlob);
            using (var ecdsa = new ECDsaCng(key) {HashAlgorithm = CngAlgorithm.Sha384})
            {
                return ecdsa.VerifyData(Encoding.UTF8.GetBytes(data), signature);
            }
        }
    }
}