using System.Collections.Generic;
using System.Security.AccessControl;
using System.IO;
using System;
using System.Diagnostics;
using System.Security.Principal;

namespace SRFS.Model.Data {

    public sealed class DirectoryEntry : FileSystemEntry, IEquatable<DirectoryEntry> {

        public DirectoryEntry(int id, string name) : base(id, name) {
            Attributes = FileAttributes.Normal;
        }

        public DirectoryEntry(ByteBlock byteBlock, int offset) : base(byteBlock, offset) { }

        public override FileAttributes Attributes {
            get {
                return base.Attributes | FileAttributes.Directory;
            }
            set {
                base.Attributes = value | FileAttributes.Directory;
            }
        }

        public bool Equals(DirectoryEntry other) {
            return base.Equals(other);
        }

        public const int StorageLength = FileSystemEntryStorageLength;
    }

    public static class DirectoryEntryExtensions {

        public static void Set(this ByteBlock byteBlock, int offset, DirectoryEntry entry) {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            entry.Save(byteBlock, offset);
        }

        public static DirectoryEntry ToDirectoryEntry(this ByteBlock byteBlock, int offset) {
            return new DirectoryEntry(byteBlock, offset);
        }
    }
}
