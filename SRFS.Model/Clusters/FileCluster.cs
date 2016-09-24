using SRFS.Model.Data;
using System.Security.Cryptography;

namespace SRFS.Model.Clusters {

    public class FileCluster : FileSystemCluster {

        // Public
        #region Methods

        public void Encrypt(CngKey encryptionKey) {
            base.Data.Set(_keyThumbprintOffset, new KeyThumbprint(encryptionKey).Bytes);

            using (ECDiffieHellmanCng source = new ECDiffieHellmanCng())
            using (AesCng aes = new AesCng()) {
                base.Data.Set(_publicKeyOffset, new PublicKey(source).Bytes);
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = source.DeriveKeyMaterial(encryptionKey);
                aes.IV = new byte[16];

                using (var encryptor = aes.CreateEncryptor()) {
                    byte[] encryptedData = encryptor.TransformFinalBlock(Data, 0, Data.Length);
                    Data.Set(0, encryptedData);
                }
            }
        }

        public void Decrypt(CngKey decryptionKey) {
            if (!new KeyThumbprint(base.Data.ToByteArray(_keyThumbprintOffset, KeyThumbprint.Length)).Equals(new KeyThumbprint(decryptionKey))) throw new System.IO.IOException();

            using (ECDiffieHellmanCng dest = new ECDiffieHellmanCng(decryptionKey))
            using (AesCng aes = new AesCng()) {

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = dest.DeriveKeyMaterial(new PublicKey(base.Data.ToByteArray(_publicKeyOffset, PublicKey.Length)).GetCngKey());
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateDecryptor()) {
                    byte[] decryptedData = decryptor.TransformFinalBlock(Data, 0, Data.Length);
                    Data.Set(0, decryptedData);
                }
            }
        }

        #endregion

        // Protected
        #region Constructors

        protected FileCluster(int offset) : base() {
            _keyThumbprintOffset = offset;
            _publicKeyOffset = _keyThumbprintOffset + Length_KeyThumbprint;

            int paddingOffset = _publicKeyOffset + Length_PublicKey;
            int availableDataSize = base.Data.Length - paddingOffset;
            int paddingLength = availableDataSize % 16;

            _dataOffset = paddingOffset + paddingLength;

            // Check to see that there is room for encrypted data?
        }

        #endregion
        #region Properties

        protected int DataOffset => _dataOffset;

        #endregion
        #region Methods

        protected static int CalculateDataOffset(int bytesPerCluster, int offset) {
            int dataOffset = offset + KeyThumbprint.Length + PublicKey.Length;
            return dataOffset + (16 - dataOffset % 16 + bytesPerCluster % 16) % 16;
        }

        #endregion

        // Private
        #region Fields

        private const int EncryptionBlockSize = 128;
        private const int EncryptionBlockSizeBytes = EncryptionBlockSize / 8;

        private readonly int _dataOffset;

        private readonly int _keyThumbprintOffset;
        private static readonly int Length_KeyThumbprint = KeyThumbprint.Length;

        private readonly int _publicKeyOffset;
        private static readonly int Length_PublicKey = PublicKey.Length;

        #endregion
    }
}