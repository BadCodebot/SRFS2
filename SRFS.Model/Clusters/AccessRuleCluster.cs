using System.Security.AccessControl;
using System.Security.Principal;
using SRFS.Model.Data;

namespace SRFS.Model.Clusters {

    public class AccessRuleCluster : ArrayCluster<FileSystemAccessRuleData> {

        // Public
        #region Constructors

        public AccessRuleCluster() : base(EntryLength) {
            Type = ClusterType.AccessRulesTable;
        }

        #endregion

        public static int ElementsPerCluster {
            get {
                if (!_elementsPerCluster.HasValue) _elementsPerCluster = CalculateElementCount(EntryLength);
                return _elementsPerCluster.Value;
            }
        }

        // Protected
        #region Methods

        public const int EntryLength =
            sizeof(bool) + // Null Flag
            sizeof(byte) + // File System Object Type
            sizeof(int) + // ID
            Constants.SecurityIdentifierLength + // Identity Reference
            sizeof(int) + // File System Rights
            sizeof(int) + // Inheritance Flags
            sizeof(int) + // Propagation Flags
            sizeof(int); // Access Control Type

        protected override void WriteElement(FileSystemAccessRuleData value, ByteBlock byteBlock, int offset) {
            if (value == null) {
                byteBlock.Set(offset, false);
                offset += sizeof(bool);

                byteBlock.Clear(offset, EntryLength - sizeof(bool));
            } else {
                byteBlock.Set(offset, true);
                offset += sizeof(bool);

                byteBlock.Set(offset, (byte)value.FileSystemObjectType);
                offset += sizeof(byte);

                byteBlock.Set(offset, value.ID);
                offset += sizeof(int);

                byteBlock.Set(offset, (SecurityIdentifier)value.Rule.IdentityReference);
                offset += Constants.SecurityIdentifierLength;

                byteBlock.Set(offset, (int)value.Rule.FileSystemRights);
                offset += sizeof(int);

                byteBlock.Set(offset, (int)value.Rule.InheritanceFlags);
                offset += sizeof(int);

                byteBlock.Set(offset, (int)value.Rule.PropagationFlags);
                offset += sizeof(int);

                byteBlock.Set(offset, (int)value.Rule.AccessControlType);
            }
        }

        protected override FileSystemAccessRuleData ReadElement(ByteBlock byteBlock, int offset) {
            if (!byteBlock.ToBoolean(offset)) return null;
            offset += sizeof(bool);

            FileSystemObjectType type = (FileSystemObjectType)byteBlock.ToByte(offset);
            offset += sizeof(byte);

            int id = byteBlock.ToInt32(offset);
            offset += sizeof(int);

            SecurityIdentifier identityReference = byteBlock.ToSecurityIdentifier(offset);
            offset += Constants.SecurityIdentifierLength;

            FileSystemRights fileSystemRights = (FileSystemRights)byteBlock.ToInt32(offset);
            offset += sizeof(int);

            InheritanceFlags inheritanceFlags = (InheritanceFlags)byteBlock.ToInt32(offset);
            offset += sizeof(int);

            PropagationFlags propagationFlags = (PropagationFlags)byteBlock.ToInt32(offset);
            offset += sizeof(int);

            AccessControlType accessControlType = (AccessControlType)byteBlock.ToInt32(offset);

            FileSystemAccessRule rule = new FileSystemAccessRule(identityReference, fileSystemRights, inheritanceFlags, propagationFlags, accessControlType);
            return new FileSystemAccessRuleData(type, id, rule);
        }

        #endregion

        private static int? _elementsPerCluster;
    }
}
