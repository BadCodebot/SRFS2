namespace SRFS.Model.Clusters {

    public class IntArrayCluster : ArrayCluster<int> {

        // Public
        #region Constructors

        public IntArrayCluster() : base(sizeof(int)) { }

        #endregion

        public static int ElementsPerCluster {
            get {
                if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(sizeof(int));
                return _elementsPerCluster.Value;
            }
        }

        public new ClusterType Type {
            get {
                return base.Type;
            }
            set {
                base.Type = value;
            }
        }


        // Protected
        #region Methods

        protected override void WriteElement(int value, DataBlock byteBlock, int offset) {
            byteBlock.Set(offset, value);
        }

        protected override int ReadElement(DataBlock byteBlock, int offset) {
            return byteBlock.ToInt32(offset);
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
