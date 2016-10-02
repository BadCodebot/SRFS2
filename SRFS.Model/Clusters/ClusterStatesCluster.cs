namespace SRFS.Model.Clusters {

    public class ClusterStatesCluster : ArrayCluster<ClusterState> {

        // Public
        #region Constructors

        public ClusterStatesCluster() : base(sizeof(ClusterState)) {
            Type = ClusterType.ClusterStateTable;
        }

        #endregion

        public static int ElementsPerCluster {
            get {
                if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(sizeof(byte));
                return _elementsPerCluster.Value;
            }
        }

        // Protected
        #region Methods

        protected override void WriteElement(ClusterState value, DataBlock dataBlock, int offset) {
            dataBlock.Set(offset, (byte)value);
        }

        protected override ClusterState ReadElement(DataBlock dataBlock, int offset) {
            return (ClusterState)dataBlock.ToByte(offset);
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
