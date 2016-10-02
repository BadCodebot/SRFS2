namespace SRFS.Model.Clusters {

    public class FileDataCluster : FileCluster {

        // Public
        #region Constructors

        public static new int HeaderLength => _headerLength;
        private static readonly int _headerLength;

        static FileDataCluster() {
            _headerLength = CalculateHeaderLength(0);
        }

        public FileDataCluster() : base(0) {
            Type = ClusterType.FileData;
        }

        #endregion
        #region Methods

        public override void Clear() {
            base.Clear();
            Type = ClusterType.FileData;
        }

        #endregion

        // Protected
        #region Properties

        public static int DataSize => Configuration.Geometry.BytesPerCluster - _headerLength;


        #endregion

        // Private
        #region Fields

        #endregion
    }
}
