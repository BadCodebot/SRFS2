using SRFS.Model.Clusters;
using System;

namespace SRFS.Model.Data {

    public sealed class File : FileSystemObject, IEquatable<File> {

        public const int StorageLength = FileSystemEntryStorageLength + sizeof(long) + sizeof(int);

        public static ObjectArrayCluster<File> CreateArrayCluster(int address) => 
            new ObjectArrayCluster<File>(
                address,
                ClusterType.FileTable,
                StorageLength, 
                (block, offset, value) => value.Save(block, offset), 
                (block, offset) => new File(block, offset));

        public File(int id, string name) : base(id, name) {
            _length = 0;
            _firstCluster = Constants.NoAddress;
        }

        public File(DataBlock dataBlock, int offset) : base(dataBlock, offset) {
            offset += FileSystemEntryStorageLength;

            _length = dataBlock.ToInt64(offset);
            offset += sizeof(long);

            _firstCluster = dataBlock.ToInt32(offset);
        }

        public override bool Equals(object obj) {
            if (obj is File file) return Equals(file);
            return false;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }

        public bool Equals(File other) {
            return base.Equals(other) &&
                _length == other._length &&
                _firstCluster == other._firstCluster;
        }

        public override void Save(DataBlock dataBlock, int offset) {
            base.Save(dataBlock, offset);
            offset += FileSystemEntryStorageLength;

            dataBlock.Set(offset, _length);
            offset += sizeof(long);

            dataBlock.Set(offset, _firstCluster);
        }

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
