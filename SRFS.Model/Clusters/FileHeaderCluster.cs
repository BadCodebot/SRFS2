using System;
using System.Text;
using SRFS.Model.Data;
using System.Security.Cryptography;
using System.IO;

namespace SRFS.Model.Clusters {

    public sealed class FileHeaderCluster : FileBaseCluster {

        // Public
        #region Fields

        public const int MaximumNameLength = 255;

        #endregion
        #region Constructors

        public FileHeaderCluster(int address, int clusterSizeBytes, Guid volumeID, PublicKey encryptionKey, PrivateKey decryptionKey) 
            : base(address, clusterSizeBytes, volumeID, ClusterType.FileHeader) {
            if (clusterSizeBytes % 16 != 0) throw new ArgumentException();

            ParentID = Constants.NoID;
            Name = string.Empty;

            _decryptionKey = decryptionKey;
            _encryptionKey = encryptionKey;

            _plainTextData = new byte[clusterSizeBytes - DataOffset];
        }

        #endregion
        #region Properties

        public int ParentID {
            get {
                return _parentID;
            }
            set {
                if (_parentID == value) return;
                _parentID = value;
                NotifyPropertyChanged();
            }
        }

        public string Name {
            get {
                return _name;
            }
            set {
                if (value == null) throw new ArgumentNullException();
                if (value.Length > MaximumNameLength) throw new ArgumentException();

                _name = value;
                NotifyPropertyChanged();
            }
        }

        #endregion
        #region Methods 

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);

            writer.Write(_parentID);
            writer.WriteSrfsString(_name);
            writer.Write(_encryptionKey.Thumbprint);

            using (ECDiffieHellmanCng source = new ECDiffieHellmanCng()) {
                writer.Write(source.PublicKey);
                writer.Write(_padding);
                writer.Write(Encrypt(source, _encryptionKey.Key, _plainTextData, 0, _plainTextData.Length));
            }
        }


        protected override void Read(BinaryReader reader) {
            base.Read(reader);

            _parentID = reader.ReadInt32();
            _name = reader.ReadSrfsString();

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

        private const int ParentIDOffset = 0;
        private const int ParentIDLength = sizeof(int);

        private const int NameLengthOffset = ParentIDOffset + ParentIDLength;
        private const int NameLengthLength = sizeof(byte);

        private const int NameOffset = NameLengthOffset + NameLengthLength;
        private const int NameLength = MaximumNameLength * sizeof(char);

        private const int KeyThumbprintOffset = NameOffset + NameLength;
        private const int KeyThumbprintLength = KeyThumbprint.Length;

        private const int PublicKeyOffset = KeyThumbprintOffset + KeyThumbprintLength;
        private const int PublicKeyLength = PublicKey.Length;

        private const int PaddingOffset = PublicKeyOffset + PublicKeyLength;
        private const int PaddingLength = (16 - ((FileBaseCluster_HeaderLength + PaddingOffset) % 16)) % 16;
        private static readonly byte[] _padding = new byte[PaddingLength];

        private const int DataOffset = PaddingOffset + PaddingLength;

        private int _parentID;
        private string _name;
        
        private PublicKey _encryptionKey;
        private PrivateKey _decryptionKey;

        private byte[] _plainTextData;

        #endregion
    }
}
