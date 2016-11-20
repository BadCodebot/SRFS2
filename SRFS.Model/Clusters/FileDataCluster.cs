using System;
using SRFS.Model.Data;
using System.Security.Cryptography;
using System.IO;
using SRFS.Model;

namespace SRFS.Model.Clusters {

    public class FileDataCluster : FileBaseCluster {

        // Public
        #region Constructors

        public FileDataCluster(int address, int clusterSizeBytes, Guid volumeID, PublicKey encryptionKey, PrivateKey decryptionKey)
            : base(address, clusterSizeBytes, volumeID, ClusterType.FileData) {
            if (clusterSizeBytes % 16 != 0) throw new ArgumentException();

            _decryptionKey = decryptionKey;
            _encryptionKey = encryptionKey;

            _plainTextData = new byte[clusterSizeBytes - DataOffset];
        }

        #endregion

        // Protected
        #region Methods

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);

            writer.Write(_encryptionKey.Thumbprint);

            using (ECDiffieHellmanCng source = new ECDiffieHellmanCng()) {
                writer.Write(source.PublicKey);
                writer.Write(_padding);
                writer.Write(Encrypt(source, _encryptionKey.Key, _plainTextData, 0, _plainTextData.Length));
            }
        }

        protected override void Read(BinaryReader reader) {
            base.Read(reader);

            KeyThumbprint encryptionKeyThumbprint = reader.ReadKeyThumbprint();
            if (!encryptionKeyThumbprint.Equals(_decryptionKey.Thumbprint)) throw new System.IO.IOException();

            CngKey publicSourceKey = reader.ReadPublicKey().Key;
            reader.ReadBytes(PaddingLength);
            byte[] encryptedData = reader.ReadBytes(_plainTextData.Length);

            using (ECDiffieHellmanCng destinationKey = new ECDiffieHellmanCng(_decryptionKey.Key)) {
                _plainTextData = Decrypt(destinationKey, publicSourceKey, encryptedData, 0, encryptedData.Length);
            }
        }

        #endregion

        // Private
        #region Fields

        private const int KeyThumbprintOffset = 0;
        private const int KeyThumbprintLength = KeyThumbprint.Length;

        private const int PublicKeyOffset = KeyThumbprintOffset + KeyThumbprintLength;
        private const int PublicKeyLength = PublicKey.Length;

        private const int PaddingOffset = PublicKeyOffset + PublicKeyLength;
        private const int PaddingLength = (16 - ((FileBaseCluster_HeaderLength + PaddingOffset) % 16)) % 16;
        private static readonly byte[] _padding = new byte[PaddingLength];

        private const int DataOffset = PaddingOffset + PaddingLength;

        private PublicKey _encryptionKey;
        private PrivateKey _decryptionKey;

        private byte[] _plainTextData;

        #endregion
    }
}
