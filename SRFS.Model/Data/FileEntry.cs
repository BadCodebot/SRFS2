namespace SRFS.Model.Data {

    public class FileEntry : FileSystemEntry {

        public FileEntry(FileSystem fileSystem, int id, string name) : base(fileSystem, id, name) { }

        public long Length {
            get {
                return _length;
            }
            set {
                _length = value;
                NotifyPropertyChanged();
            }
        }

        public int FirstCluster {
            get {
                return _firstCluster;
            }
            set {
                _firstCluster = value;
                NotifyPropertyChanged();
            }
        }

        private long _length;
        private int _firstCluster;
    }
}
