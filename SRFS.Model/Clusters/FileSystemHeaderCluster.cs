using System;
using System.Text;
using System.IO;

namespace SRFS.Model.Clusters {

    /// <summary>
    /// This is the first cluster in the filesystem.  It contains information about the geometry of the filesystem and the volume name,
    /// per below.  The size of this cluster is determined by the underlying device and will be the smallest number of blocks that will 
    /// fit the header.  
    /// 
    /// The header layout is:
    /// 
    /// [Cluster Header (219 bytes)]
    /// Bytes Per Data Cluster (4 bytes)
    /// Clusters Per Track (4 bytes)
    /// Data Clusters Per Track (4 bytes)
    /// Total Tracks (4 bytes)
    /// Volume Name (511 bytes)
    /// 
    /// Total Length: 746 bytes.
    /// </summary>
    public sealed class FileSystemHeaderCluster : Cluster {

        // Public
        #region Constructors

        public FileSystemHeaderCluster(int deviceBlockSize, Guid volumeID) :
            base(CalculateClusterSize(deviceBlockSize), volumeID, ClusterType.FileSystemHeader) {
            BytesPerDataCluster = 0;
            ClustersPerTrack = 0;
            DataClustersPerTrack = 0;
            TrackCount = 0;
            VolumeName = string.Empty;
            _deviceBlockSize = deviceBlockSize;
        }

        #endregion
        #region Properties

        /// <summary>
        /// Calculate how large this cluster will be given a block size.
        /// </summary>
        /// <param name="deviceBlockSize"></param>
        /// <returns></returns>
        public static int CalculateClusterSize(int deviceBlockSize) {
            return (Cluster_HeaderLength + HeaderLength + deviceBlockSize - 1) / deviceBlockSize * deviceBlockSize;
        }

        /// <summary>
        /// The number of bytes in a data cluster (including the header).  This must be a multiple of the underlying block size.
        /// </summary>
        public int BytesPerDataCluster {
            get {
                return _bytesPerDataCluster;
            }
            set {
                if (value <= 0) throw new ArgumentOutOfRangeException($"{nameof(BytesPerDataCluster)} must be greater than zero.");
                if (value % _deviceBlockSize != 0) throw new ArgumentException(
                    $"{nameof(BytesPerDataCluster)} must be a multiple of the device block size.");

                if (_bytesPerDataCluster == value) return;
                _bytesPerDataCluster = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// The total number of clusters in a track, data and parity.
        /// </summary>
        public int ClustersPerTrack {
            get {
                return _clustersPerTrack;
            }
            set {
                if (value <= 0) throw new ArgumentOutOfRangeException($"{nameof(ClustersPerTrack)} must be greater than zero.");

                if (_clustersPerTrack == value) return;
                _clustersPerTrack = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// The number of data clusters in a track.  The remaining clusters are parity clusters.
        /// </summary>
        public int DataClustersPerTrack {
            get {
                return _dataClustersPerTrack;
            }
            set {
                if (value <= 0) throw new ArgumentOutOfRangeException($"{nameof(DataClustersPerTrack)} must be greater than zero.");

                if (_dataClustersPerTrack == value) return;
                _dataClustersPerTrack = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// The number of tracks in the filesystem.  Error-correcting code is done on a per track basis, each track consists of data clusters
        /// and parity clusters.
        /// </summary>
        public int TrackCount {
            get {
                return _totalTracks;
            }
            set {
                if (value <= 0) throw new ArgumentOutOfRangeException($"{nameof(TrackCount)} must be greater than zero.");

                if (_totalTracks == value) return;
                _totalTracks = value;
                NotifyPropertyChanged();
            }
        }

        public string VolumeName {
            get {
                return _volumeName;
            }
            set {
                if (value.Length > Constants.MaximumNameLength) throw new ArgumentException();

                _volumeName = value ?? throw new ArgumentNullException();
                NotifyPropertyChanged();
            }
        }

        #endregion
        #region Methods

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);

            writer.Write(_bytesPerDataCluster);
            writer.Write(_clustersPerTrack);
            writer.Write(_dataClustersPerTrack);
            writer.Write(_totalTracks);

            writer.WriteSrfsString(_volumeName);
        }

        protected override void Read(BinaryReader reader) {
            base.Read(reader);

            _bytesPerDataCluster = reader.ReadInt32();
            _clustersPerTrack = reader.ReadInt32();
            _dataClustersPerTrack = reader.ReadInt32();
            _totalTracks = reader.ReadInt32();

            _volumeName = reader.ReadSrfsString();
        }

        #endregion

        // Private
        #region Fields

        private const int HeaderLength =
            sizeof(int) +
            sizeof(int) +
            sizeof(int) +
            sizeof(int) +
            sizeof(byte) +
            Constants.MaximumNameLength * sizeof(char);

        private int _bytesPerDataCluster;
        private int _clustersPerTrack;
        private int _dataClustersPerTrack;
        private int _totalTracks;
        private string _volumeName;
        private int _deviceBlockSize;

        #endregion
    }
}
