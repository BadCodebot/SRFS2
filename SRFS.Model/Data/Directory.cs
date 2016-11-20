using SRFS.Model.Clusters;
using System;
using FileAttributes = System.IO.FileAttributes;
using System.IO;

namespace SRFS.Model.Data {

    public class Directory : FileSystemObject, IEquatable<Directory> {

        public const int StorageLength = FileSystemEntryStorageLength;

        public static ObjectArrayCluster<Directory> CreateArrayCluster(int address, int clusterSizeBytes, Guid volumeID) => 
            new ObjectArrayCluster<Directory>(
                address,
                clusterSizeBytes,
                volumeID,
                ClusterType.DirectoryTable,
                StorageLength, 
                (writer, dir) => dir.Write(writer), 
                (reader) => new Directory(reader),
                (writer) => writer.Write(_zero));

        private static readonly byte[] _zero = new byte[StorageLength];

        public Directory(int id, string name) : base(id, name) {
            Attributes = FileAttributes.Normal;
        }

        public Directory(BinaryReader reader) : base(reader) { }

        public override FileAttributes Attributes {
            get {
                return base.Attributes | FileAttributes.Directory;
            }
            set {
                base.Attributes = value | FileAttributes.Directory;
            }
        }

        public override bool Equals(object obj) {
            if (obj is Directory dir) return Equals(dir);
            return false;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }

        public bool Equals(Directory other) {
            return base.Equals(other);
        }
    }
}
