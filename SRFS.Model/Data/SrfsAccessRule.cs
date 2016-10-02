using SRFS.Model.Clusters;
using System.Security.AccessControl;
using System.Security.Principal;
using System;

namespace SRFS.Model.Data {

    public class SrfsAccessRule : IEquatable<SrfsAccessRule> {

        public static ObjectArrayCluster<SrfsAccessRule> CreateArrayCluster(int address) => new ObjectArrayCluster<SrfsAccessRule>(ClusterType.AccessRulesTable,
            StorageLength, (block, offset, value) => value.Save(block, offset), (block, offset) => new SrfsAccessRule(block, offset)) { Address = address };

        public SrfsAccessRule(FileSystemObjectType type, int id, FileSystemAccessRule rule) {
            _type = type;
            _id = id;
            _accessRule = rule;
        }

        public override bool Equals(object obj) {
            if (obj is SrfsAccessRule) return Equals((SrfsAccessRule)obj);
            return false;
        }

        public bool Equals(SrfsAccessRule obj) {
            return _id == obj._id &&
                _type == obj._type &&
                _accessRule.IdentityReference == obj._accessRule.IdentityReference &&
                _accessRule.FileSystemRights == obj._accessRule.FileSystemRights &&
                _accessRule.InheritanceFlags == obj._accessRule.InheritanceFlags &&
                _accessRule.PropagationFlags == obj._accessRule.PropagationFlags &&
                _accessRule.AccessControlType == obj._accessRule.AccessControlType;
        }

        public override int GetHashCode() {
            return _id.GetHashCode();
        }

        public SrfsAccessRule(ByteBlock byteBlock, int offset) {
            _type = (FileSystemObjectType)byteBlock.ToByte(offset);
            offset += sizeof(byte);

            _id = byteBlock.ToInt32(offset);
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

            _accessRule = new FileSystemAccessRule(identityReference, fileSystemRights, inheritanceFlags, propagationFlags, accessControlType);
        }

        public FileSystemObjectType FileSystemObjectType => _type;
        public int ID => _id;
        public FileSystemAccessRule Rule => _accessRule;

        public const int StorageLength =
            sizeof(byte) + // File System Object Type
            sizeof(int) + // ID
            Constants.SecurityIdentifierLength + // Identity Reference
            sizeof(int) + // File System Rights
            sizeof(int) + // Inheritance Flags
            sizeof(int) + // Propagation Flags
            sizeof(int); // Access Control Type

        public void Save(ByteBlock byteBlock, int offset) {
            byteBlock.Set(offset, (byte)_type);
            offset += sizeof(byte);

            byteBlock.Set(offset, _id);
            offset += sizeof(int);

            byteBlock.Set(offset, (SecurityIdentifier)_accessRule.IdentityReference);
            offset += Constants.SecurityIdentifierLength;

            byteBlock.Set(offset, (int)_accessRule.FileSystemRights);
            offset += sizeof(int);

            byteBlock.Set(offset, (int)_accessRule.InheritanceFlags);
            offset += sizeof(int);

            byteBlock.Set(offset, (int)_accessRule.PropagationFlags);
            offset += sizeof(int);

            byteBlock.Set(offset, (int)_accessRule.AccessControlType);
        }

        private readonly FileSystemObjectType _type;
        private readonly int _id;
        private readonly FileSystemAccessRule _accessRule;
    }
}
