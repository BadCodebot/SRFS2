using System;
using System.Text;

namespace SRFS.Model.Clusters {

    public class FileHeaderCluster : FileCluster {

        public static new readonly int HeaderLength = CalculateHeaderLength(Offset_Data);

        // Public
        #region Fields

        public const int MaximumNameLength = 255;

        #endregion
        #region Constructors

        public FileHeaderCluster() : base(Offset_Name + MaximumNameLength * sizeof(char)) {
            ParentID = Constants.NoID;
            Name = string.Empty;
            Type = ClusterType.FileHeader;
            _data = new ByteBlock(base.Data, DataOffset, base.Data.Length - DataOffset);
        }

        #endregion
        #region Properties

        public int ParentID {
            get {
                return base.Data.ToInt32(Offset_ParentID);
            }
            set {
                base.Data.Set(Offset_ParentID, value);
            }
        }

        public string Name {
            get {
                return base.Data.ToString(Offset_Name, base.Data.ToByte(Offset_NameLength));
            }
            set {
                if (value == null) throw new ArgumentNullException();
                if (value.Length > MaximumNameLength) throw new ArgumentException();

                base.Data.Set(Offset_NameLength, (byte)value.Length);
                base.Data.Set(Offset_Name, value);
                base.Data.Clear(Offset_Name + value.Length * sizeof(char), (MaximumNameLength - value.Length) * sizeof(char));
            }
        }

        #endregion
        #region Methods 

        public override void Clear() {
            base.Clear();
            ParentID = Constants.NoID;
            Name = string.Empty;
            Type = ClusterType.FileHeader;
        }

        #endregion

        // Protected
        #region Properties

        protected override ByteBlock Data {
            get {
                return _data;
            }
        }

        #endregion

        // Private
        #region Fields

        private ByteBlock _data;

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
