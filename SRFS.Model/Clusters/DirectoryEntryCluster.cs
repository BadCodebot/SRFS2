using System;
using System.Security.AccessControl;
using System.Security.Principal;
using SRFS.Model.Data;
using System.Diagnostics;
using System.IO;

namespace SRFS.Model.Clusters {

    public class DirectoryEntryCluster : ArrayCluster<DirectoryEntry> {

        // Public
        #region Constructors

        public DirectoryEntryCluster(FileSystem fileSystem) : base(ElementLength) {
            _fileSystem = fileSystem;
        }

        #endregion

        public static int ElementsPerCluster {
            get {
                if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(ElementLength);
                return _elementsPerCluster.Value;
            }
        }

        // Protected
        #region Methods

        public const int ElementLength =
            sizeof(bool) +                               // Null Flag 
            sizeof(int) +                                // ID
            sizeof(byte) +                               // Name Length
            sizeof(char) * Constants.MaximumNameLength + // Name
            sizeof(int) +                                // Parent ID
            sizeof(int) +                                // File Attributes
            sizeof(long) +                               // Last Write Time
            sizeof(long) +                               // Creation Time
            sizeof(long) +                               // Last Access Time
            Constants.SecurityIdentifierLength +  // Owner
            Constants.SecurityIdentifierLength;   // Group

        protected override void WriteElement(DirectoryEntry value, ByteBlock byteBlock, int offset) {
            if (value == null) {
                byteBlock.Set(offset, false);
                offset += sizeof(bool);

                byteBlock.Clear(offset, ElementLength - sizeof(bool));
            } else {
                byteBlock.Set(offset, true);
                offset += sizeof(bool);

                byteBlock.Set(offset, value.ID);
                offset += sizeof(int);

                Debug.Assert(value.Name.Length <= byte.MaxValue);
                byteBlock.Set(offset, (byte)value.Name.Length);
                offset += sizeof(byte);

                byteBlock.Set(offset, value.Name);
                byteBlock.Clear(offset + value.Name.Length * sizeof(char), (Constants.MaximumNameLength - value.Name.Length) * sizeof(char));
                offset += Constants.MaximumNameLength * sizeof(char);

                byteBlock.Set(offset, value.ParentID);
                offset += sizeof(int);

                byteBlock.Set(offset, (int)value.Attributes);
                offset += sizeof(int);

                byteBlock.Set(offset, value.LastWriteTime.Ticks);
                offset += sizeof(long);

                byteBlock.Set(offset, value.CreationTime.Ticks);
                offset += sizeof(long);

                byteBlock.Set(offset, value.LastAccessTime.Ticks);
                offset += sizeof(long);

                byteBlock.Set(offset, value.Owner);
                offset += Constants.SecurityIdentifierLength;

                byteBlock.Set(offset, value.Group);
                offset += Constants.SecurityIdentifierLength;
            }
        }

        protected override DirectoryEntry ReadElement(ByteBlock byteBlock, int offset) {
            if (!byteBlock.ToBoolean(offset)) return null;
            offset += sizeof(bool);

            int id = byteBlock.ToInt32(offset);
            offset += sizeof(int);

            int nameLength = byteBlock.ToByte(offset);
            offset += sizeof(byte);

            string name = byteBlock.ToString(offset, nameLength);
            offset += Constants.MaximumNameLength * sizeof(char);

            int parentID = byteBlock.ToInt32(offset);
            offset += sizeof(int);

            FileAttributes attributes = (FileAttributes)byteBlock.ToInt32(offset);
            offset += sizeof(int);

            DateTime lastWriteTime = new DateTime(byteBlock.ToInt64(offset));
            offset += sizeof(long);

            DateTime creationTime = new DateTime(byteBlock.ToInt64(offset));
            offset += sizeof(long);

            DateTime lastAccessTime = new DateTime(byteBlock.ToInt64(offset));
            offset += sizeof(long);

            SecurityIdentifier owner = byteBlock.ToSecurityIdentifier(offset);
            offset += Constants.SecurityIdentifierLength;

            SecurityIdentifier group = byteBlock.ToSecurityIdentifier(offset);
            offset += Constants.SecurityIdentifierLength;

            return new DirectoryEntry(_fileSystem, id, name) {
                ParentID = parentID, Attributes = attributes, LastWriteTime = lastWriteTime, CreationTime = creationTime,
                LastAccessTime = lastAccessTime, Owner = owner, Group = group };
        }

        #endregion

        private FileSystem _fileSystem;
        private static int? _elementsPerCluster;
    }
}
