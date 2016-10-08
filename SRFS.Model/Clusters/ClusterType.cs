using System;

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
        FileData
    }
}
