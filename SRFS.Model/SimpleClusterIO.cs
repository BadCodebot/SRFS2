using SRFS.IO;
using SRFS.Model.Clusters;
using SRFS.Model.Data;
using System;
using System.Collections.Generic;

namespace SRFS.Model {

    public class SimpleClusterIO : IClusterIO {

        public SimpleClusterIO(IBlockIO io, Geometry geometry, PrivateKey signingKey, IDictionary<KeyThumbprint,PublicKey> signatureKeys, Options options) {
            if (io == null) throw new ArgumentNullException();

            _fileSystemHeaderClusterSize = FileSystemHeaderCluster.CalculateClusterSize(io.BlockSizeBytes);
            _parityClusterSize = _geometry.BytesPerCluster + ParityCluster.CalculateHeaderLength(io.BlockSizeBytes);

            _io = io;
            _buffer = new byte[_parityClusterSize];
            _geometry = geometry;

            _signingKey = signingKey;
            _signatureKeys = signatureKeys;
            _options = options;
        }

        public virtual void Load(Cluster c) {
            lock (_lock) {
                long address = getAddress(c);
                _io.Read(address, _buffer, 0, c.ClusterSizeBytes);
                c.Load(_buffer, 0, _signatureKeys, _options);
            }
        }

        private long getAddress(Cluster c) {
            switch (c) {
                case FileSystemHeaderCluster cluster:
                    return 0;
                case DataCluster cluster:
                    return _fileSystemHeaderClusterSize + (long)cluster.ClusterAddress * _geometry.BytesPerCluster;
                case ParityCluster cluster:
                    return _fileSystemHeaderClusterSize +
                        (long)_geometry.BytesPerCluster * _geometry.DataClustersPerTrack * _geometry.TrackCount +
                        ((long)_geometry.ParityClustersPerTrack * cluster.TrackNumber + cluster.ParityNumber) * _parityClusterSize;
            }
            throw new NotSupportedException();
        }

        public virtual void Save(Cluster c) {
            lock (_lock) {
                long address = getAddress(c);
                c.Save(_buffer, 0, _signingKey);
                _io.Write(address, _buffer, 0, c.ClusterSizeBytes);
            }
        }

        private object _lock = new object();

        private int _fileSystemHeaderClusterSize;
        private int _parityClusterSize;

        private Geometry _geometry;
        private byte[] _buffer;
        private IBlockIO _io;

        private PrivateKey _signingKey;
        private IDictionary<KeyThumbprint, PublicKey> _signatureKeys;
        private Options _options;
    }
}
