using SRFS.Model.Data;
using SRFS.Model.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace SRFS.Model.Clusters {

    /// <summary>
    /// This is the base class for all clusters.  It calculates the hash and signature for contained data, along with the volume ID and ClusterType,
    /// but does not contain data itself.  All clusters are signed.
    /// 
    /// The class implements INotifyPropertyChanged, and provides a protected method for subclasses to call to emit the event.  The class also
    /// tracks when the data is modified.  Data is considered modified if the class contains data that has not been written to an array, so:
    /// 
    ///   - Upon construction, the data is considered modified
    ///   - After a "read" the data is considered "not modified" (since it should reflect what is ina byte array)
    ///   - After a "write" the data is considered "not modified"
    ///   - Calling the protected NotifyPropertyChanged method will change the cluster to "modified".
    /// 
    /// The header layout is:
    /// 
    /// Marker (4 bytes) - The four ASCII characters "SRFS"
    /// Version (2 bytes) - The version of SRFS, major then minor.
    /// Signature (132 bytes) - The signature of the hash
    /// Hash (32 bytes) - The SHA256 hash of the remaining bytes in the cluster
    /// Signing Key Thumbprint (32 bytes) - The thumbprint of the key used to sign the hash
    /// Volume ID (16 bytes) - A GUID identifying this specific filesystem instance
    /// Cluster Type (1 byte) - The type of cluster
    /// 
    /// Total Length: 219 bytes
    /// </summary>
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
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        #region Properties

        /// <summary>
        /// The size of the header for this class.
        /// </summary>
        public const int Cluster_HeaderLength = HeaderLength;

        /// <summary>
        /// A unique GUID specifying this specific filesystem instance.
        /// </summary>
        public Guid VolumeID => _volumeID;

        /// <summary>
        /// The type of underlying cluster.
        /// </summary>
        public ClusterType ClusterType => _clusterType;

        /// <summary>
        /// The number of bytes that this cluster contains (including header).
        /// </summary>
        public int ClusterSizeBytes => _clusterSizeBytes;

        #endregion
        #region Methods

        /// <summary>
        /// Read this cluster from a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="signatureKeys"></param>
        /// <param name="options"></param>
        public void Read(byte[] bytes, int offset, IDictionary<KeyThumbprint, PublicKey> signatureKeys, Options options) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (signatureKeys == null) throw new ArgumentNullException(nameof(signatureKeys));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + _clusterSizeBytes > bytes.Length) throw new ArgumentException("Not enough space in byte array for cluster.");

            using (var stream = new MemoryStream(bytes, offset, _clusterSizeBytes))
            using (var reader = new BinaryReader(stream)) {

                if (!reader.ReadBytes(Constants.SrfsMarkerLength).SequenceEqual(Constants.SrfsMarker))
                    throw new InvalidClusterException("Invalid Marker");

                if (!reader.ReadBytes(Constants.CurrentVersionLength).SequenceEqual(Constants.CurrentVersion))
                    throw new InvalidClusterException("Unsupported Version");

                Signature signature = reader.ReadSignature();
                byte[] hash = reader.ReadBytes(Constants.HashLength);
                KeyThumbprint signatureThumbprint = reader.ReadKeyThumbprint();

                if (options.VerifyClusterHashes() && !hash.SequenceEqual(calculateHash(bytes, offset)))
                    throw new InvalidHashException();

                if (options.VerifyClusterSignatures()) {
                    PublicKey key = null;
                    if (!signatureKeys.TryGetValue(signatureThumbprint, out key)) throw new MissingKeyException(signatureThumbprint);

                    if (!signature.Verify(bytes, offset + HashPosition, Constants.HashLength, key.Key))
                        throw new InvalidSignatureException();
                }

                _volumeID = reader.ReadGuid();
                _clusterType = reader.ReadClusterType();

                Read(reader);

                _isModified = false;
            }
        }

        /// <summary>
        /// Write the cluster to an array of bytes.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="signingKey"></param>
        public void Write(byte[] bytes, int offset, PrivateKey signingKey) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + _clusterSizeBytes > bytes.Length) throw new ArgumentException();
            if (signingKey == null) throw new ArgumentNullException(nameof(signingKey));

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

        // Protected
        #region Methods

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            _isModified = true;
        }

        /// <summary>
        /// Reads the cluster contents.
        /// 
        /// This method is overridden in the child class to read its corresponding data.  The child should call the base 
        /// version first to ensure that the reader is in the correct position.
        /// </summary>
        /// <param name="reader"></param>
        protected virtual void Read(BinaryReader reader) { }

        /// <summary>
        /// Writes the cluster contents.
        /// 
        /// This method is overridden in the child class to write its corresponding data.  The child should call the base
        /// version first to ensure that the writer is in the correct position.
        /// </summary>
        /// <param name="writer"></param>
        protected virtual void Write(BinaryWriter writer) { }

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

        #endregion
    }
}
