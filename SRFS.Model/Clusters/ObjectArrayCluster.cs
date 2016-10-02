﻿using System;
using System.Security.AccessControl;
using System.Security.Principal;
using SRFS.Model.Data;
using System.Diagnostics;
using System.IO;

namespace SRFS.Model.Clusters {

    public class ObjectArrayCluster<T> : ArrayCluster<T> where T : class {

        // Public
        #region Constructors

        public ObjectArrayCluster(ClusterType type, int elementLength, Action<ByteBlock,int, T> saveDelegate,
            Func<ByteBlock,int,T> loadDelegate) : base(elementLength + sizeof(bool)) {
            Type = type;
            _saveDelegate = saveDelegate;
            _loadDelegate = loadDelegate;
            _elementLength = elementLength;
        }

        #endregion

        // Protected
        #region Methods

        protected override void WriteElement(T value, ByteBlock byteBlock, int offset) {
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

        protected override T ReadElement(ByteBlock byteBlock, int offset) {
            if (!byteBlock.ToBoolean(offset)) return null;
            offset += sizeof(bool);

            return _loadDelegate(byteBlock, offset);
        }

        #endregion

        private int _elementLength;
        private Action<ByteBlock, int, T> _saveDelegate;
        private Func<ByteBlock, int, T> _loadDelegate;
    }
}
