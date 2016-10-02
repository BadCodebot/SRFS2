﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace SRFS.Model.Clusters {

    public abstract class ArrayCluster : Cluster {

        // Public
        #region Fields

        public static new int HeaderLength => _headerLength;
        private static readonly int _headerLength;

        static ArrayCluster() {
            _headerLength = Cluster.HeaderLength + Offset_Data;
        }

        #endregion
        #region Properties

        public int Address {
            get { return _address; }
            set { _address = value; }
        }

        public int NextClusterAddress {
            get {
                return base.Data.ToInt32(Offset_NextCluster);
            }
            set {
                base.Data.Set(Offset_NextCluster, value);
                IsModified = true;
            }
        }

        #endregion
        #region Methods

        public static int CalculateElementCount(int elementSize) {
            return (Configuration.Geometry.BytesPerCluster - HeaderLength) / elementSize;
        }

        public override void Clear() {
            base.Clear();
            NextClusterAddress = Constants.NoAddress;
        }

        #endregion

        // Protected
        #region Constructors

        protected ArrayCluster() : base(Configuration.Geometry.BytesPerCluster) {
            _data = new ByteBlock(base.Data, Offset_Data, base.Data.Length - Offset_Data);
            NextClusterAddress = Constants.NoAddress;
            _address = Constants.NoAddress;
        }

        #endregion
        #region Properties

        protected override long AbsoluteAddress {
            get {
                return _address == Constants.NoAddress ? Constants.NoAddress : _address * Configuration.Geometry.BytesPerCluster;
            }
        }

        protected new ByteBlock Data => _data;

        #endregion

        // Private
        #region Fields

        private static readonly int Offset_NextCluster = 0;
        private static readonly int Length_NextCluster = sizeof(int);

        private static readonly int Offset_Data = Offset_NextCluster + Length_NextCluster;

        private int _address;

        private ByteBlock _data;

        #endregion
    }

    public abstract class ArrayCluster<T> : ArrayCluster, IEnumerable<T> {

        // Public
        #region Properties

        public T this[int index] {
            get {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException();
                return ReadElement(base.Data, index * _elementSize);
            }
            set {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException();
                WriteElement(value, base.Data, index * _elementSize);
                IsModified = true;
            }
        }

        public int Count => _count;

        #endregion

        // Protected
        #region Constructors

        protected ArrayCluster(int elementSize) : base() {
            _elementSize = elementSize;
            _count = CalculateElementCount(elementSize);
        }

        #endregion
        #region Methods

        protected abstract void WriteElement(T value, ByteBlock byteBlock, int offset);

        protected abstract T ReadElement(ByteBlock byteBlock, int offset);

        public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < _count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        // Private
        #region Fields

        private int _elementSize;
        private int _count;

        #endregion
    }
}
