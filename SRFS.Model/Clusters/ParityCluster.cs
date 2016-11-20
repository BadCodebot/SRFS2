using System;
using System.IO;

namespace SRFS.Model.Clusters {

    public class ParityCluster : Cluster {

        // Public
        #region Fields

        #endregion
        #region Constructors

        public ParityCluster(int blockSize, int bytesPerDataCluster, int trackNumber, int parityNumber, Guid volumeID)
            : base(CalculateClusterSize(blockSize, bytesPerDataCluster), volumeID, ClusterType.Parity) {
            _trackNumber = trackNumber;
            _parityNumber = parityNumber;

            _dataOffset = ClusterSizeBytes - bytesPerDataCluster;
            _paddingLength = _dataOffset - PaddingOffset;

            _data = new byte[bytesPerDataCluster];
        }

        #endregion
        #region Methods

        public static int CalculateClusterSize(int deviceBlockSize, int bytesPerDataCluster) {
            return (PaddingOffset + deviceBlockSize - 1) / deviceBlockSize * deviceBlockSize + bytesPerDataCluster;
        }

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);

            writer.Write(_trackNumber);
            writer.Write(_parityNumber);
            writer.Write(new byte[_paddingLength]);
            writer.Write(_data);
        }

        protected override void Read(BinaryReader reader) {
            base.Read(reader);

            _trackNumber = reader.ReadInt32();
            _parityNumber = reader.ReadInt32();
            reader.ReadBytes(_paddingLength);
            _data = reader.ReadBytes(_data.Length);
        }

        #endregion

        // Protected
        #region Properties

        public int TrackNumber => _trackNumber;

        public int ParityNumber => _parityNumber;

        public byte[] Data => _data;

        #endregion

        // Private
        #region Fields

        private static readonly int TrackNumberOffset = 0;
        private static readonly int TrackNumberLength = sizeof(int);

        private static readonly int ParityNumberOffset = TrackNumberOffset + TrackNumberLength;
        private static readonly int ParityNumberLength = sizeof(int);

        private static readonly int PaddingOffset = ParityNumberOffset + ParityNumberLength;

        private int _paddingLength;

        private int _dataOffset;

        private byte[] _data;
        private int _trackNumber;
        private int _parityNumber;

        #endregion
    }
}
