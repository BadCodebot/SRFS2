using System;

namespace SRFS.Model.Clusters {

    public class PartitionHeaderCluster : Cluster {

        // Public
        #region Fields

        public static int MaximumNameLength = 255;

        public static readonly new int HeaderLength = Cluster.HeaderLength + Offset_Data;

        #endregion
        #region Constructors

        public PartitionHeaderCluster(int partitionHeaderClusterSize) : base(partitionHeaderClusterSize) {
            Type = ClusterType.PartitionHeader;
            BytesPerCluster = 0;
            TotalClustersPerTrack = 0;
            DataClustersPerTrack = 0;
            TotalTracks = 0;
            VolumeName = string.Empty;
        }

        #endregion
        #region Properties

        public static int ClusterSize {
            get {
                if (!_clusterSize.HasValue) {
                    int blockSize = Configuration.Partition.BytesPerBlock;
                    _clusterSize = (Offset_Data + blockSize - 1) / blockSize * blockSize;
                }

                return _clusterSize.Value;
            }
        }

        public int BytesPerCluster {
            get {
                return base.Data.ToInt32(Offset_BytesPerCluster);
            }
            set {
                base.Data.Set(Offset_BytesPerCluster, value);
            }
        }

        public int TotalClustersPerTrack {
            get {
                return base.Data.ToInt32(Offset_TotalClustersPerTrack);
            }
            set {
                base.Data.Set(Offset_TotalClustersPerTrack, value);
            }
        }

        public int DataClustersPerTrack {
            get {
                return base.Data.ToInt32(Offset_DataClustersPerTrack);
            }
            set {
                base.Data.Set(Offset_DataClustersPerTrack, value);
            }
        }

        public int TotalTracks {
            get {
                return base.Data.ToInt32(Offset_TotalTracks);
            }
            set {
                base.Data.Set(Offset_TotalTracks, value);
            }
        }

        public string VolumeName {
            get {
                return base.Data.ToString(Offset_Name, base.Data.ToByte(Offset_NameLength));
            }
            set {
                if (value == null) throw new ArgumentNullException();
                if (value.Length > MaximumNameLength) throw new ArgumentException();

                base.Data.Set(Offset_NameLength, (byte)value.Length);
                base.Data.Set(Offset_Name, value);
                base.Data.Clear(Offset_Name + value.Length * sizeof(char), (MaximumNameLength - value.Length) * sizeof(char));
            }
        }

        #endregion
        #region Methods

        public override void Clear() {
            base.Clear();
            BytesPerCluster = 0;
            TotalClustersPerTrack = 0;
            DataClustersPerTrack = 0;
            TotalTracks = 0;
            VolumeName = string.Empty;
        }
        #endregion

        // Protected
        #region Properties

        protected override long AbsoluteAddress => 0;

        #endregion

        // Private
        #region Fields

        private static readonly int Offset_BytesPerCluster = 0;
        private static readonly int Length_BytesPerCluster = sizeof(int);

        private static readonly int Offset_TotalClustersPerTrack = Offset_BytesPerCluster + Length_BytesPerCluster;
        private static readonly int Length_TotalClustersPerTrack = sizeof(int);

        private static readonly int Offset_DataClustersPerTrack = Offset_TotalClustersPerTrack + Length_TotalClustersPerTrack;
        private static readonly int Length_DataClustersPerTrack = sizeof(int);

        private static readonly int Offset_TotalTracks = Offset_DataClustersPerTrack + Length_DataClustersPerTrack;
        private static readonly int Length_TotalTracks = sizeof(int);

        private static readonly int Offset_NameLength = Offset_TotalTracks + Length_TotalTracks;
        private static readonly int Length_NameLength = sizeof(byte);

        private static readonly int Offset_Name = Offset_NameLength + Length_NameLength;
        private static readonly int Length_Name = MaximumNameLength * sizeof(char);

        private static readonly int Offset_Data = Offset_Name + Length_Name;

        private static int? _clusterSize;

        #endregion
    }
}
