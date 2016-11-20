using SRFS.Model.Clusters;
using System;
using System.IO;

namespace SRFS.Model.Data {

    public sealed class File : FileSystemObject, IEquatable<File> {

        public const int StorageLength = FileSystemEntryStorageLength + sizeof(long) + sizeof(int);

        public static ObjectArrayCluster<File> CreateArrayCluster(int address, int clusterSizeBytes, Guid volumeID) => 
            new ObjectArrayCluster<File>(
                address,
                clusterSizeBytes,
                volumeID,
                ClusterType.FileTable,
                StorageLength, 
                (writer, file) => file.Write(writer), 
                (reader) => new File(reader),
                (writer) => writer.Write(_zero));

        private static readonly byte[] _zero = new byte[StorageLength];

        public File(int id, string name) : base(id, name) {
            _length = 0;
            _firstCluster = Constants.NoAddress;
        }

        public File(BinaryReader reader) : base(reader) {
            _length = reader.ReadInt64();
            _firstCluster = reader.ReadInt32();
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

        public override void Write(BinaryWriter writer) {
            base.Write(writer);
            writer.Write(_length);
            writer.Write(_firstCluster);
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
