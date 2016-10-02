using SRFS.Model.Clusters;
using System;

namespace SRFS.Model.Data {

    public sealed class File : FileSystemObject, IEquatable<File> {

        public const int StorageLength = FileSystemEntryStorageLength + sizeof(long) + sizeof(int);

        public static ObjectArrayCluster<File> CreateArrayCluster(int address) => new ObjectArrayCluster<File>(ClusterType.FileTable,
            StorageLength, (block, offset, value) => value.Save(block, offset), (block, offset) => new File(block, offset)) { Address = address };

        public File(int id, string name) : base(id, name) {
            _length = 0;
            _firstCluster = Constants.NoAddress;
        }

        public File(ByteBlock byteBlock, int offset) : base(byteBlock, offset) {
            offset += FileSystemEntryStorageLength;

            _length = byteBlock.ToInt64(offset);
            offset += sizeof(long);

            _firstCluster = byteBlock.ToInt32(offset);
        }

        public override bool Equals(object obj) {
            if (obj is File) return Equals((File)obj);
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

        public override void Save(ByteBlock byteBlock, int offset) {
            base.Save(byteBlock, offset);
            offset += FileSystemEntryStorageLength;

            byteBlock.Set(offset, _length);
            offset += sizeof(long);

            byteBlock.Set(offset, _firstCluster);
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
