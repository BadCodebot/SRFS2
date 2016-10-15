namespace SRFS.Model.Clusters {

    public class FileDataCluster : FileEncryptionCluster {

        // Public
        #region Constructors

        public static new int HeaderLength => _headerLength;
        private static readonly int _headerLength;

        static FileDataCluster() {
            _headerLength = CalculateHeaderLength(0);
        }

        public FileDataCluster(int address) : base(address, 0) {
            Type = ClusterType.FileData;
        }

        public override Cluster Clone() => new FileDataCluster(this);

        private FileDataCluster(FileDataCluster c) : base(c) { }

        #endregion
        #region Methods

        public override void Initialize() {
            base.Initialize();
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
