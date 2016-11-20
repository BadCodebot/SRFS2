using System;
using System.IO;
using SRFS.Model;

namespace SRFS.Model.Clusters {

    public sealed class VerifyTimesCluster : ArrayCluster<DateTime> {

        // Public
        #region Constructors

        public VerifyTimesCluster(int address, int clusterSizeBytes, Guid volumeID)
            : base(address, clusterSizeBytes, volumeID, ClusterType.VerifyTimeTable, sizeof(long)) { }

        #endregion

        public static int CalculateElementsPerCluster(int clusterSizeBytes) {
            if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(sizeof(long), clusterSizeBytes);
            return _elementsPerCluster.Value;
        }

        // Protected
        #region Methods

        protected override void WriteElement(BinaryWriter writer, DateTime value) => writer.Write(value);

        protected override DateTime ReadElement(BinaryReader reader) => reader.ReadDateTime();

        #endregion

        private static int? _elementsPerCluster;
    }
}
