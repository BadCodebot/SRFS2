using SRFS.Model.Clusters;
using System.Security.AccessControl;
using System.Security.Principal;
using System;
using System.IO;

namespace SRFS.Model.Data {

    public class SrfsAuditRule : IEquatable<SrfsAuditRule> {

        public static ObjectArrayCluster<SrfsAuditRule> CreateArrayCluster(int address, int clusterSizeBytes, Guid volumeID) => 
            new ObjectArrayCluster<SrfsAuditRule>(
                address, 
                clusterSizeBytes,
                volumeID,
                ClusterType.AuditRulesTable,
                StorageLength, 
                (writer, value) => value.Write(writer),
                (reader) => new SrfsAuditRule(reader),
                (writer) => writer.Write(_zero));

        public SrfsAuditRule(FileSystemObjectType type, int id, FileSystemAuditRule rule) {
            _type = type;
            _id = id;
            _auditRule = rule;
        }

        public SrfsAuditRule(Directory dir, FileSystemAuditRule rule) {

            _type = FileSystemObjectType.Directory;
            _id = dir.ID;
            _auditRule = rule;
        }

        public SrfsAuditRule(File file, FileSystemAuditRule rule) {

            _type = FileSystemObjectType.File;
            _id = file.ID;
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

        public SrfsAuditRule(BinaryReader reader) {
            _type = (FileSystemObjectType)reader.ReadByte();
            _id = reader.ReadInt32();

            SecurityIdentifier identityReference = reader.ReadSecurityIdentifier();
            FileSystemRights fileSystemRights = (FileSystemRights)reader.ReadInt32();
            InheritanceFlags inheritanceFlags = (InheritanceFlags)reader.ReadInt32();
            PropagationFlags propagationFlags = (PropagationFlags)reader.ReadInt32();
            AuditFlags auditFlags = (AuditFlags)reader.ReadInt32();

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

        public void Write(BinaryWriter writer) {
            writer.Write((byte)_type);
            writer.Write(_id);

            writer.Write((SecurityIdentifier)_auditRule.IdentityReference);
            writer.Write((int)_auditRule.FileSystemRights);
            writer.Write((int)_auditRule.InheritanceFlags);
            writer.Write((int)_auditRule.PropagationFlags);
            writer.Write((int)_auditRule.AuditFlags);
        }

        private static readonly byte[] _zero = new byte[StorageLength];

        private readonly FileSystemObjectType _type;
        private readonly int _id;
        private readonly FileSystemAuditRule _auditRule;
    }
}
