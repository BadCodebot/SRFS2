namespace SRFS.Model.Clusters {

    public class FileDataCluster : FileCluster {

        // Public
        #region Constructors

        public static new readonly int HeaderLength = CalculateHeaderLength(0);

        public FileDataCluster() : base(0) {
            Type = ClusterType.FileBody;
            _data = new ByteBlock(base.Data, DataOffset, base.Data.Length - DataOffset);
        }

        #endregion
        #region Methods

        public override void Clear() {
            base.Clear();
            Type = ClusterType.FileBody;
        }

        #endregion

        // Protected
        #region Properties

        protected override ByteBlock Data {
            get {
                return _data;
            }
        }

        #endregion

        // Private
        #region Fields

        private ByteBlock _data;

        #endregion
    }
}
