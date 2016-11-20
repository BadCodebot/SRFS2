using System;
using System.IO;

namespace SRFS.Model.Clusters {

    public class DataCluster : Cluster {

        // Public
        #region Constructors

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
