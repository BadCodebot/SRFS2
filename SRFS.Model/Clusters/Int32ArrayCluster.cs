using System;
using System.IO;

namespace SRFS.Model.Clusters {

    public sealed class Int32ArrayCluster : ArrayCluster<int> {

        // Public
        #region Constructors

        public Int32ArrayCluster(int address, int clusterSizeBytes, Guid volumeID, ClusterType clusterType)
            : base(address, clusterSizeBytes, volumeID, clusterType, sizeof(int)) { }

        #endregion

        public static int CalculateElementsPerCluster(int clusterSizeBytes) {
            if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(sizeof(int), clusterSizeBytes);
            return _elementsPerCluster.Value;
        }

        // Protected
        #region Methods

        protected override void WriteElement(BinaryWriter writer, int value) {
            writer.Write(value);
        }

        protected override int ReadElement(BinaryReader reader) {
            return reader.ReadInt32();
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
