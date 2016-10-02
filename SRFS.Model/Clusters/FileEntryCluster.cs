using System;
using System.Security.AccessControl;
using System.Security.Principal;
using SRFS.Model.Data;
using System.Diagnostics;
using System.IO;

namespace SRFS.Model.Clusters {

    public class FileEntryCluster : ArrayCluster<FileEntry> {

        // Public
        #region Constructors

        public FileEntryCluster() : base(ElementLength) {
            Type = ClusterType.FileTable;
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
            sizeof(byte) +                               // Attribute Flags
            sizeof(long) +                               // Last Write Time
            sizeof(long) +                               // Creation Time
            sizeof(long) +                               // Last Access Time
            Constants.SecurityIdentifierLength +  // Owner
            Constants.SecurityIdentifierLength +  // Group
            sizeof(long) +                               // Length
            sizeof(int);                                 // First Cluster

        protected override void WriteElement(FileEntry value, ByteBlock byteBlock, int offset) {
            if (value == null) {
                byteBlock.Set(offset, false);
                offset += sizeof(bool);

                byteBlock.Clear(offset, ElementLength - sizeof(bool));
            } else {
                byteBlock.Set(offset, true);
                offset += sizeof(bool);

                byteBlock.Set(offset, value);
            }
        }

        protected override FileEntry ReadElement(ByteBlock byteBlock, int offset) {
            if (!byteBlock.ToBoolean(offset)) return null;
            offset += sizeof(bool);

            return new FileEntry(byteBlock, offset);
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
