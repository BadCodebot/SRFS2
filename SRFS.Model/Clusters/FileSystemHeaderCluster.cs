using System;
using System.Text;
using System.IO;

namespace SRFS.Model.Clusters {

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
        }

        #endregion
        #region Properties

        public static int CalculateClusterSize(int deviceBlockSize) {
            return (Cluster_HeaderLength + HeaderLength + deviceBlockSize - 1) / deviceBlockSize * deviceBlockSize;
        }

        public int BytesPerDataCluster {
            get {
                return _bytesPerDataCluster;
            }
            set {
                if (_bytesPerDataCluster == value) return;
                _bytesPerDataCluster = value;
                NotifyPropertyChanged();
            }
        }

        public int ClustersPerTrack {
            get {
                return _clustersPerTrack;
            }
            set {
                if (_clustersPerTrack == value) return;
                _clustersPerTrack = value;
                NotifyPropertyChanged();
            }
        }

        public int DataClustersPerTrack {
            get {
                return _dataClustersPerTrack;
            }
            set {
                if (_dataClustersPerTrack == value) return;
                _dataClustersPerTrack = value;
                NotifyPropertyChanged();
            }
        }

        public int TrackCount {
            get {
                return _totalTracks;
            }
            set {
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
                if (value == null) throw new ArgumentNullException();
                if (value.Length > Constants.MaximumNameLength) throw new ArgumentException();

                _volumeName = value;
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

        #endregion
    }
}
