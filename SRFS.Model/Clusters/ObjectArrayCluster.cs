using System;

namespace SRFS.Model.Clusters {

    public sealed class ObjectArrayCluster<T> : ArrayCluster<T> where T : class {

        // Public
        #region Constructors

        public ObjectArrayCluster(int address, ClusterType type, int elementLength, Action<DataBlock,int, T> saveDelegate,
            Func<DataBlock,int,T> loadDelegate) : base(address, elementLength + sizeof(bool)) {
            Type = type;
            _saveDelegate = saveDelegate;
            _loadDelegate = loadDelegate;
            _elementLength = elementLength;
        }

        public ObjectArrayCluster(ObjectArrayCluster<T> c) : base(c) {
            _saveDelegate = c._saveDelegate;
            _loadDelegate = c._loadDelegate;
            _elementLength = c._elementLength;
        }

        #endregion

        // Protected
        #region Methods

        protected override void WriteElement(T value, DataBlock byteBlock, int offset) {
            if (value == null) {
                byteBlock.Set(offset, false);
                offset += sizeof(bool);

                byteBlock.Clear(offset, _elementLength);
            } else {
                byteBlock.Set(offset, true);
                offset += sizeof(bool);

                _saveDelegate(byteBlock, offset, value);
            }
        }

        protected override T ReadElement(DataBlock byteBlock, int offset) {
            if (!byteBlock.ToBoolean(offset)) return null;
            offset += sizeof(bool);

            return _loadDelegate(byteBlock, offset);
        }

        #endregion

        private int _elementLength;
        private Action<DataBlock, int, T> _saveDelegate;
        private Func<DataBlock, int, T> _loadDelegate;
    }
}
