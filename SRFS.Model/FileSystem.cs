using SRFS.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using IOException = System.IO.IOException;
using Path = System.IO.Path;
using DirectoryNotFoundException = System.IO.DirectoryNotFoundException;
using System.Diagnostics;
using System.Threading;
using Stream = System.IO.Stream;
using SRFS.Model.Clusters;
using SeekOrigin = System.IO.SeekOrigin;
using BinaryWriter = System.IO.BinaryWriter;
using BinaryReader = System.IO.BinaryReader;
using SRFS.Model.Data;
using System.Linq;
using log4net;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Collections.ObjectModel;

namespace SRFS.Model {

    public class FileSystem : IDisposable {

        #region Construction / Destruction

        private static readonly ILog Log = LogManager.GetLogger(typeof(FileSystem));

        public static FileSystem Create() {
            Log.Info($"Creating FileSystem volumeName={Configuration.VolumeName}, " +
                $"partition={Configuration.Partition.DeviceID}, id={Configuration.FileSystemID}, " +
                $"bytesPerCluster={Configuration.Geometry.BytesPerCluster.ToFileSize()}");

            FileSystem fileSystem = new FileSystem();
            fileSystem.create();
            return fileSystem;
        }

        public static FileSystem Mount(Partition partitionInfo, CryptoSettings cryptoSettings, Options options) {
            Log.Info($"Creating FileSystem partition={partitionInfo.DeviceID}");

            FileSystem fileSystem = new FileSystem();
            fileSystem.mount();
            return fileSystem;
        }

        private FileSystem() {
            _freeClusterSearchStart = 0;

            _nextEntryID = 0;

            _nextDirectoryIndex = 0;
            _nextFileIndex = 0;
            _nextAccessRuleIndex = 0;
            _nextAuditRuleIndex = 0;

            _directoryIndex = new Dictionary<int, int>();

            _containedDirectoriesIndex = new Dictionary<int, SortedList<string, DirectoryEntry>>();
            _containedFilesIndex = new Dictionary<int, SortedList<string, FileEntry>>();

            _fileIndex = new Dictionary<int, int>();

            _accessRulesIndex = new Dictionary<int, List<int>>();

            _auditRulesIndex = new Dictionary<int, List<int>>();
        }

        private void create() {

            // Create IO Devices
            _partitionIO = new PartitionIO(Configuration.Partition);
            _blockIO = new SuperBlockIO(_partitionIO, Configuration.Geometry.BytesPerCluster, PartitionHeaderCluster.ClusterSize);

            // Write the Partition Cluster
            PartitionHeaderCluster partitionCluster = new PartitionHeaderCluster();
            partitionCluster.Clear();
            partitionCluster.Save(_partitionIO);

            // Cluster State Table
            int entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            int clusterCount = (entryCount + ClusterStatesCluster.ElementsPerCluster - 1) / ClusterStatesCluster.ElementsPerCluster;

            _clusterStateTable = new ClusterTable<ClusterState>(
                (from n in Enumerable.Range(0, clusterCount)
                 select new ClusterStatesCluster() { Address = n }),
                sizeof(byte));

            // Next Cluster Address Table
            entryCount = Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + IntArrayCluster.ElementsPerCluster - 1) / IntArrayCluster.ElementsPerCluster;

            _nextClusterAddressTable = new ClusterTable<int>(
                (from n in Enumerable.Range(_clusterStateTable.Clusters.Last().Address + 1, clusterCount)
                 select new IntArrayCluster() { Address = n, Type = ClusterType.NextClusterAddressTable }),
                sizeof(int));

            // Bytes Used Table
            _bytesUsedTable = new ClusterTable<int>(
                (from n in Enumerable.Range(_nextClusterAddressTable.Clusters.Last().Address + 1, clusterCount)
                 select new IntArrayCluster() { Address = n, Type = ClusterType.BytesUsedTable }),
                sizeof(int));

            entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + VerifyTimesCluster.ElementsPerCluster - 1) / VerifyTimesCluster.ElementsPerCluster;

            // Verify Time Table
            _verifyTimeTable = new ClusterTable<DateTime>(
                (from n in Enumerable.Range(_bytesUsedTable.Clusters.Last().Address + 1, clusterCount)
                 select new VerifyTimesCluster() { Address = n }),
                sizeof(long));

