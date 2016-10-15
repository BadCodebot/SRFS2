namespace SRFS.Model.Clusters {

    public sealed class IntArrayCluster : ArrayCluster<int> {

        // Public
        #region Constructors

        public IntArrayCluster(int address) : base(address, sizeof(int)) { }

        private IntArrayCluster(IntArrayCluster c) : base(c) { }

        public override Cluster Clone() => new IntArrayCluster(this);

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
