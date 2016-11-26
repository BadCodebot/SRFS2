using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace SRFS.Model.Clusters {

    /// <summary>
    /// The base class for data clusters that just contain an array of data.
    /// 
    /// The header layout is:
    /// 
    /// Data Cluster Header (223 bytes)
    /// Next Cluster Address (4 bytes)
    /// 
    /// Total Length: 227 bytes.
    /// </summary>
    public abstract class ArrayCluster : DataCluster {

        // Public
        #region Fields

        public const int ArrayCluster_HeaderLength = DataCluster_HeaderLength + HeaderLength;

        #endregion
        #region Properties

        public int NextClusterAddress {
            get {
                return _nextClusterAddress;
            }
            set {
                if (_nextClusterAddress == value) return;
                _nextClusterAddress = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        // Protected
        #region Constructors

        protected ArrayCluster(int address, int clusterSize, Guid volumeID, ClusterType clusterType) :
            base(address, clusterSize, volumeID, clusterType) {
            if (address == Constants.NoAddress) throw new ArgumentOutOfRangeException();
            _nextClusterAddress = Constants.NoAddress;
        }

        #endregion
        #region Methods

        public static int CalculateElementCount(int elementSize, int bytesPerCluster) {
            return (bytesPerCluster - ArrayCluster_HeaderLength) / elementSize;
        }

        protected override void Read(BinaryReader reader) {
            base.Read(reader);
            _nextClusterAddress = reader.ReadInt32();
        }

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);
            writer.Write(_nextClusterAddress);
        }

        #endregion

        // Private
        #region Fields

        private const int HeaderLength = sizeof(int);

        private int _nextClusterAddress;

        #endregion
    }

    /// <summary>
    /// The generic base class for data clusters that just contain an array of data.  This class adds type-specific code to the non-generic
    /// ArrayCluster class.
    /// 
    /// The header layout is:
    /// 
    /// [Cluster Header (219 bytes)]
    /// [Data Cluster Header (4 bytes)]
    /// [Array Cluster Header (4 bytes)]
    /// 
    /// Total Length: 227 bytes.
    /// </summary>
    public abstract class ArrayCluster<T> : ArrayCluster, IEnumerable<T> {

        // Public
        #region Properties

        public T this[int index] {
            get {
                return _elements[index];
            }
            set {
                _elements[index] = value;
                NotifyPropertyChanged();
            }
        }

        public int Count => _elements.Length;

        #endregion

        // Protected
        #region Constructors

        protected ArrayCluster(int address, int bytesPerCluster, Guid volumeID, ClusterType clusterType, int elementSize) :
            base(address, bytesPerCluster, volumeID, clusterType) {
            _elementSize = elementSize;
            _elements = new T[CalculateElementCount(elementSize, bytesPerCluster)];
        }

        #endregion
        #region Methods

        protected abstract void WriteElement(BinaryWriter writer, T value);

        protected abstract T ReadElement(BinaryReader reader);

        public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < _elements.Length; i++) yield return _elements[i];
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        protected override void Read(BinaryReader reader) {
            base.Read(reader);

            for (int i = 0; i < _elements.Length; i++) {
                _elements[i] = ReadElement(reader);
            }
        }

        protected override void Write(BinaryWriter writer) {
            base.Write(writer);

            for (int i = 0; i < _elements.Length; i++) {
                WriteElement(writer, _elements[i]);
            }
        }

        #endregion

        // Private
        #region Fields

        private int _elementSize;
        private T[] _elements;

        #endregion
    }
}
