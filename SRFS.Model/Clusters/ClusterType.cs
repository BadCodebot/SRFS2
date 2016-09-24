using System;

namespace SRFS.Model.Clusters {

    [Flags]
    public enum ClusterType : byte {
        None = 0,
        PartitionHeader,
        ClusterStateTable,
        NextClusterTable,
        BytesUsedTable,
        WriteTimeTable,
        DirectoryTable,
        FileHeader,
        FileBody
    }
}
