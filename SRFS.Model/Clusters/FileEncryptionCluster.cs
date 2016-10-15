using SRFS.Model.Data;
using System.Security.Cryptography;
using SRFS.IO;
using System;

namespace SRFS.Model.Clusters {

    public class FileEncryptionCluster : FileBaseCluster {

        // Public
        #region Methods

        #endregion

        // Protected
        #region Constructors

        public static int CalculateHeaderLength(int additionalHeaderLength) {
            int header = FileBaseCluster.HeaderLength + additionalHeaderLength + Length_KeyThumbprint + Length_PublicKey;
            int availableDataSize = Configuration.Geometry.BytesPerCluster - header;
            int paddingLength = availableDataSize % 16;
            return header + paddingLength;
        }

        protected FileEncryptionCluster(int address, int offset) : base(address) {
            _keyThumbprintOffset = offset;
            _publicKeyOffset = _keyThumbprintOffset + Length_KeyThumbprint;

            int paddingOffset = _publicKeyOffset + Length_PublicKey;
            int availableDataSize = base.OpenBlock.Length - paddingOffset;
            int paddingLength = availableDataSize % 16;

            _dataOffset = paddingOffset + paddingLength;

            _dataBytes = new byte[availableDataSize - paddingLength];
            _data = new DataBlock(_dataBytes);
            _data.Changed += dataChanged;
            _isDataModified = true;

            // Check to see that there is room for encrypted data?
        }

        protected FileEncryptionCluster(FileEncryptionCluster c) : base(c) {
            _keyThumbprintOffset = c._keyThumbprintOffset;
            _publicKeyOffset = c._publicKeyOffset;

            _dataOffset = c._dataOffset;

            _dataBytes = new byte[c._dataBytes.Length];
            Buffer.BlockCopy(c._dataBytes, 0, _dataBytes, 0, _dataBytes.Length);
            _data = new DataBlock(_dataBytes);
            _data.Changed += dataChanged;
            _isDataModified = c._isDataModified;

            // Check to see that there is room for encrypted data?
        }

        public override Cluster Clone() => new FileEncryptionCluster(this);

        private void dataChanged(object sender) { _isDataModified = true; }

        #endregion
        #region Properties

        protected int DataOffset => _dataOffset;

        #endregion
        #region Methods

        protected static int CalculateDataOffset(int bytesPerCluster, int offset) {
            int dataOffset = offset + KeyThumbprint.Length + PublicKey.Length;
            return dataOffset + (16 - dataOffset % 16 + bytesPerCluster % 16) % 16;
        }

        public override void Save(byte[] bytes, int offset) {
            if (_isDataModified) {
                base.OpenBlock.Set(_keyThumbprintOffset, Configuration.CryptoSettings.EncryptionKeyThumbprint.Bytes);

                using (ECDiffieHellmanCng source = new ECDiffieHellmanCng())
                using (AesCng aes = new AesCng()) {
                    base.OpenBlock.Set(_publicKeyOffset, new PublicKey(source).Bytes);
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.None;
                    aes.Key = source.DeriveKeyMaterial(Configuration.CryptoSettings.EncryptionKey);
                    aes.IV = new byte[16];

                    using (var encryptor = aes.CreateEncryptor()) {
                        byte[] encryptedData = encryptor.TransformFinalBlock(_dataBytes, 0, _dataBytes.Length);
                        OpenBlock.Set(_dataOffset, encryptedData);
                    }
                }
            }

            base.Save(bytes, offset);
        }

        public override void Load(byte[] bytes, int offset) {
            base.Load(bytes, offset);

            if (!new KeyThumbprint(base.OpenBlock.ToByteArray(_keyThumbprintOffset, KeyThumbprint.Length)).Equals(
                Configuration.CryptoSettings.DecryptionKeyThumbprint)) throw new System.IO.IOException();

            using (ECDiffieHellmanCng dest = new ECDiffieHellmanCng(Configuration.CryptoSettings.DecryptionKey))
            using (AesCng aes = new AesCng()) {

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = dest.DeriveKeyMaterial(new PublicKey(base.OpenBlock.ToByteArray(_publicKeyOffset, PublicKey.Length)).GetCngKey());
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateDecryptor()) {
                    byte[] decryptedData = decryptor.TransformFinalBlock(OpenBlock, _dataOffset, _data.Length);
                    _data.Set(0, decryptedData);
                    _isDataModified = false;
                }
            }
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

        public override DataBlock Data => _data;

        private byte[] _dataBytes;
        private DataBlock _data;
        private bool _isDataModified;

        #endregion
    }
}