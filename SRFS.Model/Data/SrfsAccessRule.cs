using SRFS.Model.Clusters;
using System;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SRFS.Model.Data {

    public class SrfsAccessRule : IEquatable<SrfsAccessRule> {

        // Public
        #region Constructors

        public SrfsAccessRule(FileSystemObjectType type, int id, FileSystemAccessRule rule) {
            _type = type;
            _id = id;
            _accessRule = rule;
        }

        public SrfsAccessRule(DataBlock dataBlock, int offset) {
            _type = (FileSystemObjectType)dataBlock.ToByte(offset);
            offset += sizeof(byte);

            _id = dataBlock.ToInt32(offset);
            offset += sizeof(int);

            SecurityIdentifier identityReference = dataBlock.ToSecurityIdentifier(offset);
            offset += Constants.SecurityIdentifierLength;

            FileSystemRights fileSystemRights = (FileSystemRights)dataBlock.ToInt32(offset);
            offset += sizeof(int);

            InheritanceFlags inheritanceFlags = (InheritanceFlags)dataBlock.ToInt32(offset);
            offset += sizeof(int);

            PropagationFlags propagationFlags = (PropagationFlags)dataBlock.ToInt32(offset);
            offset += sizeof(int);

            AccessControlType accessControlType = (AccessControlType)dataBlock.ToInt32(offset);

            _accessRule = new FileSystemAccessRule(identityReference, fileSystemRights, inheritanceFlags, propagationFlags, accessControlType);
        }

        #endregion
        #region Properties

        public const int StorageLength =
            sizeof(byte) + // File System Object Type
            sizeof(int) + // ID
            Constants.SecurityIdentifierLength + // Identity Reference
            sizeof(int) + // File System Rights
            sizeof(int) + // Inheritance Flags
            sizeof(int) + // Propagation Flags
            sizeof(int); // Access Control Type

        public FileSystemObjectType FileSystemObjectType => _type;
        public int ID => _id;
        public FileSystemAccessRule Rule => _accessRule;

        #endregion
        #region Methods

        public static ObjectArrayCluster<SrfsAccessRule> CreateArrayCluster(int address) => new ObjectArrayCluster<SrfsAccessRule>(ClusterType.AccessRulesTable,
            StorageLength, (block, offset, value) => value.Save(block, offset), (block, offset) => new SrfsAccessRule(block, offset)) { Address = address };

        public void Save(DataBlock dataBlock, int offset) {
            dataBlock.Set(offset, (byte)_type);
            offset += sizeof(byte);

            dataBlock.Set(offset, _id);
            offset += sizeof(int);

            dataBlock.Set(offset, (SecurityIdentifier)_accessRule.IdentityReference);
            offset += Constants.SecurityIdentifierLength;

            dataBlock.Set(offset, (int)_accessRule.FileSystemRights);
            offset += sizeof(int);

            dataBlock.Set(offset, (int)_accessRule.InheritanceFlags);
            offset += sizeof(int);

            dataBlock.Set(offset, (int)_accessRule.PropagationFlags);
            offset += sizeof(int);

            dataBlock.Set(offset, (int)_accessRule.AccessControlType);
        }

        //--- IEquatable Implementation ---

        public bool Equals(SrfsAccessRule obj) {
            return _id == obj._id &&
                _type == obj._type &&
                _accessRule.IdentityReference == obj._accessRule.IdentityReference &&
                _accessRule.FileSystemRights == obj._accessRule.FileSystemRights &&
                _accessRule.InheritanceFlags == obj._accessRule.InheritanceFlags &&
                _accessRule.PropagationFlags == obj._accessRule.PropagationFlags &&
                _accessRule.AccessControlType == obj._accessRule.AccessControlType;
        }

        //--- Object Overrides ---

        public override bool Equals(object obj) {
            if (obj is SrfsAccessRule) return Equals((SrfsAccessRule)obj);
            return false;
        }

        public override int GetHashCode() {
            return _id.GetHashCode();
        }

        #endregion

        // Private
        #region Fields

        private readonly FileSystemObjectType _type;
        private readonly int _id;
        private readonly FileSystemAccessRule _accessRule;

        #endregion
    }
}
