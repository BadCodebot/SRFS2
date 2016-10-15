using SRFS.Model.Clusters;
using System;
using FileAttributes = System.IO.FileAttributes;

namespace SRFS.Model.Data {

    public class Directory : FileSystemObject, IEquatable<Directory> {

        public const int StorageLength = FileSystemEntryStorageLength;

        public static ObjectArrayCluster<Directory> CreateArrayCluster(int address) => 
            new ObjectArrayCluster<Directory>(
                address,
                ClusterType.DirectoryTable,
                StorageLength, (block, offset, value) => value.Save(block, offset), 
                (block, offset) => new Directory(block, offset));

        public Directory(int id, string name) : base(id, name) {
            Attributes = FileAttributes.Normal;
        }

        public Directory(DataBlock dataBlock, int offset) : base(dataBlock, offset) { }

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
