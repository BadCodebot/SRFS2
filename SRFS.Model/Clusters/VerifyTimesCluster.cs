using System;

namespace SRFS.Model.Clusters {

    public class VerifyTimesCluster : ArrayCluster<DateTime> {

        // Public
        #region Constructors

        public VerifyTimesCluster() : base(sizeof(long)) {
            Type = ClusterType.VerifyTimeTable;
        }

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
