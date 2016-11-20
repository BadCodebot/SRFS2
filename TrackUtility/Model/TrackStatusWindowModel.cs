using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using SRFS.Model;

namespace TrackUtility.Model {

    class TrackStatusWindowModel : INotifyPropertyChanged {

        public TrackStatusWindowModel(FileSystem fileSystem) {
            _fileSystem = fileSystem;

            UsedTrackCount = (from a in (from t in Enumerable.Range(0, Configuration.Geometry.TrackCount) select new Track(_fileSystem, t))
                              where a.Used select a).Count();
            ProtectedTrackCount = (from a in (from t in Enumerable.Range(0, Configuration.Geometry.TrackCount) select new Track(_fileSystem, t))
                                   where a.Used && a.UpToDate select a).Count();
            TrackCount = Configuration.Geometry.TrackCount;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ClusterState GetClusterState(int absoluteAddress) => _fileSystem.GetClusterState(absoluteAddress);

        public Track GetTrack(int n) => new Track(_fileSystem, n);

        public int ProtectedTrackCount {
            get {
                return _protectedTrackCount;
            }
            set {
                _protectedTrackCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProtectedTrackCount)));
            }
        }
        private int _protectedTrackCount = 0;

        public int TrackCount {
            get {
                return _trackCount;
            }
            set {
                _trackCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackCount)));
            }
        }
        private int _trackCount = 0;

        public int UsedTrackCount {
            get {
                return _usedTrackCount;
            }
            set {
                _usedTrackCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsedTrackCount)));
            }
        }
        private int _usedTrackCount = 0;

        private void Status_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            throw new NotImplementedException();
        }

        private FileSystem _fileSystem;
    }
}
