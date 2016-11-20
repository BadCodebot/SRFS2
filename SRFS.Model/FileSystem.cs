using SRFS.IO;
using SRFS.Model.Clusters;
using SRFS.Model.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using DirectoryNotFoundException = System.IO.DirectoryNotFoundException;
using IOException = System.IO.IOException;
using Path = System.IO.Path;

namespace SRFS.Model {

    public class FileSystem : IDisposable {

        // Public
        #region Construction / Destruction

        /// <summary>
        /// Open a file system.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="signatureKeys"></param>
        /// <param name="decryptionKey"></param>
        /// <param name="options"></param>
        public FileSystem(IBlockIO device, Dictionary<KeyThumbprint,PublicKey> signatureKeys, PrivateKey decryptionKey, Options options) {
            _keys = new Dictionary<Signature, CngKey>();
            foreach (var p in _keys) _keys.Add(p.Key, p.Value);

            _decryptionKey = decryptionKey;

            _options = options;

            _readOnly = true;

            // Create IO Devices
            _deviceIO = device;
            _clusterIO = new FileSystemClusterIO(this, _deviceIO);
            _disposeDeviceIO = true;

            // Initialize Indices
            _freeClusterSearchStart = 0;
            _nextEntryID = 0;
            _nextDirectoryIndex = 0;
            _nextFileIndex = 0;
            _nextAccessRuleIndex = 0;
            _nextAuditRuleIndex = 0;

            // Create Indexes
            _directoryIndex = new Dictionary<int, int>();
            _containedDirectoriesIndex = new Dictionary<int, SortedList<string, Directory>>();
            _containedFilesIndex = new Dictionary<int, SortedList<string, File>>();
            _fileIndex = new Dictionary<int, int>();
            _accessRulesIndex = new Dictionary<int, List<int>>();
            _auditRulesIndex = new Dictionary<int, List<int>>();

            // Read the File System Header Cluster
            FileSystemHeaderCluster fileSystemHeaderCluster = new FileSystemHeaderCluster(_deviceIO.BlockSizeBytes, Guid.Empty);
            byte[] header = new byte[FileSystemHeaderCluster.CalculateClusterSize(_deviceIO.BlockSizeBytes)];
            _deviceIO.Read(0, header, 0, header.Length);
            fileSystemHeaderCluster.Load(header, 0, signatureKeys, options);

            // Initialize values from the File System Header Cluster
            _geometry = new Geometry(
                fileSystemHeaderCluster.BytesPerDataCluster,
                fileSystemHeaderCluster.ClustersPerTrack,
                fileSystemHeaderCluster.DataClustersPerTrack,
                fileSystemHeaderCluster.TrackCount);
            _volumeName = fileSystemHeaderCluster.VolumeName;
            _volumeID = fileSystemHeaderCluster.VolumeID;

            // Initialize the Cluster State Table
            int entryCount = _geometry.ClustersPerTrack * _geometry.TrackCount;
            int clusterCount = (entryCount + ClusterStatesCluster.CalculateElementsPerCluster(_geometry.BytesPerCluster) - 1) / 
                ClusterStatesCluster.CalculateElementsPerCluster(_geometry.BytesPerCluster);

            _clusterStateTable = new ClusterTable<ClusterState>(
                Enumerable.Range(0, clusterCount),
                sizeof(ClusterState),
                (address) => new ClusterStatesCluster(address, _geometry.BytesPerCluster, _volumeID));
            _clusterStateTable.Load(_clusterIO);

            // Initialize the Next Cluster Address Table
            entryCount = _geometry.DataClustersPerTrack * _geometry.TrackCount;
            clusterCount = (entryCount + Int32ArrayCluster.CalculateElementsPerCluster(_geometry.BytesPerCluster) - 1) / 
                Int32ArrayCluster.CalculateElementsPerCluster(_geometry.BytesPerCluster);

            _nextClusterAddressTable = new ClusterTable<int>(
                Enumerable.Range(_clusterStateTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                (address) => new Int32ArrayCluster(address, _geometry.BytesPerCluster, _volumeID, ClusterType.NextClusterAddressTable));
            _nextClusterAddressTable.Load(_clusterIO);

            // Initialize the Bytes Used Table
            _bytesUsedTable = new ClusterTable<int>(
                Enumerable.Range(_nextClusterAddressTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                 (address) => new Int32ArrayCluster(address, _geometry.BytesPerCluster, _volumeID, ClusterType.BytesUsedTable ));
            _bytesUsedTable.Load(_clusterIO);

            entryCount = _geometry.ClustersPerTrack * _geometry.TrackCount;
            clusterCount = (entryCount + VerifyTimesCluster.CalculateElementsPerCluster(_geometry.BytesPerCluster) - 1) / 
                VerifyTimesCluster.CalculateElementsPerCluster(_geometry.BytesPerCluster);

            // Initialize the Verify Time Table
            _verifyTimeTable = new ClusterTable<DateTime>(
                Enumerable.Range(_bytesUsedTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(long),
                 (address) => new VerifyTimesCluster(address, _geometry.BytesPerCluster, _volumeID));
            _verifyTimeTable.Load(_clusterIO);

            int l = _verifyTimeTable.ClusterAddresses.Last() + 1;
            int[] cl = getClusterChain(l).ToArray();

            // Initialize the Directory Table
            _directoryTable = new MutableObjectClusterTable<Directory>(
                getClusterChain(_verifyTimeTable.ClusterAddresses.Last() + 1),
                Directory.StorageLength,
                (address) => Directory.CreateArrayCluster(address));
            _directoryTable.Load(_clusterIO);

            // Initialize the File Table
            _fileTable = new MutableObjectClusterTable<File>(
                getClusterChain(_directoryTable.ClusterAddresses.First() + 1),
                File.StorageLength,
                (address) => File.CreateArrayCluster(address));
            _fileTable.Load(_clusterIO);

            // Initialize the Access Rules Table
            _accessRules = new ClusterTable<SrfsAccessRule>(
                getClusterChain(_fileTable.ClusterAddresses.First() + 1),
                SrfsAccessRule.StorageLength + sizeof(bool),
                (address) => SrfsAccessRule.CreateArrayCluster(address));
            _accessRules.Load(_clusterIO);

            // Initialize the Audit Rules Table
            _auditRules = new ClusterTable<SrfsAuditRule>(
                getClusterChain(_accessRules.ClusterAddresses.First() + 1),
                SrfsAuditRule.StorageLength + sizeof(bool),
                (address) => SrfsAuditRule.CreateArrayCluster(address));
            _auditRules.Load(_clusterIO);

            // Create the Indexes
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

            // Update the next entry ID
            _nextEntryID = Math.Max(
                _directoryIndex.Keys.Max(),
                _fileIndex.Count == 0 ? -1 : _fileIndex.Keys.Max()) + 1;
        }

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    Flush();
                    if (_disposeDeviceIO) _deviceIO.Dispose();
                }
                _isDisposed = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion
        #region Properties

        public long TotalNumberOfBytes {
            get {
                return (long)(_geometry.BytesPerCluster - FileDataCluster.FileBaseClusterHeaderLength) *
                    _geometry.DataClustersPerTrack * _geometry.TrackCount;
            }
        }

        public Directory RootDirectory => _directoryTable[0];
        public int BlockSize => _deviceIO.BlockSizeBytes;

        public IEnumerable<ClusterState> ClusterStates => _clusterStateTable;

        public IClusterIO ClusterIO => _clusterIO;

        public bool ReadOnly => _readOnly;

        #endregion
        #region Methods

        public ClusterState GetClusterState(int absoluteClusterNumber) {
            lock (_lock) return _clusterStateTable[absoluteClusterNumber];
        }

        public void SetClusterState(int absoluteClusterNumber, ClusterState value) {
            if (_readOnly) throw new NotSupportedException();

            lock (_lock) _clusterStateTable[absoluteClusterNumber] = value;
        }

        public int GetBytesUsed(int absoluteClusterNumber) {
            lock (_lock) return _bytesUsedTable[absoluteClusterNumber];
        }

        public void SetBytesUsed(int absoluteClusterNumber, int value) {
            if (_readOnly) throw new NotSupportedException();

            lock (_lock) _bytesUsedTable[absoluteClusterNumber] = value;
        }

        public int GetNextClusterAddress(int absoluteClusterNumber) {
            lock (_lock) return _nextClusterAddressTable[absoluteClusterNumber];
        }

        public void SetNextClusterAddress(int absoluteClusterNumber, int value) {
            if (_readOnly) throw new NotSupportedException();

            lock (_lock) _nextClusterAddressTable[absoluteClusterNumber] = value;
        }

        public DateTime GetVerifyTime(int absoluteClusterNumber) {
            lock (_lock) return _verifyTimeTable[absoluteClusterNumber];
        }

        public void SetVerifyTime(int absoluteClusterNumber, DateTime value) {
            if (_readOnly) throw new NotSupportedException();

            lock (_lock) _verifyTimeTable[absoluteClusterNumber] = value;
        }

        public int AllocateCluster() {
            if (_readOnly) throw new NotSupportedException();

            lock (_lock) {
                for (int i = _freeClusterSearchStart; i < _geometry.DataClustersPerTrack * _geometry.TrackCount; i++) {
                    if (!_clusterStateTable[i].IsUsed()) {
                        _freeClusterSearchStart = i + 1;
                        SetClusterState(i, ClusterState.Used | (_clusterStateTable[i].IsModified() ? ClusterState.Modified : 0));
                        SetNextClusterAddress(i, Constants.NoAddress);
                        SetBytesUsed(i, 0);
                        return i;
                    }
                }
                return Constants.NoAddress;
            }
        }

        public IEnumerable<int> AllocateClusters(int n) {
            if (_readOnly) throw new NotSupportedException();

            for (int i = 0; i < n; i++) yield return AllocateCluster();
        }

        public void DeallocateCluster(int absoluteClusterNumber) {
            if (_readOnly) throw new NotSupportedException();

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
            if (_readOnly) return;

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
            if (_readOnly) throw new NotSupportedException();

            if (_containedDirectoriesIndex[newDirectory.ID].ContainsKey(newName)) throw new System.IO.IOException();
            if (_containedFilesIndex[newDirectory.ID].ContainsKey(newName)) throw new System.IO.IOException();
            _containedDirectoriesIndex[dir.ParentID].Remove(dir.Name);
            dir.ParentID = newDirectory.ID;
            dir.Name = newName;
            _containedDirectoriesIndex[newDirectory.ID].Add(dir.Name, dir);
        }

        public void MoveFile(File file, Directory newDirectory, string newName) {
            if (_readOnly) throw new NotSupportedException();

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
            if (_readOnly) throw new NotSupportedException();

            if (parent == null && _directoryIndex.Count != 0) throw new ArgumentException();

            if (parent != null) {
                if (GetContainedDirectories(parent).ContainsKey(name)) throw new ArgumentException();
                if (GetContainedFiles(parent).ContainsKey(name)) throw new ArgumentException();
            }

            Directory dir = new Directory(_nextEntryID++, name);
            dir.Attributes = System.IO.FileAttributes.Directory;
            if (parent != null) _containedDirectoriesIndex[parent.ID].Add(name, dir);
            int index = getNextFreeDirectoryIndex();
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
            if (_readOnly) throw new NotSupportedException();

            if (parent == null) throw new ArgumentException();

            if (GetContainedDirectories(parent).ContainsKey(name)) throw new ArgumentException();
            if (GetContainedFiles(parent).ContainsKey(name)) throw new ArgumentException();

            File file = new File(_nextEntryID++, name);
            file.Attributes = System.IO.FileAttributes.Normal;

            _containedFilesIndex[parent.ID].Add(name, file);
            int index = getNextFreeFileIndex();
            _fileTable[index] = file;
            _fileIndex.Add(file.ID, index);

            _accessRulesIndex.Add(file.ID, new List<int>());
            _auditRulesIndex.Add(file.ID, new List<int>());

            file.ParentID = parent.ID;
            return file;
        }

        public void RemoveDirectory(Directory dir) {
            if (_readOnly) throw new NotSupportedException();

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
            if (_readOnly) throw new NotSupportedException();

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
            if (_readOnly) throw new NotSupportedException();

            int lowestIndex = int.MaxValue;
            foreach (var index in _accessRulesIndex[obj.ID]) {
                _accessRules[index] = null;
                if (index < lowestIndex) lowestIndex = index;
            }
            _accessRulesIndex[obj.ID].Clear();
            if (lowestIndex != int.MaxValue) _nextAccessRuleIndex = Math.Min(_nextAccessRuleIndex, lowestIndex);
        }

        public void RemoveAuditRules(FileSystemObject obj) {
            if (_readOnly) throw new NotSupportedException();

            int lowestIndex = int.MaxValue;
            foreach (var index in _auditRulesIndex[obj.ID]) {
                _auditRules[index] = null;
                if (index < lowestIndex) lowestIndex = index;
            }
            _auditRulesIndex[obj.ID].Clear();
            if (lowestIndex != int.MaxValue) _nextAuditRuleIndex = Math.Min(_nextAuditRuleIndex, lowestIndex);
        }

        public void AddAccessRule(Directory dir, FileSystemAccessRule rule) {
            if (_readOnly) throw new NotSupportedException();

            int index = getNextFreeAccessRuleIndex();
            _accessRules[index] = new SrfsAccessRule(dir, rule);
            _accessRulesIndex[dir.ID].Add(index);
        }

        public void AddAccessRule(File file, FileSystemAccessRule rule) {
            if (_readOnly) throw new NotSupportedException();

            int index = getNextFreeAccessRuleIndex();
            _accessRules[index] = new SrfsAccessRule(file, rule);
            _accessRulesIndex[file.ID].Add(index);
        }

        public void AddAuditRule(Directory dir, FileSystemAuditRule rule) {
            if (_readOnly) throw new NotSupportedException();

            int index = getNextFreeAuditRuleIndex();
            _auditRules[index] = new SrfsAuditRule(dir, rule);
            _auditRulesIndex[dir.ID].Add(index);
        }

        public void AddAuditRule(File file, FileSystemAuditRule rule) {
            if (_readOnly) throw new NotSupportedException();

            int index = getNextFreeAuditRuleIndex();
            _auditRules[index] = new SrfsAuditRule(file, rule);
            _auditRulesIndex[file.ID].Add(index);
        }

        #endregion

        // Private
        #region Methods

        private static void Create(IBlockIO deviceIO, string volumeName, Geometry geometry, Guid volumeID, PrivateKey signatureKey) {

            // Write the Partition Cluster
            FileSystemHeaderCluster fileSystemHeaderCluster = new FileSystemHeaderCluster(deviceIO.BlockSizeBytes, volumeID);
            fileSystemHeaderCluster.BytesPerDataCluster = geometry.BytesPerCluster;
            fileSystemHeaderCluster.ClustersPerTrack = geometry.ClustersPerTrack;
            fileSystemHeaderCluster.DataClustersPerTrack = geometry.DataClustersPerTrack;
            fileSystemHeaderCluster.TrackCount = geometry.TrackCount;
            fileSystemHeaderCluster.VolumeName = volumeName;
            byte[] data = new byte[fileSystemHeaderCluster.ClusterSizeBytes];
            fileSystemHeaderCluster.Save(data, 0, signatureKey);
            deviceIO.Write(0, data, 0, data.Length);

            // Cluster State Table
            int entryCount = geometry.ClustersPerTrack * geometry.TrackCount;
            int clusterCount = (entryCount + ClusterStatesCluster.CalculateElementsPerCluster(geometry.BytesPerCluster) - 1) / 
                ClusterStatesCluster.CalculateElementsPerCluster(geometry.BytesPerCluster);
            ClusterTable<ClusterState> clusterStateTable = new ClusterTable<ClusterState>(
                Enumerable.Range(0, clusterCount),
                sizeof(ClusterState),
                (address) => new ClusterStatesCluster(address, geometry.BytesPerCluster, volumeID));

            // Next Cluster Address Table
            entryCount = geometry.DataClustersPerTrack * geometry.TrackCount;
            clusterCount = (entryCount + Int32ArrayCluster.CalculateElementsPerCluster(geometry.BytesPerCluster) - 1) / 
                Int32ArrayCluster.CalculateElementsPerCluster(geometry.BytesPerCluster);
            ClusterTable<int> nextClusterAddressTable = new ClusterTable<int>(
                Enumerable.Range(clusterStateTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                (address) => new Int32ArrayCluster(address, geometry.BytesPerCluster, volumeID, ClusterType.NextClusterAddressTable ));

            // Bytes Used Table
            ClusterTable<int> bytesUsedTable = new ClusterTable<int>(
                Enumerable.Range(nextClusterAddressTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(int),
                 (address) => new Int32ArrayCluster(address, geometry.BytesPerCluster, volumeID, ClusterType.BytesUsedTable ));

            entryCount = geometry.ClustersPerTrack * geometry.TrackCount;
            clusterCount = (entryCount + VerifyTimesCluster.CalculateElementsPerCluster(geometry.BytesPerCluster) - 1) / 
                VerifyTimesCluster.CalculateElementsPerCluster(geometry.BytesPerCluster);

            // Verify Time Table
            ClusterTable<DateTime> verifyTimeTable = new ClusterTable<DateTime>(
                Enumerable.Range(bytesUsedTable.ClusterAddresses.Last() + 1, clusterCount),
                sizeof(long),
                 (address) => new VerifyTimesCluster(address, geometry.BytesPerCluster, volumeID));

            // Directory Table
            MutableObjectClusterTable<Directory> directoryTable = new MutableObjectClusterTable<Directory>(
                new int[] { verifyTimeTable.ClusterAddresses.Last() + 1 },
                Directory.StorageLength,
                (address) => Directory.CreateArrayCluster(address));

            // File Table
            MutableObjectClusterTable<File> fileTable = new MutableObjectClusterTable<File>(
                new int[] { directoryTable.ClusterAddresses.Last() + 1 },
                File.StorageLength,
                (address) => File.CreateArrayCluster(address));

            // Access Rules Table
            ClusterTable<SrfsAccessRule> accessRules = new ClusterTable<SrfsAccessRule>(
                new int[] { fileTable.ClusterAddresses.Last() + 1 },
                SrfsAccessRule.StorageLength + sizeof(bool),
                (address) => SrfsAccessRule.CreateArrayCluster(address));

            // Audit Rules Table
            ClusterTable<SrfsAuditRule> auditRules = new ClusterTable<SrfsAuditRule>(
                new int[] { accessRules.ClusterAddresses.Last() + 1 },
                SrfsAuditRule.StorageLength + sizeof(bool),
                (address) => SrfsAuditRule.CreateArrayCluster(address));

            // Initialize the tables
            int nDataClusters = geometry.DataClustersPerTrack * geometry.TrackCount;
            int nParityClusters = geometry.ParityClustersPerTrack * geometry.TrackCount;

            for (int i = 0; i < nDataClusters; i++)
                clusterStateTable[i] = ClusterState.Data | ClusterState.Unwritten;
            for (int i = nDataClusters; i < nDataClusters + nParityClusters; i++)
                clusterStateTable[i] = ClusterState.Parity | ClusterState.Unwritten;
            for (int i = nDataClusters + nParityClusters; i < clusterStateTable.Count; i++)
                clusterStateTable[i] = ClusterState.Null;

            for (int i = 0; i < clusterStateTable.Count; i++) clusterStateTable[i] = ClusterState.None;
            for (int i = 0; i < nextClusterAddressTable.Count; i++) nextClusterAddressTable[i] = Constants.NoAddress;
            for (int i = 0; i < bytesUsedTable.Count; i++) bytesUsedTable[i] = 0;
            for (int i = 0; i < verifyTimeTable.Count; i++) verifyTimeTable[i] = DateTime.MinValue;
            for (int i = 0; i < directoryTable.Count; i++) directoryTable[i] = null;
            for (int i = 0; i < fileTable.Count; i++) fileTable[i] = null;
            for (int i = 0; i < accessRules.Count; i++) accessRules[i] = null;
            for (int i = 0; i < auditRules.Count; i++) auditRules[i] = null;

            // Update the cluster state and next cluster address tables
            foreach (var t in new IEnumerable<int>[] {
                clusterStateTable.ClusterAddresses,
                nextClusterAddressTable.ClusterAddresses,
                bytesUsedTable.ClusterAddresses,
                verifyTimeTable.ClusterAddresses,
                directoryTable.ClusterAddresses,
                fileTable.ClusterAddresses,
                accessRules.ClusterAddresses,
                auditRules.ClusterAddresses
            }) {
                foreach (var n in t) {
                    clusterStateTable[n] = ClusterState.System | ClusterState.Used;
                    nextClusterAddressTable[n] = n + 1;
                }
                nextClusterAddressTable[t.Last()] = Constants.NoAddress;
            }

            // Create the root directory
            Directory dir = new Directory(0, "");
            dir.Attributes = System.IO.FileAttributes.Directory;
            dir.ParentID = Constants.NoID;
            dir.Owner = WindowsIdentity.GetCurrent().User;
            dir.Group = WindowsIdentity.GetCurrent().User;
            directoryTable[0] = dir;
            accessRules[0] = new SrfsAccessRule(dir, new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                AccessControlType.Allow));

            SimpleClusterIO clusterIO = new SimpleClusterIO(deviceIO, fileSystemHeaderCluster.BytesPerDataCluster);
            clusterStateTable.Flush(clusterIO);
            nextClusterAddressTable.Flush(clusterIO);
            bytesUsedTable.Flush(clusterIO);
            verifyTimeTable.Flush(clusterIO);
            directoryTable.Flush(clusterIO);
            fileTable.Flush(clusterIO);
            accessRules.Flush(clusterIO);
            auditRules.Flush(clusterIO);
        }

        private IEnumerable<int> getClusterChain(int startingClusterNumber) {
            while (startingClusterNumber != Constants.NoAddress) {
                yield return startingClusterNumber;
                startingClusterNumber = GetNextClusterAddress(startingClusterNumber);
            }
        }

        #endregion

        #region Properties

        public Directory GetDirectory(int id) => _directoryTable[id];

        public IEnumerable<File> Files => _fileTable.Where(f => f != null);

        public long TotalNumberOfFreeBytes {
            get {
                lock (_lock) {
                    long count = 0;
                    for (int i = 0; i < _geometry.DataClustersPerTrack * _geometry.TrackCount; i++) {
                        if ((_clusterStateTable[i] & ClusterState.Used) == 0) count++;
                    }

                    //long freeClusters = (from c in _clusterStateTable
                    //                     where (c & ClusterState.Used) == 0 && (c & ClusterState.Parity) == 0 &&
                    //                     (c & ClusterState.Null) == 0
                    //                     select c).Count();
                    return count * (_geometry.BytesPerCluster - FileDataCluster.FileBaseClusterHeaderLength);
                }
            }
        }

        #endregion
        #region Private Methods

        private int getNextFreeDirectoryIndex() => getNextFreeIndex(ref _nextDirectoryIndex, _directoryTable);

        private int getNextFreeFileIndex() => getNextFreeIndex(ref _nextFileIndex, _fileTable);

        private int getNextFreeAccessRuleIndex() => getNextFreeIndex(ref _nextAccessRuleIndex, _accessRules);

        private int getNextFreeAuditRuleIndex() => getNextFreeIndex(ref _nextAuditRuleIndex, _auditRules);

        private int getNextFreeIndex<T>(ref int nextIndex, IClusterTable<T> table) {
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

        #endregion
        #region Private Fields

        // Cryptographic settings
        private Dictionary<Signature, CngKey> _keys;
        private PrivateKey _decryptionKey;

        // Geometry
        private Geometry _geometry;

        // Metadata
        private Guid _volumeID;
        private string _volumeName;

        // Options
        private Options _options;

        // Indices
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

        private bool _disposeDeviceIO = false;
        private IBlockIO _deviceIO;
        private FileSystemClusterIO _clusterIO;

        private object _lock = new object();

        private bool _isDisposed = false;

        private bool _readOnly = true;

        #endregion
    }
}
