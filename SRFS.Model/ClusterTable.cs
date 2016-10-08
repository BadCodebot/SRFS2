using SRFS.IO;
using SRFS.Model.Clusters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SRFS.Model {

    public interface IClusterTable<T> : IEnumerable<T> {

        IEnumerable<int> ClusterAddresses { get; }
        T this[int index] { get; set; }
        void Load(IClusterIO io);
        void Flush(IClusterIO io);
        void AddCluster(int clusterAddress);
        void RemoveLastCluster();
        int Count { get; }
    }

    public class ClusterTable<T> : IClusterTable<T> {

        // Public
        #region Constructors 

        public ClusterTable(IEnumerable<int> clusterAddresses, int elementSize, Func<int, ArrayCluster<T>> clusterFactory) {
            _clusterAddresses = clusterAddresses.ToList();

            _elementSize = elementSize;
            _elementsPerCluster = ArrayCluster.CalculateElementCount(elementSize);

            _isModified = new List<bool>(_elementsPerCluster * _clusterAddresses.Count);

            _elements = new List<T>(_elementsPerCluster * _clusterAddresses.Count);

            for (int i = 0; i < _elementsPerCluster * _clusterAddresses.Count; i++) {
                _elements.Add(default(T));
            }

            for (int i = 0; i < _clusterAddresses.Count; i++) {
                _isModified.Add(true);
                int clusterLogicalNumber = i;
            }

            _clusterFactory = clusterFactory;
        }

        #endregion
        #region Properties

        public IEnumerable<int> ClusterAddresses {
            get {
                return _clusterAddresses.AsReadOnly();
            }
        }

        public T this[int index] {
            get {
                return _elements[index];
            }
            set {
                int cluster = index / _elementsPerCluster;
                _elements[index] = value;
                _isModified[cluster] = true;
            }
        }

        #endregion
        #region Methods

        public void Load(IClusterIO io) {

            for (int i = 0; i < _clusterAddresses.Count; i++) {
                ArrayCluster<T> c = _clusterFactory(_clusterAddresses[i]);
                io.Load(c);
                for (int j = 0; j < _elementsPerCluster; j++) {
                    int index = i * _elementsPerCluster + j;
                    _elements[index] = c[j];
                }
                _isModified[i] = false;
            }
        }

        public void Flush(IClusterIO io) {
            for (int i = 0; i < _clusterAddresses.Count; i++) {
                if (_isModified[i]) {
                    ArrayCluster<T> c = _clusterFactory(_clusterAddresses[i]);
                    c.Initialize();
                    c.NextClusterAddress = (i == _clusterAddresses.Count - 1) ? Constants.NoAddress : _clusterAddresses[i + 1];
                    for (int j = 0; j < _elementsPerCluster; j++) c[j] = _elements[i * _elementsPerCluster + j];
                    io.Save(c);
                    _isModified[i] = false;
                }
            }
        }

        public void AddCluster(int clusterAddress) {
            if (_clusterAddresses.Count > 0) _isModified[_isModified.Count - 1] = true;
            _clusterAddresses.Add(clusterAddress);
            _isModified.Add(true);
            for (int i = 0; i < _elementsPerCluster; i++) _elements.Add(default(T));
        }

        public void RemoveLastCluster() {
            if (_clusterAddresses.Count == 0) throw new InvalidOperationException();
            _clusterAddresses.RemoveAt(_clusterAddresses.Count - 1);
            _isModified.RemoveAt(_isModified.Count - 1);

            int start = (_clusterAddresses.Count - 1) * _elementsPerCluster;
            _elements.RemoveRange(start, _elementsPerCluster);

            _isModified[_isModified.Count - 1] = true;
        }

        public IEnumerator<T> GetEnumerator() {
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int Count => _elements.Count;

        #endregion

        // Private
        #region Fields

        private readonly List<T> _elements;
        private readonly List<int> _clusterAddresses;
        private readonly List<bool> _isModified;
        private readonly int _elementSize;
        private readonly int _elementsPerCluster;

        private Func<int, ArrayCluster<T>> _clusterFactory;

        #endregion
    }

    public class MutableObjectClusterTable<T> : IClusterTable<T> where T : INotifyChanged {

        // Public
        #region Constructors 

        public MutableObjectClusterTable(IEnumerable<int> clusterAddresses, int elementSize, Func<int, ArrayCluster<T>> clusterFactory) {
            _clusterAddresses = clusterAddresses.ToList();

            _elementSize = elementSize;
            _elementsPerCluster = ArrayCluster.CalculateElementCount(elementSize + sizeof(bool));

            _isModified = new List<bool>(_elementsPerCluster * _clusterAddresses.Count);
            _notifyChangedDelegates = new List<ChangedEventHandler>(_elementsPerCluster * _clusterAddresses.Count);

            _elements = new List<T>(_elementsPerCluster * _clusterAddresses.Count);

            for (int i = 0; i < _elementsPerCluster * _clusterAddresses.Count; i++) {
                _elements.Add(default(T));
            }

            for (int i = 0; i < _clusterAddresses.Count; i++) {
                _isModified.Add(true);
                int clusterLogicalNumber = i;
                _notifyChangedDelegates.Add((sender) => { _isModified[clusterLogicalNumber] = true; });
            }

            _clusterFactory = clusterFactory;
        }

        #endregion
        #region Properties

        public IEnumerable<int> ClusterAddresses {
            get {
                return _clusterAddresses.AsReadOnly();
            }
        }

        public T this[int index] {
            get {
                return _elements[index];
            }
            set {
                int cluster = index / _elementsPerCluster;
                if (_elements[index] != null) _elements[index].Changed -= _notifyChangedDelegates[cluster];
                _elements[index] = value;
                _isModified[cluster] = true;
                if (value != null) value.Changed += _notifyChangedDelegates[cluster];
            }
        }

        #endregion
        #region Methods

        public void Load(IClusterIO io) {

            for (int i = 0; i < _clusterAddresses.Count; i++) {
                ArrayCluster<T> c = _clusterFactory(_clusterAddresses[i]);
                io.Load(c);
                for (int j = 0; j < _elementsPerCluster; j++) {
                    int index = i * _elementsPerCluster + j;

                    if (_elements[index] != null) _elements[index].Changed -= _notifyChangedDelegates[i];
                    _elements[index] = c[j];
                    if (_elements[index] != null) _elements[index].Changed += _notifyChangedDelegates[i];
                }
                _isModified[i] = false;
            }
        }

        public void Flush(IClusterIO io) {
            for (int i = 0; i < _clusterAddresses.Count; i++) {
                if (_isModified[i]) {
                    ArrayCluster<T> c = _clusterFactory(_clusterAddresses[i]);
                    c.NextClusterAddress = (i == _clusterAddresses.Count - 1) ? Constants.NoAddress : _clusterAddresses[i + 1];
                    for (int j = 0; j < _elementsPerCluster; j++) c[j] = _elements[i * _elementsPerCluster + j];
                    io.Save(c);
                    _isModified[i] = false;
                }
            }
        }

        public void AddCluster(int clusterAddress) {
            _clusterAddresses.Add(clusterAddress);
            _isModified.Add(true);
            for (int i = 0; i < _elementsPerCluster; i++) _elements.Add(default(T));
            int cluster = _notifyChangedDelegates.Count;
            _notifyChangedDelegates.Add((sender) => { _isModified[cluster] = true; });
        }

        public void RemoveLastCluster() {
            if (_clusterAddresses.Count == 0) throw new InvalidOperationException();
            _clusterAddresses.RemoveAt(_clusterAddresses.Count - 1);
            _isModified.RemoveAt(_isModified.Count - 1);
            ChangedEventHandler h = _notifyChangedDelegates[_notifyChangedDelegates.Count - 1];
            _notifyChangedDelegates.RemoveAt(_notifyChangedDelegates.Count - 1);

            int start = (_clusterAddresses.Count - 1) * _elementsPerCluster;
            for (int i = start; i < start + _elementsPerCluster; i++) if (_elements[i] != null) _elements[i].Changed -= h;
            _elements.RemoveRange(start, _elementsPerCluster);
        }

        public IEnumerator<T> GetEnumerator() {
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int Count => _elements.Count;

        #endregion

        // Private
        #region Fields

        private readonly List<T> _elements;
        private readonly List<int> _clusterAddresses;
        private readonly List<ChangedEventHandler> _notifyChangedDelegates;
        private readonly List<bool> _isModified;
        private readonly int _elementSize;
        private readonly int _elementsPerCluster;

        private Func<int, ArrayCluster<T>> _clusterFactory;

        #endregion
    }
}
