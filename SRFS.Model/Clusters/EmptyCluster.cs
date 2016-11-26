using System;
using System.ComponentModel;

namespace SRFS.Model.Clusters {

    /// <summary>
    /// An empty cluster with no data.  This cluster is used to initialize data clusters on disk to contain no data.  Typically, the filesystem does
    /// not write to the disk until the clusters are needed, but when the parity is calculated uninitialized clusters will be written as empty clusters
    /// so that the error-correcting methods will work correctly.
    /// </summary>
    public class EmptyCluster : DataCluster {

        // Public
        #region Constructors

        public EmptyCluster(int address, int clusterSize, Guid volumeID) : base(address, clusterSize, volumeID, ClusterType.Empty) { }

        #endregion
    }
}
