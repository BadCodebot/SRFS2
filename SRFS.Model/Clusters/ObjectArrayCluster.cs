using System;
using System.IO;

namespace SRFS.Model.Clusters {

    public sealed class ObjectArrayCluster<T> : ArrayCluster<T> where T : class {

        // Public
        #region Constructors

        public ObjectArrayCluster(int address, int clusterSizeBytes, Guid volumeID, ClusterType clusterType, 
            int elementLength, Action<BinaryWriter, T> saveDelegate, Func<BinaryReader,T> loadDelegate, Action<BinaryWriter> zeroDelegate) 
            : base(address, clusterSizeBytes, volumeID, clusterType, elementLength + sizeof(bool)) {
            _saveDelegate = saveDelegate;
            _loadDelegate = loadDelegate;
            _zeroDelegate = zeroDelegate;
            _elementLength = elementLength;
        }

        #endregion

        // Protected
        #region Methods

        protected override void WriteElement(BinaryWriter writer, T value) {
            if (value == null) {
                writer.Write(false);
                _zeroDelegate(writer);
            } else {
                writer.Write(true);
                _saveDelegate(writer, value);
            }
        }

        protected override T ReadElement(BinaryReader reader) {
            if (!reader.ReadBoolean()) {
                reader.BaseStream.Seek(_elementLength, SeekOrigin.Current);
                return null;
            } else {
                return _loadDelegate(reader);
            }
        }

        #endregion

        private int _elementLength;
        private Action<BinaryWriter, T> _saveDelegate;
        private Func<BinaryReader, T> _loadDelegate;
        private Action<BinaryWriter> _zeroDelegate;
    }
}
