using System;
using System.IO;

namespace SRFS.Model.Clusters {

    public sealed class ClusterStatesCluster : ArrayCluster<ClusterState> {

        // Public
        #region Constructors

        public ClusterStatesCluster(int address, int clusterSizeBytes, Guid volumeID) :
            base(address, clusterSizeBytes, volumeID, ClusterType.ClusterStateTable, sizeof(ClusterState)) {
        }

        #endregion

        public static int CalculateElementsPerCluster(int clusterSizeBytes) {
            if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(sizeof(byte), clusterSizeBytes);
            return _elementsPerCluster.Value;
        }

        // Protected
        #region Methods

        protected override void WriteElement(BinaryWriter writer, ClusterState value) {
            writer.Write(value);
        }

        protected override ClusterState ReadElement(BinaryReader reader) {
            return reader.ReadClusterState();
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
