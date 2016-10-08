﻿using System;
using System.Text;

namespace SRFS.Model.Clusters {

    public sealed class FileHeaderCluster : FileEncryptionCluster {

        public static new int HeaderLength => _headerLength;
        private static readonly int _headerLength;

        static FileHeaderCluster() {
            _headerLength = CalculateHeaderLength(Offset_Data);
        }

        // Public
        #region Fields

        public const int MaximumNameLength = 255;

        #endregion
        #region Constructors

        public FileHeaderCluster(int address) : base(address, Offset_Data) {
            ParentID = Constants.NoID;
            Name = string.Empty;
            Type = ClusterType.FileHeader;
        }

        public FileHeaderCluster(FileHeaderCluster c) : base(c) { }

        #endregion
        #region Properties

        public static int DataSize => Configuration.Geometry.BytesPerCluster - _headerLength;

        public int ParentID {
            get {
                return base.OpenBlock.ToInt32(Offset_ParentID);
            }
            set {
                base.OpenBlock.Set(Offset_ParentID, value);
            }
        }

        public string Name {
            get {
                return base.OpenBlock.ToString(Offset_Name, base.OpenBlock.ToByte(Offset_NameLength));
            }
            set {
                if (value == null) throw new ArgumentNullException();
                if (value.Length > MaximumNameLength) throw new ArgumentException();

                base.OpenBlock.Set(Offset_NameLength, (byte)value.Length);
                base.OpenBlock.Set(Offset_Name, value);
                base.OpenBlock.Clear(Offset_Name + value.Length * sizeof(char), (MaximumNameLength - value.Length) * sizeof(char));
            }
        }

        #endregion
        #region Methods 

        public override void Initialize() {
            base.Initialize();
            ParentID = Constants.NoID;
            Name = string.Empty;
            Type = ClusterType.FileHeader;
        }

        #endregion

        // Protected
        #region Properties

        #endregion

        // Private
        #region Fields

        private static readonly int Offset_ParentID = 0;
        private static readonly int Length_ParentID = sizeof(int);

        private static readonly int Offset_NameLength = Offset_ParentID + Length_ParentID;
        private static readonly int Length_NameLength = sizeof(byte);

        private static readonly int Offset_Name = Offset_NameLength + Length_NameLength;
        private static readonly int Length_Name = MaximumNameLength * sizeof(char);

        private static readonly int Offset_Data = Offset_Name + Length_Name;

        #endregion
    }
}
