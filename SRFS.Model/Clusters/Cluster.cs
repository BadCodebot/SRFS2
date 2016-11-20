using SRFS.Model.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace SRFS.Model.Clusters {

    public class Cluster : INotifyPropertyChanged {

        // Public
        #region Constructors

        public Cluster(int clusterSizeBytes, Guid volumeID, ClusterType clusterType) {
            if (clusterSizeBytes < HeaderLength) throw new ArgumentOutOfRangeException();
            _clusterSizeBytes = clusterSizeBytes;

            _volumeID = volumeID;
            _clusterType = ClusterType;

            _isModified = true;
        }

        #endregion
        #region Properties

        public const int Cluster_HeaderLength = HeaderLength;

        public Guid VolumeID => _volumeID;

        public ClusterType ClusterType => _clusterType;

        public int ClusterSizeBytes => _clusterSizeBytes;

        #endregion
        #region Methods

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            _isModified = true;
        }

        public void Load(byte[] bytes, int offset, IDictionary<KeyThumbprint, PublicKey> signatureKeys, Options options) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (signatureKeys == null) throw new ArgumentNullException(nameof(signatureKeys));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (bytes.Length + offset > bytes.Length) throw new ArgumentException();

            using (var stream = new MemoryStream(bytes, offset, _clusterSizeBytes))
            using (var reader = new BinaryReader(stream)) {

                if (!reader.ReadBytes(Constants.SrfsMarkerLength).SequenceEqual(Constants.SrfsMarker))
                    throw new IOException("Invalid Marker");

                if (!reader.ReadBytes(Constants.CurrentVersionLength).SequenceEqual(Constants.CurrentVersion))
                    throw new IOException("Unsupported Version");

                Signature signature = reader.ReadSignature();
                byte[] hash = reader.ReadBytes(Constants.HashLength);
                KeyThumbprint signatureThumbprint = reader.ReadKeyThumbprint();

                if (options.VerifyClusterHashes() && !hash.SequenceEqual(calculateHash(bytes, offset)))
                    throw new IOException("Cluster has invalid hash");

                if (options.VerifyClusterSignatures()) {
                    PublicKey key = null;
                    if (!signatureKeys.TryGetValue(signatureThumbprint, out key)) throw new IOException("Cannot verify signature, key not found.");

                    if (!signature.Verify(bytes, offset + HashPosition, Constants.HashLength, key.Key))
                        throw new IOException("Cluster has invalid signature");
                }

                _volumeID = reader.ReadGuid();
                _clusterType = reader.ReadClusterType();

                Read(reader);

                _isModified = false;
            }
        }

        protected virtual void Read(BinaryReader reader) { }

        protected virtual void Write(BinaryWriter writer) { }

        public void Save(byte[] bytes, int offset, PrivateKey signingKey) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + _clusterSizeBytes > bytes.Length) throw new ArgumentException();

            using (var stream = new MemoryStream(bytes, offset, _clusterSizeBytes))
            using (var writer = new BinaryWriter(stream)) {
                writer.Write(Constants.SrfsMarker);
                writer.Write(Constants.CurrentVersion);

                stream.Position = HashCalculationStartPosition;

                writer.Write(signingKey.Thumbprint);
                writer.Write(_volumeID);
                writer.Write(_clusterType);

                Write(writer);

                Array.Clear(bytes, offset + (int)stream.Position, _clusterSizeBytes - (int)stream.Position);

                stream.Position = HashPosition;
                writer.Write(calculateHash(bytes, offset));

                stream.Position = SignaturePosition;
                writer.Write(calculateSignature(bytes, offset, signingKey));
            }

            _isModified = false;
        }

        #endregion

        // Private
        #region Methods

        private byte[] calculateHash(byte[] bytes, int offset) {
            using (var hasher = new SHA256Cng()) {
                hasher.TransformFinalBlock(bytes, offset + HashCalculationStartPosition, _clusterSizeBytes - HashCalculationStartPosition);
                return hasher.Hash;
            }
        }

        private byte[] calculateSignature(byte[] bytes, int offset, PrivateKey signingKey) {
            return new Signature(signingKey.Key, bytes, offset + HashPosition, Constants.HashLength).Bytes;
        }

        #endregion
        #region Fields

        private const int SignaturePosition =
            Constants.SrfsMarkerLength +
            Constants.CurrentVersionLength;

        private const int HashPosition =
            SignaturePosition +
            Signature.Length;

        private const int HashCalculationStartPosition =
            HashPosition +
            Constants.HashLength;

        private const int HeaderLength =
            HashCalculationStartPosition +
            KeyThumbprint.Length +
            Constants.GuidLength +
            sizeof(ClusterType);

        private Guid _volumeID;
        private ClusterType _clusterType;

        private int _clusterSizeBytes;
        private bool _isModified;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