            // Directory Table
            _directoryTable = new ClusterTable<DirectoryEntry>(
                new DirectoryEntryCluster[] { new DirectoryEntryCluster() {
                    Address = _verifyTimeTable.Clusters.Last().Address + 1 } },
                DirectoryEntryCluster.ElementLength);

            // File Table
            _fileTable = new ClusterTable<FileEntry>(
                new FileEntryCluster[] { new FileEntryCluster() {
                    Address = _directoryTable.Clusters.Last().Address + 1 } },
                FileEntryCluster.ElementLength);

            // Access Rules Table
            _accessRules = new ClusterTable<FileSystemAccessRuleData>(
                new AccessRuleCluster[] { new AccessRuleCluster() {
                    Address = _fileTable.Clusters.Last().Address + 1 } },
                AccessRuleCluster.EntryLength);

            // Audit Rules Table
            _auditRules = new ClusterTable<FileSystemAuditRuleData>(
                new AuditRuleCluster[] { new AuditRuleCluster() {
                    Address = _accessRules.Clusters.Last().Address + 1 } },
                AuditRuleCluster.EntryLength);

            // Initialize the tables
            for (int i = 0; i < _clusterStateTable.Count; i++) _clusterStateTable[i] = ClusterState.Unused;
            for (int i = 0; i < _nextClusterAddressTable.Count; i++) _nextClusterAddressTable[i] = Constants.NoAddress;
            for (int i = 0; i < _bytesUsedTable.Count; i++) _bytesUsedTable[i] = 0;
            for (int i = 0; i < _verifyTimeTable.Count; i++) _verifyTimeTable[i] = DateTime.MinValue;
            for (int i = 0; i < _directoryTable.Count; i++) _directoryTable[i] = null;
            for (int i = 0; i < _fileTable.Count; i++) _fileTable[i] = null;
            for (int i = 0; i < _accessRules.Count; i++) _accessRules[i] = null;
            for (int i = 0; i < _auditRules.Count; i++) _auditRules[i] = null;

            // Update the cluster state and next cluster address tables
            foreach (var t in new IEnumerable<int>[] {
                from c in _clusterStateTable.Clusters select c.Address,
                from c in _nextClusterAddressTable.Clusters select c.Address,
                from c in _bytesUsedTable.Clusters select c.Address,
                from c in _verifyTimeTable.Clusters select c.Address,
                from c in _directoryTable.Clusters select c.Address,
                from c in _fileTable.Clusters select c.Address,
                from c in _accessRules.Clusters select c.Address,
                from c in _auditRules.Clusters select c.Address
            }) {
                foreach (var n in t) {
                    _clusterStateTable[n] = ClusterState.System | ClusterState.Used;
                    _nextClusterAddressTable[n] = n + 1;
                }
                _nextClusterAddressTable[t.Last()] = Constants.NoAddress;
            }

