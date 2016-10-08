using System;

namespace SRFS.Model.Clusters {

    public sealed class VerifyTimesCluster : ArrayCluster<DateTime> {

        // Public
        #region Constructors

        public VerifyTimesCluster(int address) : base(address, sizeof(long)) {
            Type = ClusterType.VerifyTimeTable;
        }

        public VerifyTimesCluster(VerifyTimesCluster c) : base(c) { }

        #endregion

        public static int ElementsPerCluster {
            get {
                if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(sizeof(long));
                return _elementsPerCluster.Value;
            }
        }

        // Protected
        #region Methods

        protected override void WriteElement(DateTime value, DataBlock byteBlock, int offset) {
            byteBlock.Set(offset, value.Ticks);
        }

        protected override DateTime ReadElement(DataBlock byteBlock, int offset) {
            return new DateTime(byteBlock.ToInt64(offset));
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
