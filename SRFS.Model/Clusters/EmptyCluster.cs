using System;
using System.ComponentModel;

namespace SRFS.Model.Clusters {

    public class EmptyCluster : DataCluster {

        // Public
        #region Constructors

        public EmptyCluster(int address, int clusterSize, Guid volumeID) : base(address, clusterSize, volumeID, ClusterType.Empty) { }

        #endregion
    }
}