            // Create the root directory
            DirectoryEntry dir = CreateDirectory(null, "");
            AddAccessRule(dir, 
                new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, 
                AccessControlType.Allow));

            Flush();
        }

        private void mount() {

            // Create IO Devices
            _partitionIO = new PartitionIO(Configuration.Partition);
            _blockIO = new SuperBlockIO(_partitionIO, Configuration.Geometry.BytesPerCluster, PartitionHeaderCluster.ClusterSize);

            // Read the Partition Cluster
            PartitionHeaderCluster partitionCluster = new PartitionHeaderCluster();
            partitionCluster.Load(_partitionIO);

            Configuration.VolumeName = partitionCluster.VolumeName;
            Configuration.Geometry = new Geometry(partitionCluster.BytesPerCluster, partitionCluster.ClustersPerTrack, partitionCluster.DataClustersPerTrack,
                partitionCluster.TrackCount);

            // Cluster State Table
            int entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            int clusterCount = (entryCount + ClusterStatesCluster.ElementsPerCluster - 1) / ClusterStatesCluster.ElementsPerCluster;

            _clusterStateTable = new ClusterTable<ClusterState>(
                (from n in Enumerable.Range(0, clusterCount)
                 select new ClusterStatesCluster() { Address = n }),
                sizeof(byte));
            _clusterStateTable.Load(_blockIO);

            // Next Cluster Address Table
            entryCount = Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + IntArrayCluster.ElementsPerCluster - 1) / IntArrayCluster.ElementsPerCluster;

            _nextClusterAddressTable = new ClusterTable<int>(
                (from n in Enumerable.Range(_clusterStateTable.Clusters.Last().Address + 1, clusterCount)
                 select new IntArrayCluster() { Address = n, Type = ClusterType.NextClusterAddressTable }),
                sizeof(int));
            _nextClusterAddressTable.Load(_blockIO);

            // Bytes Used Table
            _bytesUsedTable = new ClusterTable<int>(
                (from n in Enumerable.Range(_nextClusterAddressTable.Clusters.Last().Address + 1, clusterCount)
                 select new IntArrayCluster() { Address = n, Type = ClusterType.BytesUsedTable }),
                sizeof(int));
            _bytesUsedTable.Load(_blockIO);

            entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + VerifyTimesCluster.ElementsPerCluster - 1) / VerifyTimesCluster.ElementsPerCluster;

            // Verify Time Table
            _verifyTimeTable = new ClusterTable<DateTime>(
                (from n in Enumerable.Range(_bytesUsedTable.Clusters.Last().Address + 1, clusterCount)
                 select new VerifyTimesCluster() { Address = n }),
                sizeof(long));
            _verifyTimeTable.Load(_blockIO);

            // Directory Table
            _directoryTable = new ClusterTable<DirectoryEntry>(
                from n in getClusterChain(_verifyTimeTable.Clusters.Last().Address + 1)
                select new DirectoryEntryCluster() { Address = n },
                DirectoryEntryCluster.ElementLength);
            _directoryTable.Load(_blockIO);

            // File Table
            _fileTable = new ClusterTable<FileEntry>(
                from n in getClusterChain(_directoryTable.Clusters.First().Address + 1)
                select new FileEntryCluster() { Address = n },
                FileEntryCluster.ElementLength);
            _fileTable.Load(_blockIO);

            // Access Rules Table
            _accessRules = new ClusterTable<FileSystemAccessRuleData>(
                from n in getClusterChain(_fileTable.Clusters.First().Address + 1)
                select new AccessRuleCluster() { Address = n },
                AccessRuleCluster.EntryLength);
            _accessRules.Load(_blockIO);

            // Audit Rules Table
            _auditRules = new ClusterTable<FileSystemAuditRuleData>(
                from n in getClusterChain(_accessRules.Clusters.First().Address + 1)
                select new AuditRuleCluster() { Address = n },
                AuditRuleCluster.EntryLength);
            _auditRules.Load(_blockIO);

            // Create the Indices
            for (int i = 0; i < _directoryTable.Count; i++) {
                DirectoryEntry d = _directoryTable[i];
                if (d != null) {
                    _directoryIndex.Add(d.ID, i);
                    _containedDirectoriesIndex.Add(d.ID, new SortedList<string, DirectoryEntry>(StringComparer.OrdinalIgnoreCase));
                    _containedFilesIndex.Add(d.ID, new SortedList<string, FileEntry>(StringComparer.OrdinalIgnoreCase));
                    _accessRulesIndex.Add(d.ID, new List<int>());
                    _auditRulesIndex.Add(d.ID, new List<int>());
                }
            }

            foreach (var d in from d in _directoryTable where d != null && d.ParentID != Constants.NoID select d) {
                _containedDirectoriesIndex[d.ParentID].Add(d.Name, d);
            }

            for (int i = 0; i < _fileTable.Count; i++) {
                FileEntry f = _fileTable[i];
                if (f != null) {
                    _fileIndex.Add(f.ID, i);
                    _containedFilesIndex[f.ParentID].Add(f.Name, f);
                    _accessRulesIndex.Add(f.ID, new List<int>());
                    _auditRulesIndex.Add(f.ID, new List<int>());
                }
            }

            for (int i = 0; i < _accessRules.Count; i++) {
                FileSystemAccessRuleData r = _accessRules[i];
                if (r != null) _accessRulesIndex[r.ID].Add(i);
            }

            for (int i = 0; i < _auditRules.Count; i++) {
                FileSystemAuditRuleData r = _auditRules[i];
                if (r != null) _auditRulesIndex[r.ID].Add(i);
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    Flush();
                    _blockIO.Dispose();
                    _partitionIO.Dispose();
                }
                _isDisposed = true;
            }
        }

        private IEnumerable<int> getClusterChain(int startingClusterNumber) {
            while (startingClusterNumber != Constants.NoAddress) {
                yield return startingClusterNumber;
                startingClusterNumber = GetNextClusterNumber(startingClusterNumber);
            }
        }

        public void Dispose() => Dispose(true);

        public IDictionary<string, DirectoryEntry> GetContainedDirectories(DirectoryEntry dir) {
            return new ReadOnlyDictionary<string, DirectoryEntry>(_containedDirectoriesIndex[dir.ID]);
        }

        public IDictionary<string, FileEntry> GetContainedFiles(DirectoryEntry dir) {
            return new ReadOnlyDictionary<string, FileEntry>(_containedFilesIndex[dir.ID]);
        }

        public IEnumerable<FileSystemAccessRule> GetAccessRules(FileSystemEntry entry) {
            return (from i in _accessRulesIndex[entry.ID] select _accessRules[i].Rule);
        }

        public IEnumerable<FileSystemAuditRule> GetAuditRules(FileSystemEntry entry) {
            return (from i in _auditRulesIndex[entry.ID] select _auditRules[i].Rule);
        }

        public void MoveDirectory(DirectoryEntry dir, DirectoryEntry newDirectory) {
            _containedDirectoriesIndex[dir.ParentID].Remove(dir.Name);
            _containedDirectoriesIndex[newDirectory.ID].Add(dir.Name, dir);
            dir.ParentID = newDirectory.ID;
        }

        public DirectoryEntry CreateDirectory(DirectoryEntry parent, string name) {
            if (parent == null && _directoryIndex.Count != 0) throw new ArgumentException();

            if (parent != null) {
                if (GetContainedDirectories(parent).ContainsKey(name)) throw new ArgumentException();
                if (GetContainedFiles(parent).ContainsKey(name)) throw new ArgumentException();
            }

            DirectoryEntry dir = new DirectoryEntry(_nextEntryID++, name);
            if (parent != null) _containedDirectoriesIndex[parent.ID].Add(name, dir);
            int index = getFreeDirectoryIndex();
            _directoryTable[index] = dir;
            _directoryIndex.Add(dir.ID, index);

            _containedDirectoriesIndex.Add(dir.ID, new SortedList<string, DirectoryEntry>(StringComparer.OrdinalIgnoreCase));
            _containedFilesIndex.Add(dir.ID, new SortedList<string, FileEntry>(StringComparer.OrdinalIgnoreCase));
            _accessRulesIndex.Add(dir.ID, new List<int>());
            _auditRulesIndex.Add(dir.ID, new List<int>());

            return dir;
        }

        public void RemoveDirectory(DirectoryEntry dir) {
            if (_containedDirectoriesIndex[dir.ID].Count > 0) throw new InvalidOperationException();
            if (_containedFilesIndex[dir.ID].Count > 0) throw new InvalidOperationException();

            _containedDirectoriesIndex[dir.ParentID].Remove(dir.Name);
            _containedDirectoriesIndex.Remove(dir.ID);
            _containedFilesIndex.Remove(dir.ID);
            int index = _directoryIndex[dir.ID];
            _directoryIndex.Remove(dir.ID);
            _directoryTable[index] = null;

            foreach (var r in _accessRulesIndex[dir.ID]) _accessRules[r] = null;
            _accessRulesIndex.Remove(dir.ID);

            foreach (var r in _auditRulesIndex[dir.ID]) _auditRules[r] = null;
            _auditRulesIndex.Remove(dir.ID);

            if (index < _nextDirectoryIndex) _nextDirectoryIndex = index;
        }

        public void RemoveFile(FileEntry file) {
            lock (_lock) {
                List<int> clusters = getClusterChain(file.FirstCluster).ToList();
                foreach (var cluster in clusters) DeallocateCluster(cluster);
            }

            _containedFilesIndex[file.ParentID].Remove(file.Name);
            int index = _fileIndex[file.ID];
            _fileIndex.Remove(file.ID);
            _fileTable[index] = null;

            foreach (var r in _accessRulesIndex[file.ID]) _accessRules[r] = null;
            _accessRulesIndex.Remove(file.ID);

            foreach (var r in _auditRulesIndex[file.ID]) _auditRules[r] = null;
            _auditRulesIndex.Remove(file.ID);

            if (index < _nextFileIndex) _nextFileIndex = index;
        }

        public void AddAccessRule(FileSystemEntry entry, FileSystemAccessRule rule) {
            int index = getFreeAccessRuleIndex();
            _accessRules[index] = new FileSystemAccessRuleData(
                entry is DirectoryEntry ? FileSystemObjectType.Directory : FileSystemObjectType.File, entry.ID, rule);
            _accessRulesIndex[entry.ID].Add(index);
        }

        #endregion
        #region Methods

        public ClusterState GetClusterState(int absoluteClusterNumber) {
            lock (_lock) return _clusterStateTable[absoluteClusterNumber];
        }

        public void SetClusterState(int absoluteClusterNumber, ClusterState value) {
            lock (_lock) _clusterStateTable[absoluteClusterNumber] = value;
        }

        public int GetBytesUsed(int absoluteClusterNumber) {
            lock (_lock) return _bytesUsedTable[absoluteClusterNumber];
        }

        public void SetBytesUsed(int absoluteClusterNumber, int value) {
            lock (_lock) _bytesUsedTable[absoluteClusterNumber] = value;
        }

        public int GetNextClusterNumber(int absoluteClusterNumber) {
            lock (_lock) return _nextClusterAddressTable[absoluteClusterNumber];
        }

        public void SetNextClusterAddress(int absoluteClusterNumber, int value) {
            lock (_lock) _nextClusterAddressTable[absoluteClusterNumber] = value;
        }

        private DateTime GetVerifyTime(int absoluteClusterNumber) {
            lock (_lock) return _verifyTimeTable[absoluteClusterNumber];
        }

        private void SetVerifyTime(int absoluteClusterNumber, DateTime value) {
            lock (_lock) _verifyTimeTable[absoluteClusterNumber] = value;
        }

        public int AllocateCluster() {
            lock (_lock) {
                for (int i = _freeClusterSearchStart; i < Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount; i++) {
                    if (_clusterStateTable[i] == ClusterState.Unused) {
                        _freeClusterSearchStart = i + 1;
                        SetClusterState(i, ClusterState.Used);
                        SetNextClusterAddress(i, Constants.NoAddress);
                        SetBytesUsed(i, 0);
                        return i;
                    }
                }
                throw new IOException("Out of space");
            }
        }

        public IEnumerable<int> AllocateClusters(int n) {
            for (int i = 0; i < n; i++) yield return AllocateCluster();
        }

        public void DeallocateCluster(int absoluteClusterNumber) {
            lock (_lock) {
                SetClusterState(absoluteClusterNumber, GetClusterState(absoluteClusterNumber) & ~ClusterState.Used);
                SetNextClusterAddress(absoluteClusterNumber, Constants.NoAddress);
                SetBytesUsed(absoluteClusterNumber, 0);
                if (absoluteClusterNumber < _freeClusterSearchStart) _freeClusterSearchStart = absoluteClusterNumber;
            }
        }

        public FileSystemEntry GetFileSystemObject(string path) {
            string[] components = path.Split(Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            if (components.Length < 2) throw new IOException($"Incomplete path '{path}'");
            DirectoryEntry dir = (DirectoryEntry)_directoryTable[0];
            for (int i = 1; i < components.Length - 1; i++) {
                if (!_containedDirectoriesIndex[dir.ID].TryGetValue(components[i], out dir))
                    throw new DirectoryNotFoundException($"Directory component {i} not found in '{path}'");
            }

            if (components[components.Length - 1] == string.Empty) return dir;

            DirectoryEntry d;
            if (_containedDirectoriesIndex[dir.ID].TryGetValue(components[components.Length - 1], out d)) return d;
            FileEntry f;
            if (_containedFilesIndex[dir.ID].TryGetValue(components[components.Length - 1], out f)) return f;

            return null;
        }

        public DirectoryEntry GetParentDirectory(string path, out string name) {
            string[] components = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (components.Length < 2) throw new IOException($"Incomplete path '{path}'");
            DirectoryEntry dir = _directoryTable[0];
            for (int i = 1; i < components.Length - 1; i++) {
                if (!_containedDirectoriesIndex[dir.ID].TryGetValue(components[i], out dir)) throw new DirectoryNotFoundException($"Directory component {i} not found in '{path}'");
            }
            name = components[components.Length - 1];
            return dir;
        }

        public void Flush() {

            _clusterStateTable.Flush(_blockIO);
            _nextClusterAddressTable.Flush(_blockIO);
            _bytesUsedTable.Flush(_blockIO);
            _verifyTimeTable.Flush(_blockIO);

            _directoryTable.Flush(_blockIO);
            _fileTable.Flush(_blockIO);
            _accessRules.Flush(_blockIO);
            _auditRules.Flush(_blockIO);
        }

        #endregion
        #region Properties

        public long TotalNumberOfBytes {
            get {
                return (long)(Configuration.Geometry.BytesPerCluster - FileDataCluster.HeaderLength) * Configuration.Geometry.TrackCount;
            }
        }

        public long TotalNumberOfFreeBytes {
            get {
                lock (_lock) {
                    long freeClusters = (from c in _clusterStateTable where c != ClusterState.Used select c).Count();
                    return freeClusters * (Configuration.Geometry.BytesPerCluster - FileDataCluster.HeaderLength);
                }
            }
        }

        #endregion
        #region Private Fields

        private int getFreeDirectoryIndex() {
            return getFreeIndex(ref _nextDirectoryIndex, _directoryTable, n => new DirectoryEntryCluster() { Address = n });
        }

        private int getFreeFileIndex() {
            return getFreeIndex(ref _nextFileIndex, _fileTable, n => new FileEntryCluster() { Address = n });
        }

        private int getFreeAccessRuleIndex() {
            return getFreeIndex(ref _nextAccessRuleIndex, _accessRules, n => new AccessRuleCluster() { Address = n });
        }

        private int getFreeAuditRuleIndex() {
            return getFreeIndex(ref _nextAuditRuleIndex, _auditRules, n => new AuditRuleCluster() { Address = n });
        }

        private int getFreeIndex<T>(ref int nextIndex, ClusterTable<T> table, Func<int, ArrayCluster<T>> clusterFactory) {
            int i = nextIndex;
            while (table[i] != null) {
                i++;
                if (i >= table.Count) {
                    int newClusterNumber = AllocateCluster();
                    _clusterStateTable[newClusterNumber] |= ClusterState.System;
                    ArrayCluster<T> c = clusterFactory(newClusterNumber);
                    table.AddCluster(c);
                }
            }
            nextIndex = i + 1;
            return i;
        }

        private int _nextEntryID;
        private int _freeClusterSearchStart;
        private int _nextDirectoryIndex;
        private int _nextFileIndex;
        private int _nextAccessRuleIndex;
        private int _nextAuditRuleIndex;

        private ClusterTable<ClusterState> _clusterStateTable;
        private ClusterTable<int> _nextClusterAddressTable;
        private ClusterTable<int> _bytesUsedTable;
        private ClusterTable<DateTime> _verifyTimeTable;

        private ClusterTable<DirectoryEntry> _directoryTable;
        private Dictionary<int, int> _directoryIndex;

        private Dictionary<int, SortedList<string, DirectoryEntry>> _containedDirectoriesIndex;
        private Dictionary<int, SortedList<string, FileEntry>> _containedFilesIndex;

        private ClusterTable<FileEntry> _fileTable;
        private Dictionary<int, int> _fileIndex;

        private ClusterTable<FileSystemAccessRuleData> _accessRules;
        private Dictionary<int, List<int>> _accessRulesIndex;

        private ClusterTable<FileSystemAuditRuleData> _auditRules;
        private Dictionary<int, List<int>> _auditRulesIndex;

        private PartitionIO _partitionIO;
        private SuperBlockIO _blockIO;

        private object _lock = new object();

        private bool _isDisposed = false;

        #endregion
    }
}
