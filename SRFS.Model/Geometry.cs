using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRFS.Model {

    public class Geometry {

        public Geometry(int bytesPerCluster, int clustersPerTrack, int dataClustersPerTrack, int trackCount) {
            _bytesPerCluster = bytesPerCluster;
            _clustersPerTrack = clustersPerTrack;
            _dataClustersPerTrack = dataClustersPerTrack;
            _trackCount = trackCount;
        }

        public int BytesPerCluster => _bytesPerCluster;

        public int ClustersPerTrack => _clustersPerTrack;

        public int DataClustersPerTrack => _dataClustersPerTrack;

        public int TrackCount => _trackCount;

        private readonly int _bytesPerCluster;
        private readonly int _clustersPerTrack;
        private readonly int _dataClustersPerTrack;
        private readonly int _trackCount;
    }
}
