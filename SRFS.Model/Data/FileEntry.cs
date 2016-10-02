using System;

namespace SRFS.Model.Data {

    public sealed class FileEntry : FileSystemEntry, IEquatable<FileEntry> {

        public FileEntry(int id, string name) : base(id, name) {
            _length = 0;
            _firstCluster = Constants.NoAddress;
        }

        public FileEntry(ByteBlock byteBlock, int offset) : base(byteBlock, offset) {
            offset += FileSystemEntryStorageLength;

            _length = byteBlock.ToInt64(offset);
            offset += sizeof(long);

            _firstCluster = byteBlock.ToInt32(offset);
        }

        public bool Equals(FileEntry other) {
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

    public static class FileEntryExtensions {

        public static void Set(this ByteBlock byteBlock, int offset, FileEntry entry) {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            entry.Save(byteBlock, offset);
        }

        public static FileEntry ToFileEntry(this ByteBlock byteBlock, int offset) {
            return new FileEntry(byteBlock, offset);
        }
    }
}
