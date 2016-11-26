using System;
using System.IO;

namespace SRFS.Model.Clusters {


    /// <summary>
    /// The base class for all the data clusters.  Data clusters contain filesystem metadata and files, they do not include the filesystem header
    /// cluster or parity clusters.  This class itself only adds an address field to the header.
    /// 
    /// The header layout is:
    /// 
    /// [Cluster Header (219 bytes)]
    /// Address (4 bytes)
    /// 
    /// Total Length: 223 bytes.
    /// </summary>
    public class DataCluster : Cluster {

        // Public
        #region Constructors

        /// <summary>
        /// Create a data cluster.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="clusterSizeBytes"></param>
        /// <param name="volumeID"></param>
        /// <param name="clusterType"></param>
        public DataCluster(int address, int clusterSizeBytes, Guid volumeID, ClusterType clusterType)
            : base(clusterSizeBytes, volumeID, clusterType) {
            if (address == Constants.NoAddress) throw new ArgumentException();
            _address = address;
        }

        #endregion
        #region Properties

        public const int DataCluster_HeaderLength = Cluster_HeaderLength + sizeof(int);

        public int Address => _address;

        #endregion
        #region Methods

        protected override void Read(BinaryReader reader) {
            base.Read(reader);
            _address = reader.ReadInt32();
        }

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);
            writer.Write(_address);
        }

        #endregion

        // Private
        #region Fields

        private int _address;

        #endregion
    }
}
