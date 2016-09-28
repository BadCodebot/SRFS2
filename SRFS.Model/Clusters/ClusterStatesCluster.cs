namespace SRFS.Model.Clusters {

    public class ClusterStatesCluster : ArrayCluster<ClusterState> {

        // Public
        #region Constructors

        public ClusterStatesCluster() : base(sizeof(ClusterState)) { }

        #endregion

        public static int ElementsPerCluster {
            get {
                if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(sizeof(byte));
                return _elementsPerCluster.Value;
            }
        }

        // Protected
        #region Methods

        protected override void WriteElement(ClusterState value, ByteBlock byteBlock, int offset) {
            byteBlock.Set(offset, (byte)value);
        }

        protected override ClusterState ReadElement(ByteBlock byteBlock, int offset) {
            return (ClusterState)byteBlock.ToByte(offset);
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
