using SRFS.Model.Clusters;
using System.Security.AccessControl;
using System.Security.Principal;
using System;

namespace SRFS.Model.Data {

    public class SrfsAuditRule : IEquatable<SrfsAuditRule> {

        public static ObjectArrayCluster<SrfsAuditRule> CreateArrayCluster(int address) => new ObjectArrayCluster<SrfsAuditRule>(ClusterType.AuditRulesTable,
            StorageLength, (block, offset, value) => value.Save(block, offset), (block, offset) => new SrfsAuditRule(block, offset)) { Address = address };

        public SrfsAuditRule(FileSystemObjectType type, int id, FileSystemAuditRule rule) {
            _type = type;
            _id = id;
            _auditRule = rule;
        }

        public override bool Equals(object obj) {
            if (obj is SrfsAuditRule) return Equals((SrfsAuditRule)obj);
            return false;
        }

        public bool Equals(SrfsAuditRule obj) {
            return _id == obj._id &&
                _type == obj._type &&
                _auditRule.IdentityReference == obj._auditRule.IdentityReference &&
                _auditRule.FileSystemRights == obj._auditRule.FileSystemRights &&
                _auditRule.InheritanceFlags == obj._auditRule.InheritanceFlags &&
                _auditRule.PropagationFlags == obj._auditRule.PropagationFlags &&
                _auditRule.AuditFlags == obj._auditRule.AuditFlags;
        }

        public override int GetHashCode() {
            return _id.GetHashCode();
        }

        public SrfsAuditRule(ByteBlock byteBlock, int offset) {
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

            AuditFlags auditFlags = (AuditFlags)byteBlock.ToInt32(offset);

            _auditRule = new FileSystemAuditRule(identityReference, fileSystemRights, inheritanceFlags, propagationFlags, auditFlags);
        }

        public FileSystemObjectType FileSystemObjectType => _type;
        public int ID => _id;
        public FileSystemAuditRule Rule => _auditRule;

        public const int StorageLength =
            sizeof(byte) + // File System Object Type
            sizeof(int) + // ID
            Constants.SecurityIdentifierLength + // Identity Reference
            sizeof(int) + // File System Rights
            sizeof(int) + // Inheritance Flags
            sizeof(int) + // Propagation Flags
            sizeof(int); // Audit Flags

        public void Save(ByteBlock byteBlock, int offset) {
            byteBlock.Set(offset, (byte)_type);
            offset += sizeof(byte);

            byteBlock.Set(offset, _id);
            offset += sizeof(int);

            byteBlock.Set(offset, (SecurityIdentifier)_auditRule.IdentityReference);
            offset += Constants.SecurityIdentifierLength;

            byteBlock.Set(offset, (int)_auditRule.FileSystemRights);
            offset += sizeof(int);

            byteBlock.Set(offset, (int)_auditRule.InheritanceFlags);
            offset += sizeof(int);

            byteBlock.Set(offset, (int)_auditRule.PropagationFlags);
            offset += sizeof(int);

            byteBlock.Set(offset, (int)_auditRule.AuditFlags);
        }

        private readonly FileSystemObjectType _type;
        private readonly int _id;
        private readonly FileSystemAuditRule _auditRule;
    }
}
