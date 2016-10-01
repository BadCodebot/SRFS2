using SRFS.IO;
using SRFS.Model.Data;
using System;
using System.IO;
using System.Security.Cryptography;

namespace SRFS.Model.Clusters {

    public abstract class Cluster {

        // Public
        #region Fields

        public const string HashAlgorithm = "SHA256";

        public static readonly int HeaderLength = Offset_ClusterType + Length_ClusterType;

        #endregion
        #region Constructors

        public Cluster(int clusterSize) {
            if (clusterSize < HeaderLength) throw new ArgumentOutOfRangeException();

            _data = new byte[clusterSize];
            _byteBlock = new ByteBlock(_data, HeaderLength, Configuration.Geometry.BytesPerCluster - HeaderLength);

            Marker = Constants.SrfsMarker;
            Version = Constants.CurrentVersion;
            ID = Guid.Empty;
            Type = ClusterType.None;
        }

        #endregion
        #region Properties

        public byte[] Marker {
            get {
                byte[] buffer = new byte[Length_Marker];
                Buffer.BlockCopy(_data, Offset_Marker, buffer, 0, Length_Marker);
                return buffer;
            }
            set {
                if (value == null) throw new ArgumentNullException();
                if (value.Length != Length_Marker) throw new ArgumentException();
                Buffer.BlockCopy(value, 0, _data, Offset_Marker, Length_Marker);
            }
        }

        public byte[] Version {
            get {
                byte[] buffer = new byte[Length_Version];
                Buffer.BlockCopy(_data, Offset_Version, buffer, 0, Length_Version);
                return buffer;
            }
            set {
                if (value == null) throw new ArgumentNullException();
                if (value.Length != Length_Version) throw new ArgumentException();
                Buffer.BlockCopy(value, 0, _data, Offset_Version, Length_Version);
            }
        }

        public Guid ID {
            get {
                byte[] bytes = new byte[Constants.GuidLength];
                Buffer.BlockCopy(_data, Offset_ID, bytes, 0, Constants.GuidLength);
                return new Guid(bytes);
            }
            set {
                if (value == null) throw new ArgumentNullException();
                Buffer.BlockCopy(value.ToByteArray(), 0, _data, Offset_ID, Constants.GuidLength);
            }
        }

        public ClusterType Type {
            get {
                return (ClusterType)_data[Offset_ClusterType];
            }
            protected set {
                _data[Offset_ClusterType] = (byte)value;
            }
        }

        #endregion
        #region Methods

        public virtual void Clear() {
            Array.Clear(_data, 0, _data.Length);
            Marker = Constants.SrfsMarker;
            Version = Constants.CurrentVersion;
            ID = Configuration.FileSystemID;
            Type = ClusterType.None;
        }

        public bool IsMarkerValid() {
            for (int i = 0; i < Length_Marker; i++) if (_data[Offset_Marker + i] != Constants.SrfsMarker[i]) return false;
            return true;
        }

        public bool IsVersionCompatible() {
            for (int i = 0; i < Length_Version; i++) if (_data[Offset_Version + i] != Constants.CurrentVersion[i]) return false;
            return true;
        }

        public bool IsHashValid() {
            byte[] hash = CalculateHash();
            for (int i = 0; i < Length_Hash; i++) if (hash[i] != _data[Offset_Hash + i]) return false;
            return true;
        }

        public bool IsSignatureValid() {
            return new Signature(_data, Offset_Signature).Verify(_data, Offset_SignatureThumbprint, _data.Length - Offset_SignatureThumbprint,
                Configuration.CryptoSettings.SigningKey);
        }

        public void UpdateHash() {
            Buffer.BlockCopy(CalculateHash(), 0, _data, Offset_Hash, Length_Hash);
        }

        public void UpdateSignature() {
            Buffer.BlockCopy(Configuration.CryptoSettings.SigningKeyThumbprint.Bytes, 0, _data, Offset_SignatureThumbprint, KeyThumbprint.Length);
            Buffer.BlockCopy(
                new Signature(Configuration.CryptoSettings.SigningKey, _data, Offset_SignatureThumbprint, _data.Length - Offset_SignatureThumbprint).Bytes,
                0, _data, Offset_Signature, Signature.Length);
        }

        public virtual void Load(IBlockIO io) {
            if (_data.Length % io.BlockSizeBytes != 0) throw new ArgumentException();

            io.Read(AbsoluteAddress, _data, 0, _data.Length / io.BlockSizeBytes);
            if (!IsMarkerValid()) throw new ArgumentException("Invalid Cluster Marker");
            if (!IsVersionCompatible()) throw new ArgumentException("Unsupported version");
            if (Configuration.Options.VerifyClusterHashes() && !IsHashValid()) throw new IOException("Cluster has invalid hash");
            if (Configuration.Options.VerifyClusterSignatures() && !IsSignatureValid()) throw new IOException("Cluster has invalid signature");
            IsModified = false;
        }

        public virtual void Save(IBlockIO io) {
            if (_data.Length % io.BlockSizeBytes != 0) throw new ArgumentException();

            if (IsModified) {
                UpdateHash();
                UpdateSignature();
                IsModified = false;
            }
            io.Write(AbsoluteAddress, _data, 0, _data.Length / io.BlockSizeBytes);
        }

        #endregion

        // Protected
        #region Properties

        protected abstract long AbsoluteAddress { get; }

        protected virtual ByteBlock Data => _byteBlock;

        protected bool IsModified {
            get { return _isModified; }
            set { _isModified = value; }
        }

        #endregion

        // Private
        #region Methods

        private byte[] CalculateHash() {
            using (var hasher = new SHA256Cng()) {
                hasher.TransformFinalBlock(_data, HeaderLength, _data.Length - HeaderLength);
                return hasher.Hash;
            }
        }

        #endregion
        #region Fields

        private static readonly int Offset_Marker = 0;
        private static readonly int Length_Marker = 4;

        private static readonly int Offset_Version = Offset_Marker + Length_Marker;
        private static readonly int Length_Version = 2;

        private static readonly int Offset_ID = Offset_Version + Length_Version;
        private static readonly int Length_ID = Constants.GuidLength;

        private static readonly int Offset_Signature = Offset_ID + Length_ID;
        private static readonly int Length_Signature = Signature.Length;

        private static readonly int Offset_SignatureThumbprint = Offset_Signature + Length_Signature;
        private static readonly int Length_SignatureThumbprint = KeyThumbprint.Length;

        private static readonly int Offset_Hash = Offset_SignatureThumbprint + Length_SignatureThumbprint;
        private static readonly int Length_Hash = 32;

        private static readonly int Offset_ClusterType = Offset_Hash + Length_Hash;
        private static readonly int Length_ClusterType = sizeof(ClusterType);

        private byte[] _data;
        private ByteBlock _byteBlock;

        private int _clusterSize;
        private bool _isModified;

        #endregion
    }
}
