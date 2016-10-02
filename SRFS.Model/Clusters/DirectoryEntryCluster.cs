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

        public DirectoryEntryCluster() : base(ElementLength) {
            Type = ClusterType.DirectoryTable;
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

                byteBlock.Set(offset, value);
            }
        }

        protected override DirectoryEntry ReadElement(ByteBlock byteBlock, int offset) {
            if (!byteBlock.ToBoolean(offset)) return null;
            offset += sizeof(bool);

            return new DirectoryEntry(byteBlock, offset);
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
