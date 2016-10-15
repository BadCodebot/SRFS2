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

        public static FileSystem Create(IBlockIO deviceIO) {
            FileSystem fileSystem = new FileSystem(deviceIO);
            fileSystem.create();
            return fileSystem;
        }

        public static FileSystem Mount(IBlockIO deviceIO) {
            FileSystem fileSystem = new FileSystem(deviceIO);
            fileSystem.mount();
            return fileSystem;
        }

        private FileSystem(IBlockIO deviceIO) {
            _deviceIO = deviceIO;

            _freeClusterSearchStart = 0;

            _nextEntryID = 0;

            _nextDirectoryIndex = 0;
            _nextFileIndex = 0;
            _nextAccessRuleIndex = 0;
            _nextAuditRuleIndex = 0;

            _directoryIndex = new Dictionary<int, int>();

            _containedDirectoriesIndex = new Dictionary<int, SortedList<string, Directory>>();
            _containedFilesIndex = new Dictionary<int, SortedList<string, File>>();

            _fileIndex = new Dictionary<int, int>();

            _accessRulesIndex = new Dictionary<int, List<int>>();

            _auditRulesIndex = new Dictionary<int, List<int>>();
        }

        private void create() {

            // Write the Partition Cluster
            FileSystemHeaderCluster fileSystemHeaderCluster = new FileSystemHeaderCluster(_deviceIO.BlockSizeBytes);
            fileSystemHeaderCluster.Initialize();
            byte[] header = new byte[fileSystemHeaderCluster.SizeBytes];
            fileSystemHeaderCluster.Save(header, 0);
            _deviceIO.Write(0, header, 0, header.Length);

            // Create IO Devices
            _clusterIO = new FileSystemClusterIO(this, _deviceIO);

            // Cluster State Table
            int entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            int clusterCount = (entryCount + ClusterStatesCluster.ElementsPerCluster - 1) / ClusterStatesCluster.ElementsPerCluster;

            _clusterStateTable = new ClusterTable<ClusterState>(
                Enumerable.Range(0, clusterCount),
                sizeof(ClusterState),
                (address) => new ClusterStatesCluster(address));

            // Next Cluster Address Table
            entryCount = Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + IntArrayCluster.ElementsPerCluster - 1) / IntArrayCluster.ElementsPerCluster;

            _nextClusterAddressTable = new ClusterTable<int>(
                Enumerable.Range(_clusterStateTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                (address) => new IntArrayCluster(address) { Type = ClusterType.NextClusterAddressTable });

            // Bytes Used Table
            _bytesUsedTable = new ClusterTable<int>(
                Enumerable.Range(_nextClusterAddressTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                 (address) => new IntArrayCluster(address) { Type = ClusterType.BytesUsedTable });

            entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + VerifyTimesCluster.ElementsPerCluster - 1) / VerifyTimesCluster.ElementsPerCluster;

            // Verify Time Table
            _verifyTimeTable = new ClusterTable<DateTime>(
                Enumerable.Range(_bytesUsedTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(long),
                 (address) => new VerifyTimesCluster(address));

            // Directory Table
            _directoryTable = new MutableObjectClusterTable<Directory>(
                new int[] { _verifyTimeTable.ClusterAddresses.Last() + 1 },
                Directory.StorageLength,
                (address) => Directory.CreateArrayCluster(address));

            // File Table
            _fileTable = new MutableObjectClusterTable<File>(
                new int[] { _directoryTable.ClusterAddresses.Last() + 1 },
                File.StorageLength,
                (address) => File.CreateArrayCluster(address));

            // Access Rules Table
            _accessRules = new ClusterTable<SrfsAccessRule>(
                new int[] { _fileTable.ClusterAddresses.Last() + 1 },
                SrfsAccessRule.StorageLength + sizeof(bool),
                (address) => SrfsAccessRule.CreateArrayCluster(address));

            // Audit Rules Table
            _auditRules = new ClusterTable<SrfsAuditRule>(
                new int[] { _accessRules.ClusterAddresses.Last() + 1 },
                SrfsAuditRule.StorageLength + sizeof(bool),
                (address) => SrfsAuditRule.CreateArrayCluster(address));

            // Initialize the tables
            int nDataClusters = Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount;
            int nParityClusters = Configuration.Geometry.ParityClustersPerTrack * Configuration.Geometry.TrackCount;

            for (int i = 0; i < nDataClusters; i++) _clusterStateTable[i] = ClusterState.Unused;
            for (int i = nDataClusters; i < nDataClusters + nParityClusters; i++) _clusterStateTable[i] = ClusterState.Parity;
            for (int i = nDataClusters + nParityClusters; i < _clusterStateTable.Count; i++) _clusterStateTable[i] = ClusterState.Null;

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
                _clusterStateTable.ClusterAddresses,
                _nextClusterAddressTable.ClusterAddresses,
                _bytesUsedTable.ClusterAddresses,
                _verifyTimeTable.ClusterAddresses,
                _directoryTable.ClusterAddresses,
                _fileTable.ClusterAddresses,
                _accessRules.ClusterAddresses,
                _auditRules.ClusterAddresses
            }) {
                foreach (var n in t) {
                    _clusterStateTable[n] = ClusterState.System | ClusterState.Used;
                    _nextClusterAddressTable[n] = n + 1;
                }
                _nextClusterAddressTable[t.Last()] = Constants.NoAddress;
            }

            // Create the root directory
            Directory dir = CreateDirectory(null, "");
            dir.Owner = WindowsIdentity.GetCurrent().User;
            dir.Group = WindowsIdentity.GetCurrent().User;
            //dir.Group = new SecurityIdentifier(WellKnownSidType.NullSid, null);
            AddAccessRule(dir, new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                AccessControlType.Allow));

            Flush();
        }

        private void mount() {

            // Read the Partition Cluster
            FileSystemHeaderCluster fileSystemHeaderCluster = new FileSystemHeaderCluster(_deviceIO.BlockSizeBytes);
            byte[] header = new byte[fileSystemHeaderCluster.SizeBytes];
            _deviceIO.Read(0, header, 0, header.Length);
            fileSystemHeaderCluster.Load(header, 0);

            Configuration.Geometry = new Geometry(
                fileSystemHeaderCluster.BytesPerCluster,
                fileSystemHeaderCluster.ClustersPerTrack,
                fileSystemHeaderCluster.DataClustersPerTrack,
                fileSystemHeaderCluster.TrackCount);
            Configuration.VolumeName = fileSystemHeaderCluster.VolumeName;
            Configuration.FileSystemID = fileSystemHeaderCluster.ID;

            // Create IO Devices
            _clusterIO = new FileSystemClusterIO(this, _deviceIO);

            // Cluster State Table
            int entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            int clusterCount = (entryCount + ClusterStatesCluster.ElementsPerCluster - 1) / ClusterStatesCluster.ElementsPerCluster;

            _clusterStateTable = new ClusterTable<ClusterState>(
                Enumerable.Range(0, clusterCount),
                sizeof(ClusterState),
                (address) => new ClusterStatesCluster(address));
            _clusterStateTable.Load(_clusterIO);

            // Next Cluster Address Table
            entryCount = Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + IntArrayCluster.ElementsPerCluster - 1) / IntArrayCluster.ElementsPerCluster;

            _nextClusterAddressTable = new ClusterTable<int>(
                Enumerable.Range(_clusterStateTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                (address) => new IntArrayCluster(address) { Type = ClusterType.NextClusterAddressTable });
            _nextClusterAddressTable.Load(_clusterIO);

            // Bytes Used Table
            _bytesUsedTable = new ClusterTable<int>(
                Enumerable.Range(_nextClusterAddressTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                 (address) => new IntArrayCluster(address) { Type = ClusterType.BytesUsedTable });
            _bytesUsedTable.Load(_clusterIO);

            entryCount = Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount;
            clusterCount = (entryCount + VerifyTimesCluster.ElementsPerCluster - 1) / VerifyTimesCluster.ElementsPerCluster;

            // Verify Time Table
            _verifyTimeTable = new ClusterTable<DateTime>(
                Enumerable.Range(_bytesUsedTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(long),
                 (address) => new VerifyTimesCluster(address));
            _verifyTimeTable.Load(_clusterIO);

            int l = _verifyTimeTable.ClusterAddresses.Last() + 1;
            int[] cl = getClusterChain(l).ToArray();

            // Directory Table
            _directoryTable = new MutableObjectClusterTable<Directory>(
                getClusterChain(_verifyTimeTable.ClusterAddresses.Last() + 1),
                Directory.StorageLength,
                (address) => Directory.CreateArrayCluster(address));
            _directoryTable.Load(_clusterIO);

            // File Table
            _fileTable = new MutableObjectClusterTable<File>(
                getClusterChain(_directoryTable.ClusterAddresses.First() + 1),
                File.StorageLength,
                (address) => File.CreateArrayCluster(address));
            _fileTable.Load(_clusterIO);

            // Access Rules Table
            _accessRules = new ClusterTable<SrfsAccessRule>(
                getClusterChain(_fileTable.ClusterAddresses.First() + 1),
                SrfsAccessRule.StorageLength + sizeof(bool),
                (address) => SrfsAccessRule.CreateArrayCluster(address));
            _accessRules.Load(_clusterIO);

            // Audit Rules Table
            _auditRules = new ClusterTable<SrfsAuditRule>(
                getClusterChain(_accessRules.ClusterAddresses.First() + 1),
                SrfsAuditRule.StorageLength + sizeof(bool),
                (address) => SrfsAuditRule.CreateArrayCluster(address));
            _auditRules.Load(_clusterIO);

            // Create the Indices
            for (int i = 0; i < _directoryTable.Count; i++) {
                Directory d = _directoryTable[i];
                if (d != null) {
                    _directoryIndex.Add(d.ID, i);
                    _containedDirectoriesIndex.Add(d.ID, new SortedList<string, Directory>(StringComparer.OrdinalIgnoreCase));
                    _containedFilesIndex.Add(d.ID, new SortedList<string, File>(StringComparer.OrdinalIgnoreCase));
                    _accessRulesIndex.Add(d.ID, new List<int>());
                    _auditRulesIndex.Add(d.ID, new List<int>());
                }
            }

            foreach (var d in from d in _directoryTable where d != null && d.ParentID != Constants.NoID select d) {
                _containedDirectoriesIndex[d.ParentID].Add(d.Name, d);
            }

            for (int i = 0; i < _fileTable.Count; i++) {
                File f = _fileTable[i];
                if (f != null) {
                    _fileIndex.Add(f.ID, i);
                    _containedFilesIndex[f.ParentID].Add(f.Name, f);
                    _accessRulesIndex.Add(f.ID, new List<int>());
                    _auditRulesIndex.Add(f.ID, new List<int>());
                }
            }

            for (int i = 0; i < _accessRules.Count; i++) {
                Data.SrfsAccessRule r = _accessRules[i];
                if (r != null) _accessRulesIndex[r.ID].Add(i);
            }

            for (int i = 0; i < _auditRules.Count; i++) {
                Data.SrfsAuditRule r = _auditRules[i];
                if (r != null) _auditRulesIndex[r.ID].Add(i);
            }

            _nextEntryID = Math.Max(
                _directoryIndex.Keys.Max(), 
                _fileIndex.Count == 0 ? -1 : _fileIndex.Keys.Max()) + 1;
        }

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    Flush();
                }
                _isDisposed = true;
            }
        }

        private IEnumerable<int> getClusterChain(int startingClusterNumber) {
            while (startingClusterNumber != Constants.NoAddress) {
                yield return startingClusterNumber;
                startingClusterNumber = GetNextClusterAddress(startingClusterNumber);
            }
        }

        public void Dispose() => Dispose(true);

        public IDictionary<string, Directory> GetContainedDirectories(Directory dir) {
            return new ReadOnlyDictionary<string, Directory>(_containedDirectoriesIndex[dir.ID]);
        }

        public IDictionary<string, File> GetContainedFiles(Directory dir) {
            return new ReadOnlyDictionary<string, File>(_containedFilesIndex[dir.ID]);
        }

        public IEnumerable<FileSystemAccessRule> GetAccessRules(FileSystemObject entry) {
            return (from i in _accessRulesIndex[entry.ID] select _accessRules[i].Rule);
        }

        public IEnumerable<FileSystemAuditRule> GetAuditRules(FileSystemObject entry) {
            return (from i in _auditRulesIndex[entry.ID] select _auditRules[i].Rule);
        }

        public void MoveDirectory(Directory dir, Directory newDirectory, string newName) {
            if (_containedDirectoriesIndex[newDirectory.ID].ContainsKey(newName)) throw new System.IO.IOException();
            if (_containedFilesIndex[newDirectory.ID].ContainsKey(newName)) throw new System.IO.IOException();
            _containedDirectoriesIndex[dir.ParentID].Remove(dir.Name);
            dir.ParentID = newDirectory.ID;
            dir.Name = newName;
            _containedDirectoriesIndex[newDirectory.ID].Add(dir.Name, dir);
        }

        public void MoveFile(File file, Directory newDirectory, string newName) {
            if (_containedDirectoriesIndex[newDirectory.ID].ContainsKey(newName)) throw new System.IO.IOException();
            if (_containedFilesIndex[newDirectory.ID].ContainsKey(newName)) throw new System.IO.IOException();
            // We need to lock and check if the names already exist.  All access to these indices should be threadsafe.  Also,
            // think about passing back copies of internal structures like lists and dictionaries so that we don't have pointers
            // to stuff we try and lock later.
            _containedFilesIndex[file.ParentID].Remove(file.Name);
            file.ParentID = newDirectory.ID;
            file.Name = newName;
            _containedFilesIndex[newDirectory.ID].Add(file.Name, file);
        }

        public Directory CreateDirectory(Directory parent, string name) {
            if (parent == null && _directoryIndex.Count != 0) throw new ArgumentException();

            if (parent != null) {
                if (GetContainedDirectories(parent).ContainsKey(name)) throw new ArgumentException();
                if (GetContainedFiles(parent).ContainsKey(name)) throw new ArgumentException();
            }

            Directory dir = new Directory(_nextEntryID++, name);
            dir.Attributes = System.IO.FileAttributes.Directory;
            if (parent != null) _containedDirectoriesIndex[parent.ID].Add(name, dir);
            int index = getFreeDirectoryIndex();
            _directoryTable[index] = dir;
            _directoryIndex.Add(dir.ID, index);

            _containedDirectoriesIndex.Add(dir.ID, new SortedList<string, Directory>(StringComparer.OrdinalIgnoreCase));
            _containedFilesIndex.Add(dir.ID, new SortedList<string, File>(StringComparer.OrdinalIgnoreCase));
            _accessRulesIndex.Add(dir.ID, new List<int>());
            _auditRulesIndex.Add(dir.ID, new List<int>());

            dir.ParentID = parent?.ID ?? Constants.NoID;
            return dir;
        }

        public File CreateFile(Directory parent, string name) {
            if (parent == null) throw new ArgumentException();

            if (GetContainedDirectories(parent).ContainsKey(name)) throw new ArgumentException();
            if (GetContainedFiles(parent).ContainsKey(name)) throw new ArgumentException();

            File file = new File(_nextEntryID++, name);
            file.Attributes = System.IO.FileAttributes.Normal;

            _containedFilesIndex[parent.ID].Add(name, file);
            int index = getFreeFileIndex();
            _fileTable[index] = file;
            _fileIndex.Add(file.ID, index);

            _accessRulesIndex.Add(file.ID, new List<int>());
            _auditRulesIndex.Add(file.ID, new List<int>());

            file.ParentID = parent.ID;
            return file;
        }

        public void RemoveDirectory(Directory dir) {
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

        public void RemoveFile(File file) {
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

        public void RemoveAccessRules(FileSystemObject obj) {
            int lowestIndex = int.MaxValue;
            foreach (var index in _accessRulesIndex[obj.ID]) {
                _accessRules[index] = null;
                if (index < lowestIndex) lowestIndex = index;
            }
            _accessRulesIndex[obj.ID].Clear();
            if (lowestIndex != int.MaxValue) _nextAccessRuleIndex = Math.Min(_nextAccessRuleIndex, lowestIndex);
        }

        public void RemoveAuditRules(FileSystemObject obj) {
            int lowestIndex = int.MaxValue;
            foreach (var index in _auditRulesIndex[obj.ID]) {
                _auditRules[index] = null;
                if (index < lowestIndex) lowestIndex = index;
            }
            _auditRulesIndex[obj.ID].Clear();
            if (lowestIndex != int.MaxValue) _nextAuditRuleIndex = Math.Min(_nextAuditRuleIndex, lowestIndex);
        }

        public void AddAccessRule(Directory dir, FileSystemAccessRule rule) {
            int index = getFreeAccessRuleIndex();
            _accessRules[index] = new SrfsAccessRule(dir, rule);
            _accessRulesIndex[dir.ID].Add(index);
        }

        public void AddAccessRule(File file, FileSystemAccessRule rule) {
            int index = getFreeAccessRuleIndex();
            _accessRules[index] = new SrfsAccessRule(file, rule);
            _accessRulesIndex[file.ID].Add(index);
        }

        public void AddAuditRule(Directory dir, FileSystemAuditRule rule) {
            int index = getFreeAuditRuleIndex();
            _auditRules[index] = new SrfsAuditRule(dir, rule);
            _auditRulesIndex[dir.ID].Add(index);
        }

        public void AddAuditRule(File file, FileSystemAuditRule rule) {
            int index = getFreeAuditRuleIndex();
            _auditRules[index] = new SrfsAuditRule(file, rule);
            _auditRulesIndex[file.ID].Add(index);
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

        public int GetNextClusterAddress(int absoluteClusterNumber) {
            lock (_lock) return _nextClusterAddressTable[absoluteClusterNumber];
        }

        public void SetNextClusterAddress(int absoluteClusterNumber, int value) {
            lock (_lock) _nextClusterAddressTable[absoluteClusterNumber] = value;
        }

        public DateTime GetVerifyTime(int absoluteClusterNumber) {
            lock (_lock) return _verifyTimeTable[absoluteClusterNumber];
        }

        public void SetVerifyTime(int absoluteClusterNumber, DateTime value) {
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
                return Constants.NoAddress;
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

        public FileSystemObject GetFileSystemObject(string path) {
            string[] components = path.Split(Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            if (components.Length < 2) throw new IOException($"Incomplete path '{path}'");
            Directory dir = (Directory)_directoryTable[0];
            for (int i = 1; i < components.Length - 1; i++) {
                if (!_containedDirectoriesIndex[dir.ID].TryGetValue(components[i], out dir))
                    throw new DirectoryNotFoundException($"Directory component {i} not found in '{path}'");
            }

            if (components[components.Length - 1] == string.Empty) return dir;

            Directory d;
            if (_containedDirectoriesIndex[dir.ID].TryGetValue(components[components.Length - 1], out d)) return d;
            File f;
            if (_containedFilesIndex[dir.ID].TryGetValue(components[components.Length - 1], out f)) return f;

            return null;
        }

        public Directory GetParentDirectory(string path, out string name) {
            string[] components = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (components.Length < 2) throw new IOException($"Incomplete path '{path}'");
            Directory dir = _directoryTable[0];
            for (int i = 1; i < components.Length - 1; i++) {
                if (!_containedDirectoriesIndex[dir.ID].TryGetValue(components[i], out dir)) throw new DirectoryNotFoundException($"Directory component {i} not found in '{path}'");
            }
            name = components[components.Length - 1];
            return dir;
        }

        public void Flush() {

            // These must be flushed first, since they can modify the nextCluster and bytesUsed tables below.
            _directoryTable.Flush(_clusterIO);
            _fileTable.Flush(_clusterIO);
            _accessRules.Flush(_clusterIO);
            _auditRules.Flush(_clusterIO);

            _clusterStateTable.Flush(_clusterIO);
            _nextClusterAddressTable.Flush(_clusterIO);
            _bytesUsedTable.Flush(_clusterIO);
            _verifyTimeTable.Flush(_clusterIO);
        }

        #endregion
        #region Properties

        public long TotalNumberOfBytes {
            get {
                return (long)(Configuration.Geometry.BytesPerCluster - FileDataCluster.HeaderLength) * 
                    Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount;
            }
        }

        public Directory RootDirectory => _directoryTable[0];
        public Directory GetDirectory(int id) => _directoryTable[id];

        public long TotalNumberOfFreeBytes {
            get {
                lock (_lock) {
                    long count = 0;
                    for (int i = 0; i < Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount; i++) {
                        if ((_clusterStateTable[i] & ClusterState.Used) == 0) count++;
                    }

                    //long freeClusters = (from c in _clusterStateTable
                    //                     where (c & ClusterState.Used) == 0 && (c & ClusterState.Parity) == 0 &&
                    //                     (c & ClusterState.Null) == 0
                    //                     select c).Count();
                    return count * (Configuration.Geometry.BytesPerCluster - FileDataCluster.HeaderLength);
                }
            }
        }

        #endregion
        #region Private Fields

        private int getFreeDirectoryIndex() {
            return getFreeIndex(ref _nextDirectoryIndex, _directoryTable);
        }

        private int getFreeFileIndex() {
            return getFreeIndex(ref _nextFileIndex, _fileTable);
        }

        private int getFreeAccessRuleIndex() {
            return getFreeIndex(ref _nextAccessRuleIndex, _accessRules);
        }

        private int getFreeAuditRuleIndex() {
            return getFreeIndex(ref _nextAuditRuleIndex, _auditRules);
        }

        private int getFreeIndex<T>(ref int nextIndex, IClusterTable<T> table) {
            int i = nextIndex;
            while (true) {
                if (i >= table.Count) {
                    int newClusterNumber = AllocateCluster();
                    _clusterStateTable[newClusterNumber] |= ClusterState.System;
                    table.AddCluster(newClusterNumber);
                }
                if (table[i] == null) {
                    nextIndex = i + 1;
                    return i;
                }
                i++;
            }
        }

        private int _nextEntryID;
        private int _freeClusterSearchStart;
        private int _nextDirectoryIndex;
        private int _nextFileIndex;
        private int _nextAccessRuleIndex;
        private int _nextAuditRuleIndex;

        public IEnumerable<ClusterState> ClusterStates => _clusterStateTable;

        private ClusterTable<ClusterState> _clusterStateTable;
        private ClusterTable<int> _nextClusterAddressTable;
        private ClusterTable<int> _bytesUsedTable;
        private ClusterTable<DateTime> _verifyTimeTable;

        private MutableObjectClusterTable<Directory> _directoryTable;
        private Dictionary<int, int> _directoryIndex;

        private Dictionary<int, SortedList<string, Directory>> _containedDirectoriesIndex;
        private Dictionary<int, SortedList<string, File>> _containedFilesIndex;

        private MutableObjectClusterTable<File> _fileTable;
        private Dictionary<int, int> _fileIndex;

        private ClusterTable<SrfsAccessRule> _accessRules;
        private Dictionary<int, List<int>> _accessRulesIndex;

        private ClusterTable<SrfsAuditRule> _auditRules;
        private Dictionary<int, List<int>> _auditRulesIndex;

        public IClusterIO ClusterIO => _clusterIO;

        private IBlockIO _deviceIO;
        private FileSystemClusterIO _clusterIO;

        private object _lock = new object();

        private bool _isDisposed = false;

        #endregion

        private class FileSystemClusterIO : SimpleClusterIO {

            public FileSystemClusterIO(FileSystem fs, IBlockIO io) : base(io, FileSystemHeaderCluster.CalculateClusterSize(io.BlockSizeBytes)) {
                _fileSystem = fs;
            }

            public override void Load(Cluster c) {
                if (c is FileBaseCluster fb) {
                    Console.WriteLine($"Loading FileBaseCluster {fb.Address}");
                } else if (c is ArrayCluster a) {
                    Console.WriteLine($"Loading ArrayCluster {a.Address}");
                }
                base.Load(c);
            }
            public override void Save(Cluster c) {
                base.Save(c);
                if (c is FileBaseCluster fb) {
                    Console.WriteLine($"Saved FileBaseCluster {fb.Address}");
                    _fileSystem.SetBytesUsed(fb.Address, fb.BytesUsed);
                    _fileSystem.SetNextClusterAddress(fb.Address, fb.NextClusterAddress);
                    _fileSystem.SetClusterState(fb.Address, _fileSystem.GetClusterState(fb.Address) | ClusterState.Modified);
                } else if (c is ArrayCluster a) {
                    Console.WriteLine($"Saved ArrayCluster {a.Address}");
                    _fileSystem.SetNextClusterAddress(a.Address, a.NextClusterAddress);
                    _fileSystem.SetClusterState(a.Address, _fileSystem.GetClusterState(a.Address) | ClusterState.Modified);
                }
            }

            private FileSystem _fileSystem;
        }
    }
}
