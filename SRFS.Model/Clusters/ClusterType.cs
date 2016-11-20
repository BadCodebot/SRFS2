using System;
using System.IO;

namespace SRFS.Model.Clusters {

    [Flags]
    public enum ClusterType : byte {
        None = 0,
        FileSystemHeader,
        ClusterStateTable,
        NextClusterAddressTable,
        BytesUsedTable,
        VerifyTimeTable,
        DirectoryTable,
        FileTable,
        AccessRulesTable,
        AuditRulesTable,
        FileHeader,
        FileData,
        Parity,
        Empty
    }

    public static class ClusterTypeExtensions {

        public static void Write(this BinaryWriter writer, ClusterType type) => writer.Write((byte)type);
        public static ClusterType ReadClusterType(this BinaryReader reader) => (ClusterType)reader.ReadByte();
    }
}
