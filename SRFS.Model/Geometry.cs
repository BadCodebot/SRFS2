using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model.Clusters;

namespace SRFS.Model {

    public class Geometry {

        public override bool Equals(object obj) {
            if (obj is Geometry) {
                Geometry g = (Geometry)obj;
                return _bytesPerCluster == g._bytesPerCluster &&
                    _clustersPerTrack == g._clustersPerTrack &&
                    _dataClustersPerTrack == g._dataClustersPerTrack &&
                    _trackCount == g._trackCount;
            } else {
                return false;
            }
        }

        public override int GetHashCode() {
            return _bytesPerCluster + _clustersPerTrack + _dataClustersPerTrack + _trackCount;
        }

        public Geometry(int bytesPerCluster, int clustersPerTrack, int dataClustersPerTrack, int trackCount) {
            _bytesPerCluster = bytesPerCluster;
            _clustersPerTrack = clustersPerTrack;
            _dataClustersPerTrack = dataClustersPerTrack;
            _trackCount = trackCount;
        }

        public long CalculateFileSystemSize(int bytesPerBlock) {
            int fileSystemHeaderSize = FileSystemHeaderCluster.CalculateClusterSize(bytesPerBlock);
            return fileSystemHeaderSize + CalculateTrackSizeBytes(bytesPerBlock) * TrackCount;
        }

        public long CalculateTrackSizeBytes(int bytesPerBlock) {
            long dataClusterSize = (long)DataClustersPerTrack * BytesPerCluster;
            long parityClusterHeader = (Cluster.HeaderLength + bytesPerBlock - 1) / bytesPerBlock * bytesPerBlock;
            long parityClusterSize = (ClustersPerTrack - DataClustersPerTrack) * (BytesPerCluster + parityClusterHeader);
            return dataClusterSize + parityClusterSize;
        }

        public int BytesPerCluster => _bytesPerCluster;

        public int ClustersPerTrack => _clustersPerTrack;

        public int DataClustersPerTrack => _dataClustersPerTrack;

        public int ParityClustersPerTrack => _clustersPerTrack - _dataClustersPerTrack;

        public int TrackCount => _trackCount;

        private readonly int _bytesPerCluster;
        private readonly int _clustersPerTrack;
        private readonly int _dataClustersPerTrack;
        private readonly int _trackCount;
    }
}
