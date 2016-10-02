using System;

namespace SRFS.Model.Clusters {

    public class FileSystemCluster : Cluster {

        // Public
        #region Fields

        public static new int HeaderLength => _headerLength;
        private static readonly int _headerLength;

        static FileSystemCluster() {
            _headerLength = Cluster.HeaderLength + Offset_Data;
        }

        #endregion
        #region Constructors

        public FileSystemCluster() : base(Configuration.Geometry.BytesPerCluster) {
            _data = new DataBlock(base.OpenBlock, Offset_Data, base.OpenBlock.Length - Offset_Data);
            _address = Constants.NoAddress;

            FileID = Constants.NoID;
            NextClusterAddress = Constants.NoAddress;
            BytesUsed = 0;
            WriteTime = DateTime.MinValue;
        }

        #endregion
        #region Properties

        /// <summary>
        /// A unique ID number that remains constant throughout the life of the file.
        /// </summary>
        public int FileID {
            get {
                return base.OpenBlock.ToInt32(Offset_FileID);
            }
            set {
                base.OpenBlock.Set(Offset_FileID, value);
            }
        }

        public int NextClusterAddress {
            get {
                return base.OpenBlock.ToInt32(Offset_NextClusterAddress);
            }
            set {
                base.OpenBlock.Set(Offset_NextClusterAddress, value);
            }
        }

        public int BytesUsed {
            get {
                return base.OpenBlock.ToInt32(Offset_BytesUsed);
            }
            set {
                base.OpenBlock.Set(Offset_BytesUsed, value);
            }
        }

        public DateTime WriteTime {
            get {
                return new DateTime(base.OpenBlock.ToInt64(Offset_WriteTime));
            }
            set {
                base.OpenBlock.Set(Offset_WriteTime, value.Ticks);
            }
        }

        public int Address {
            get {
                return _address;
            }
            set {
                _address = value;
            }
        }

        #endregion
        #region Methods

        public override void Clear() {
            base.Clear();
            FileID = Constants.NoID;
            NextClusterAddress = Constants.NoAddress;
            BytesUsed = 0;
            WriteTime = DateTime.MinValue;
        }

        #endregion

        // Protected
        #region Properties

        protected override long AbsoluteAddress {
            get {
                return _address == Constants.NoAddress ? Constants.NoAddress : _address * Configuration.Geometry.BytesPerCluster;
            }
        }

        protected override DataBlock OpenBlock => _data;

        #endregion

        // Private
        #region Fields

        private static readonly int Offset_FileID = 0;
        private static readonly int Length_FileID = sizeof(int);

        private static readonly int Offset_NextClusterAddress = Offset_FileID + Length_FileID;
        private static readonly int Length_NextClusterAddress = sizeof(int);

        private static readonly int Offset_BytesUsed = Offset_NextClusterAddress + Length_NextClusterAddress;
        private static readonly int Length_BytesUsed = sizeof(int);

        private static readonly int Offset_WriteTime = Offset_BytesUsed + Length_BytesUsed;
        private static readonly int Length_WriteTime = sizeof(long);

        private static readonly int Offset_Data = Offset_WriteTime + Length_WriteTime;

        private DataBlock _data;

        private int _address;

        #endregion
    }
}
