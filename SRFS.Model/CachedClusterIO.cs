//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using SRFS.Model.Clusters;
//using SRFS.IO;
//using System.Threading;

//namespace SRFS.Model {
//    public class CachedClusterIO : IClusterIO {

//        private const int PrimaryCacheSize = 100;
//        private const int MonitorWriteTimeout = 5000;
//        private const int MonitorReadTimeout = 1000;

//        public CachedClusterIO(IBlockIO io, int skipBytes = 0, FileSystem fileSystem) {
//            if (io == null) throw new ArgumentNullException();
//            if (Configuration.Geometry.BytesPerCluster % io.BlockSizeBytes != 0) throw new ArgumentException();
//            if (skipBytes % io.BlockSizeBytes != 0) throw new ArgumentException();
//            if (skipBytes < 0) throw new ArgumentOutOfRangeException();

//            _skipBytes = skipBytes;
//            _io = io;
//            _buffer = new byte[Configuration.Geometry.BytesPerCluster];
//        }

//        private Cluster getLastClusterBeforeTime(int address, DateTime time) {
//            // The thread should have the lock on _primaryCache before calling

//            SortedList<DateTime, Cluster> clusters = null;
//            Cluster c = null;

//            if (_primaryCache.TryGetValue(address, out clusters)) {
//                // todo: Replace with binary search
//                int i;
//                for (i = clusters.Count - 1; i >= 0 && clusters.Keys[i] > time; i--) ;
//                if (i >= 0) c = clusters.Values[i];
//            }

//            return c;
//        }

//        private object createReadRequest(int address, DateTime requestTime) {
//            SortedList<DateTime, object> requests = null;
//            if (!_readRequests.TryGetValue(address, out requests)) {
//                requests = new SortedList<DateTime, object>();
//                _readRequests.Add(address, requests);
//            }

//            object requestLock = new object();
//            requests.Add(requestTime, requestLock);

//            return requestLock;
//        }

//        private void addClusterToCache(Cluster cluster, DateTime writeTime) {
//            SortedList<DateTime, Cluster> r = null;
//            if (!_primaryCache.TryGetValue(cluster.Address, out r)) {
//                r = new SortedList<DateTime, Cluster>();
//                _primaryCache.Add(cluster.Address, r);
//                if (cluster.Address < _currentClusterAddress) _currentClusterAddress++;
//                _primaryCacheCount++;
//            }
//            r.Add(writeTime, cluster);
//        }

//        private void addBytesToWriteCache(byte[] bytes, int address, DateTime writeTime) {
//            SortedList<DateTime, byte[]> r = null;
//            if (!_writeCache.TryGetValue(address, out r)) {
//                r = new SortedList<DateTime, byte[]>();
//                _writeCache.Add(address, r);
//                if (address < _currentWriteClusterAddress) _currentWriteClusterAddress++;
//                _writeCacheCount++;
//            }
//            r.Add(writeTime, bytes);
//        }

//        private class DateTimeComparer<T> : IComparer<T> {
//            public DateTimeComparer(Func<T, DateTime> getter) {
//                _getter = getter;
//            }

//            public int Compare(T t1, T t2) {
//                return _getter(t1).CompareTo(_getter(t2));
//            }

//            private Func<T, DateTime> _getter;
//        }

//        public virtual Cluster Load(int address) {

//            DateTime requestTime = DateTime.UtcNow;
//            Cluster c = null;

//            if (_primaryCache.GetLowerInclusiveBound(address, requestTime, out c)) return c;
//            // It is possible that the cluster is added to the cache in between these two statements, in which case the 
//            // monitor below will need to timeout before it finds the cluster.  If this is a problem, we need to 
//            // prevent that, perhaps with another lock, or by checking the cache again after the read request is created.
//            object requestLock = createReadRequest(address, requestTime);

//            // Keep looping until we get the cluster
//            do {
//                lock (requestLock) Monitor.Wait(requestLock, MonitorReadTimeout);
//            } while (!_primaryCache.GetLowerInclusiveBound(address, requestTime, out c));

//            return c;
//        }

//        public virtual void Save(Cluster c) {

//            _primaryCache.Add(c.Address, DateTime.UtcNow, c.Clone());

//            foreach (var requestLock in from x in _readRequests.GetAll(c.Address) select x.Value)
//                lock (requestLock) Monitor.PulseAll(requestLock);
//        }

//        public void packWorker(Cluster c) {
//            byte[] buffer = new byte[c.SizeBytes];
//            c.Save(buffer, 0);
//        }

//        private ADTContainer<Cluster> _primaryCache;
//        private ADTContainer<object> _readRequests;

//        private object _writeCacheLock = new object();
//        private SortedList<int, SortedList<DateTime, byte[]>> _writeCache;
//        private int _currentWriteClusterAddress;
//        private int _writeCacheCount;

//        private object _lock = new object();

//        private IBlockIO _io;
//        private long _skipBytes;


//        public class ADTContainer<T> {

//            public ADTContainer(int maxCount) {
//                _lock = new object();
//                _items = new SortedList<int, SortedList<DateTime, T>>();
//                _count = 0;
//                _maxCount = maxCount;
//            }

//            public void Add(int address, DateTime time, T item) {
//                lock (_lock) {
//                    if (_count == _maxCount) Monitor.Wait(_lock, 1000);
//                    SortedList<DateTime, T> r = null;
//                    if (!_items.TryGetValue(address, out r)) {
//                        r = new SortedList<DateTime, T>();
//                        _items.Add(address, r);
//                        _count++;
//                    }
//                    r.Add(time, item);
//                }
//            }

//            public bool GetLowerInclusiveBound(int address, DateTime time, out T item) {
//                DateTime[] times;
//                T[] values;

//                lock (_lock) {
//                    SortedList<DateTime, T> addressItems = null;
//                    if (!_items.TryGetValue(address, out addressItems)) {
//                        item = default(T);
//                        return false;
//                    }

//                    if (addressItems.Count == 0) {
//                        item = default(T);
//                        return false;
//                    } else if (addressItems.Count == 1) {
//                        if (addressItems.Keys[0] < time) {
//                            item = addressItems.Values[0];
//                            return true;
//                        } else {
//                            item = default(T);
//                            return false;
//                        }
//                    }

//                    times = addressItems.Keys.ToArray();
//                    values = addressItems.Values.ToArray();
//                }

//                int index = Array.BinarySearch(times, time);
//                if (index >= 0) {
//                    item = values[index];
//                    return true;
//                } else {
//                    index = ~index;
//                    if (index == 0) {
//                        item = default(T);
//                        return false;
//                    } else {
//                        item = values[index - 1];
//                        return true;
//                    }
//                }
//            }

//            public KeyValuePair<DateTime, T>[] GetAll(int address) {
//                lock (_lock) {
//                    SortedList<DateTime, T> r = null;
//                    if (!_items.TryGetValue(address, out r)) return new KeyValuePair<DateTime, T>[0];
//                    return r.ToArray();
//                }
//            }

//            public void Remove(int address, DateTime time) {
//                lock (_lock) {
//                    SortedList<DateTime, T> r = null;
//                    if (!_items.TryGetValue(address, out r)) return;
//                    r.Remove(time);
//                    if (r.Count == 0) _items.Remove(address);

//                    Monitor.PulseAll(_lock);
//                }
//            }

//            private object _lock;
//            private SortedList<int, SortedList<DateTime, T>> _items;
//            private int _count;
//            private int _maxCount;
//        }
//    }
//}
