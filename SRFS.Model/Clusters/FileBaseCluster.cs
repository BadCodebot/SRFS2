using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.IO;

namespace SRFS.Model.Clusters {

    public abstract class FileBaseCluster : DataCluster {

        // Public
        #region Fields

        public const int FileBaseCluster_HeaderLength = DataCluster_HeaderLength + HeaderLength;

        #endregion
        #region Constructors

        protected FileBaseCluster(int address, int clusterSize, Guid volumeID, ClusterType clusterType) : base(address, clusterSize, volumeID, clusterType) {
            if (address == Constants.NoAddress) throw new ArgumentOutOfRangeException();

            _fileID = Constants.NoID;
            _nextClusterAddress = Constants.NoAddress;
            _bytesUsed = 0;
            _writeTime = DateTime.MinValue;
        }

        #endregion
        #region Properties

        /// <summary>
        /// A unique ID number that remains constant throughout the life of the file.
        /// </summary>
        public int FileID {
            get {
                return _fileID;
            }
            set {
                if (_fileID == value) return;
                _fileID = value;
                NotifyPropertyChanged();
            }
        }

        public int NextClusterAddress {
            get {
                return _nextClusterAddress;
            }
            set {
                if (_nextClusterAddress == value) return;
                _nextClusterAddress = value;
                NotifyPropertyChanged();
            }
        }

        public int BytesUsed {
            get {
                return _bytesUsed;
            }
            set {
                if (_bytesUsed == value) return;
                _bytesUsed = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime WriteTime {
            get {
                return _writeTime;
            }
            set {
                if (_writeTime == value) return;
                _writeTime = value;
                NotifyPropertyChanged();
            }
        }

        public abstract byte[] Data { get; }

        #endregion
        #region Methods

        protected override void Read(BinaryReader reader) {
            base.Read(reader);

            _fileID = reader.ReadInt32();
            _nextClusterAddress = reader.ReadInt32();
            _bytesUsed = reader.ReadInt32();
            _writeTime = reader.ReadDateTime();
        }

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);

            writer.Write(_fileID);
            writer.Write(_nextClusterAddress);
            writer.Write(_bytesUsed);
            writer.Write(_writeTime);
        }

        #endregion

        protected static byte[] Encrypt(ECDiffieHellmanCng ecc, CngKey publicKey, byte[] bytes, int offset, int length) {
            using (AesCng aes = new AesCng()) {

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = ecc.DeriveKeyMaterial(publicKey);
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateEncryptor()) return decryptor.TransformFinalBlock(bytes, offset, length);
            }
        }

        protected static byte[] Decrypt(ECDiffieHellmanCng ecc, CngKey publicKey, byte[] bytes, int offset, int length) {
            using (AesCng aes = new AesCng()) {

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = ecc.DeriveKeyMaterial(publicKey);
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateDecryptor()) return decryptor.TransformFinalBlock(bytes, offset, length);
            }
        }


        // Private
        #region Fields

        private const int HeaderLength =
            sizeof(int) +
            sizeof(int) +
            sizeof(int) +
            sizeof(long);

        private int _fileID;
        private int _nextClusterAddress;
        private int _bytesUsed;
        private DateTime _writeTime;

        private DataBlock _data;

        #endregion
    }
}
