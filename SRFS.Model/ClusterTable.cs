using SRFS.IO;
using SRFS.Model.Clusters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SRFS.Model {

    public class ClusterTable<T> : IEnumerable<T> {

        // Public
        #region Constructors 

        public ClusterTable(IEnumerable<ArrayCluster<T>> clusters, int sizeOfElement) {
            _clusters = clusters.ToList();
            _sizeOfElement = sizeOfElement;
            _elementsPerCluster = ArrayCluster.CalculateElementCount(sizeOfElement);

            _isModified = new List<bool>();

            for (int i = 0; i < _clusters.Count; i++) {
                if (i > 0) _clusters[i - 1].NextClusterAddress = _clusters[i].Address;
                _isModified.Add(true);
            }
            _clusters[_clusters.Count - 1].NextClusterAddress = Constants.NoAddress;
        }

        #endregion
        #region Properties

        public IEnumerable<ArrayCluster<T>> Clusters {
            get {
                return _clusters.AsReadOnly();
            }
        }

        public T this[int index] {
            get {
                var address = GetIndexLocation(index);
                return _clusters[address.cluster][address.offset];
            }
            set {
                var address = GetIndexLocation(index);
                _clusters[address.cluster][address.offset] = value;
                _isModified[address.cluster] = true;
            }
        }

        #endregion
        #region Methods

        public void Load(IBlockIO io) {

            for (int i = 0; i < _clusters.Count; i++) {
                _clusters[i].Load(io);
                _isModified[i] = false;
            }
        }

        public void Flush(IBlockIO io) {
            for (int i = 0; i < _clusters.Count; i++) {
                if (_isModified[i]) {
                    _clusters[i].Save(io);
                    _isModified[i] = false;
                }
            }
        }

        public void AddCluster(ArrayCluster<T> cluster) {
            if (_clusters.Count != 0) {
                _clusters[_clusters.Count - 1].NextClusterAddress = cluster.Address;
                _isModified[_isModified.Count - 1] = true;
            }

            cluster.NextClusterAddress = Constants.NoAddress;
            _clusters.Add(cluster);
            _isModified.Add(true);
        }

        public void RemoveLastCluster() {
            if (_clusters.Count == 0) throw new InvalidOperationException();
            if (_clusters.Count == 1) {
                _clusters.Clear();
                _isModified.Clear();
            } else {
                _clusters.RemoveAt(_clusters.Count - 1);
                _isModified.RemoveAt(_isModified.Count - 1);

                _clusters[_clusters.Count - 1].NextClusterAddress = Constants.NoAddress;
                _isModified[_isModified.Count - 1] = true;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            int n = 0;
            for (int i = 0; i < _clusters.Count; i++) {
                for (int j = 0; j < _elementsPerCluster && n < Count; j++, n++) {
                    yield return _clusters[i][j];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int Count => _clusters.Count * _elementsPerCluster;

        #endregion

        // Private
        #region Methods

        private (int cluster, int offset) GetIndexLocation(int index) {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException();
            return (index / _elementsPerCluster, index % _elementsPerCluster);
        }

        #endregion
        #region Fields

        private readonly List<ArrayCluster<T>> _clusters;
        private readonly List<bool> _isModified;

        private readonly int _sizeOfElement;

        private readonly int _elementsPerCluster;

        #endregion
    }
}
