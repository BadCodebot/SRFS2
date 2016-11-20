using SRFS.Model.Clusters;
using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.IO;

namespace SRFS.Model.Data {

    public class SrfsAccessRule : IEquatable<SrfsAccessRule> {

        // Public
        #region Constructors

        public SrfsAccessRule(FileSystemObjectType type, int id, FileSystemAccessRule rule) {
            _type = type;
            _id = id;
            _accessRule = rule;
        }

        public SrfsAccessRule(Directory dir, FileSystemAccessRule rule) {

            _type = FileSystemObjectType.Directory;
            _id = dir.ID;
            _accessRule = rule;
        }

        public SrfsAccessRule(File file, FileSystemAccessRule rule) {

            _type = FileSystemObjectType.File;
            _id = file.ID;
            _accessRule = rule;
        }

        public SrfsAccessRule(BinaryReader reader) {
            _type = (FileSystemObjectType)reader.ReadByte();
            _id = reader.ReadInt32();

            SecurityIdentifier identityReference = reader.ReadSecurityIdentifier();
            FileSystemRights fileSystemRights = (FileSystemRights)reader.ReadInt32();
            InheritanceFlags inheritanceFlags = (InheritanceFlags)reader.ReadInt32();
            PropagationFlags propagationFlags = (PropagationFlags)reader.ReadInt32();
            AccessControlType accessControlType = (AccessControlType)reader.ReadInt32();

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

        public static ObjectArrayCluster<SrfsAccessRule> CreateArrayCluster(int address, int clusterSizeBytes, Guid volumeID) => 
            new ObjectArrayCluster<SrfsAccessRule>(
                address, 
                clusterSizeBytes,
                volumeID,
                ClusterType.AccessRulesTable,
                StorageLength, 
                (writer, value) => value.Write(writer), 
                (reader) => new SrfsAccessRule(reader),
                (writer) => writer.Write(_zero));

        public void Write(BinaryWriter writer) {
            writer.Write((byte)_type);
            writer.Write(_id);
            writer.Write((SecurityIdentifier)_accessRule.IdentityReference);
            writer.Write((int)_accessRule.FileSystemRights);
            writer.Write((int)_accessRule.InheritanceFlags);
            writer.Write((int)_accessRule.PropagationFlags);
            writer.Write((int)_accessRule.AccessControlType);
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

        private static readonly byte[] _zero = new byte[StorageLength];

        private readonly FileSystemObjectType _type;
        private readonly int _id;
        private readonly FileSystemAccessRule _accessRule;

        #endregion
    }
}
