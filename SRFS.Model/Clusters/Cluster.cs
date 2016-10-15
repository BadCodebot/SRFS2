using SRFS.IO;
using SRFS.Model.Data;
using System;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;

namespace SRFS.Model.Clusters {

    public abstract class Cluster : ICloneable<Cluster> {

        // Public
        #region Fields

        public const string HashAlgorithm = "SHA256";

        public abstract Cluster Clone();

        #endregion
        #region Constructors

        protected Cluster(int address, int clusterSize) {
            if (clusterSize < HeaderLength) throw new ArgumentOutOfRangeException();

            _data = new byte[clusterSize];
            _openBlock = new DataBlock(_data, HeaderLength, clusterSize - HeaderLength);
            _openBlock.Changed += changedHandler;

            Marker = Constants.SrfsMarker;
            Version = Constants.CurrentVersion;
            ID = Guid.Empty;
            Type = ClusterType.None;
            _isModified = true;
            _address = address;
        }

        protected Cluster(Cluster c) {
            _data = new byte[c._data.Length];
            _openBlock = new DataBlock(_data, HeaderLength, _data.Length - HeaderLength);
            _openBlock.Changed += changedHandler;
            _address = c._address;

            Buffer.BlockCopy(c._data, 0, _data, 0, _data.Length);
            _isModified = c._isModified;
        }

        static Cluster() {
            _headerLength = Offset_ClusterType + Length_ClusterType;
        }

        #endregion
        #region Properties

        public int Address {
            get {
                return _address;
            }
        }

        public int SizeBytes => _data.Length;

        public static int HeaderLength => _headerLength;

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
                _isModified = true;
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
                _isModified = true;
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
                _isModified = true;
            }
        }

        public ClusterType Type {
            get {
                return (ClusterType)_data[Offset_ClusterType];
            }
            protected set {
                _data[Offset_ClusterType] = (byte)value;
                _isModified = true;
            }
        }

        #endregion
        #region Methods

        private void changedHandler(object sender) {
            _isModified = true;
        }

        public virtual void Initialize() {
            Marker = Constants.SrfsMarker;
            Version = Constants.CurrentVersion;
            ID = Configuration.FileSystemID;
            _isModified = true;
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

        public virtual void Load(byte[] bytes, int offset) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (_data.Length + offset > bytes.Length) throw new ArgumentException();

            Buffer.BlockCopy(bytes, offset, _data, 0, _data.Length);
            if (!IsMarkerValid()) throw new ArgumentException("Invalid Cluster Marker");
            if (!IsVersionCompatible()) throw new ArgumentException("Unsupported version");
            if (Configuration.Options.VerifyClusterHashes() && !IsHashValid()) throw new IOException("Cluster has invalid hash");
            if (Configuration.Options.VerifyClusterSignatures() && !IsSignatureValid()) throw new IOException("Cluster has invalid signature");
            _isModified = false;
        }

        public virtual void Save(byte[] bytes, int offset) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (_data.Length + offset > bytes.Length) throw new ArgumentException();

            if (_isModified) {
                UpdateHash();
                UpdateSignature();
                _isModified = false;
            }
            Buffer.BlockCopy(_data, 0, bytes, offset, _data.Length);
        }

        #endregion

        // Protected
        #region Properties

        public abstract long AbsoluteAddress { get; }

        protected virtual DataBlock OpenBlock => _openBlock;

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

        private static readonly int _headerLength;

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
        private DataBlock _openBlock;

        private int _clusterSize;
        private bool _isModified;

        private int _address;

        #endregion
    }
}
