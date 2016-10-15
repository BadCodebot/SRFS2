using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model.Clusters;
using System.Diagnostics;
using SRFS.Model.Data;
using log4net;

namespace SRFS.Model {

    public class FileIO : IDisposable {

        private static readonly ILog Log = LogManager.GetLogger(typeof(FileIO));

        #region Construction/Destruction

        public FileIO(FileSystem fileSystem, File file) {

            _fileSystem = fileSystem;
            _file = file;
            _clusterAddresses = new List<int>();

            _cluster = null;

            _isModified = false;
            _logicalClusterNumber = Constants.NoAddress;

            _firstClusterDataLength = Configuration.Geometry.BytesPerCluster - FileHeaderCluster.HeaderLength;
            _clusterDataLength = Configuration.Geometry.BytesPerCluster - FileDataCluster.HeaderLength;

            int absoluteClusterNumber = file.FirstCluster;
            while (absoluteClusterNumber != Constants.NoAddress) {
                _clusterAddresses.Add(absoluteClusterNumber);
                absoluteClusterNumber = _fileSystem.GetNextClusterAddress(absoluteClusterNumber);
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    Flush();
                    _fileSystem.Flush();
                }

                _isDisposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        #endregion
        #region Properties

        private int _logicalClusterNumber;

        protected int BytesUsed {
            get {
                return _cluster.BytesUsed;
            }
            set {
                if (value == _cluster.BytesUsed) return;
                if (value > _cluster.Data.Length) throw new ArgumentOutOfRangeException();

                _cluster.BytesUsed = value;
                if (_bufferUsed > _cluster.BytesUsed) {
                    _cluster.Data.Clear(_cluster.BytesUsed, _bufferUsed - _cluster.BytesUsed);
                    _bufferUsed = _cluster.BytesUsed;
                }

                _isModified = true;
            }
        }

        protected int NextCluster {
            get {
                return _cluster.NextClusterAddress;
            }
            set {
                if (value == _cluster.NextClusterAddress) return;
                _cluster.NextClusterAddress = value;
                _isModified = true;
            }
        }

        #endregion
        #region Methods

        public void Close() {
            Dispose();
        }

        public void Flush() {
            if (!_isModified) return;
            fillFromDisk();
            _fileSystem.ClusterIO.Save(_cluster);

            _isModified = false;
        }

        public int ReadFile(byte[] buffer, long filePosition) {
            int bufferOffset = 0;
            int count = buffer.Length;
            int totalBytesRead = 0;

            while (count > 0) {
                int bytesRead = read(buffer, bufferOffset, count, filePosition);
                if (bytesRead == 0) return totalBytesRead;
                bufferOffset += bytesRead;
                filePosition += bytesRead;
                totalBytesRead += bytesRead;
                count -= bytesRead;
            }

            return totalBytesRead;
        }

        public void SetName(string name) {
            if (_clusterAddresses.Count == 0) return;
            Load(0);
            ((FileHeaderCluster)_cluster).Name = name;
        }

        public void SetEndOfFile(long filePosition) {
            if (filePosition < 0) throw new ArgumentOutOfRangeException(nameof(filePosition));

            // Check to see if we have anything to do
            if (filePosition == _file.Length) return;

            int lastLogicalClusterNumber;
            int lastClusterSize;

            if (filePosition == 0) {
                lastLogicalClusterNumber = Constants.NoAddress;
                lastClusterSize = 0;
            } else {
                if (filePosition <= _firstClusterDataLength) {
                    lastLogicalClusterNumber = 0;
                    lastClusterSize = (int)filePosition;
                } else {
                    long offset = filePosition - _firstClusterDataLength - 1;
                    lastLogicalClusterNumber = (int)(offset / _clusterDataLength) + 1;
                    lastClusterSize = (int)(offset % _clusterDataLength) + 1;
                }
            }

            int clusterCountCompare = _clusterAddresses.Count.CompareTo(lastLogicalClusterNumber + 1);
            if (clusterCountCompare == -1) {
                // We need to add clusters
                if (!Load(lastLogicalClusterNumber)) throw new System.IO.IOException("Out of space");
                NextCluster = Constants.NoAddress;
                BytesUsed = lastClusterSize;

            } else if (clusterCountCompare == 0) {
                // We just need to resize the last cluster
                Load(_clusterAddresses.Count - 1);
                BytesUsed = lastClusterSize;

            } else {
                // We need to remove clusters
                for (int i = lastLogicalClusterNumber + 1; i < _clusterAddresses.Count; i++) _fileSystem.DeallocateCluster(_clusterAddresses[i]);
                _clusterAddresses = _clusterAddresses.Take(lastLogicalClusterNumber + 1).ToList();
                if (_logicalClusterNumber >= _clusterAddresses.Count) _logicalClusterNumber = Constants.NoAddress;

                if (_clusterAddresses.Count == 0) {
                    _logicalClusterNumber = Constants.NoAddress;
                } else {
                    Load(_clusterAddresses.Count - 1);
                    NextCluster = Constants.NoAddress;
                    BytesUsed = lastClusterSize;
                }
            }

            _file.Length = filePosition;
        }

        private List<int> _clustersToInitialize = new List<int>();

        private FileBaseCluster CreateCluster(int logicalClusterNumber) {
            // Make sure the requested cluster exists
            if (logicalClusterNumber < 0 || logicalClusterNumber >= _clusterAddresses.Count)
                throw new ArgumentOutOfRangeException();

            int clusterAddress = _clusterAddresses[logicalClusterNumber];

            FileBaseCluster fileCluster;

            if (logicalClusterNumber == 0) {
                FileHeaderCluster fileHeaderCluster = new FileHeaderCluster(clusterAddress);
                fileHeaderCluster.Initialize();
                fileHeaderCluster.ParentID = _file.ParentID;
                fileHeaderCluster.Name = _file.Name;
                fileCluster = fileHeaderCluster;
            } else {
                fileCluster = new FileDataCluster(clusterAddress);
                fileCluster.Initialize();
            }

            fileCluster.FileID = _file.ID;
            fileCluster.NextClusterAddress = _fileSystem.GetNextClusterAddress(clusterAddress);
            fileCluster.BytesUsed = _fileSystem.GetBytesUsed(clusterAddress);

            _logicalClusterNumber = logicalClusterNumber;
            return fileCluster;
        }

        protected bool Load(int logicalClusterNumber) {

            // If it's already load, do nothing
            if (_logicalClusterNumber == logicalClusterNumber) return true;

            // Add clusters if necessary
            while (logicalClusterNumber >= _clusterAddresses.Count) {
                int allocatedCluster = _fileSystem.AllocateCluster();
                if (allocatedCluster == Constants.NoAddress) return false;

                if (_clusterAddresses.Count == 0) {
                    _file.FirstCluster = allocatedCluster;
                } else {
                    Load(_clusterAddresses.Count - 1);
                    NextCluster = allocatedCluster;
                    BytesUsed = _cluster.Data.Length;
                    _isModified = true;
                }
                _clusterAddresses.Add(allocatedCluster);
            }

            // If there is already a cluster loaded, write it to disk (if modified)
            if (_logicalClusterNumber != Constants.NoAddress) Flush();

            // We start off with nothing being accessed in the cluster
            _bufferUsed = 0;

            // Create the cluster
            _cluster = CreateCluster(logicalClusterNumber);

            // The cluster has not been changed
            _isModified = false;

            return true;
        }

        private int read(byte[] buffer, int bufferOffset, int count, long filePosition) {
            if (filePosition >= _file.Length) return 0;

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (bufferOffset < 0 || bufferOffset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(bufferOffset));
            if (count < 0 || bufferOffset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            int clusterNumber;
            int clusterOffset;

            if (count == 0) return 0;

            if (filePosition < _firstClusterDataLength) {
                clusterNumber = 0;
                clusterOffset = (int)filePosition;
            } else {
                filePosition -= _firstClusterDataLength;
                clusterNumber = (int)(filePosition / _clusterDataLength) + 1;
                clusterOffset = (int)(filePosition % _clusterDataLength);
            }

            Load(clusterNumber);

            // The total number of bytes to read.
            int bytesToRead = Math.Min(count, _cluster.BytesUsed - clusterOffset);
            if (bytesToRead <= 0) return 0;

            if (clusterOffset + bytesToRead > _bufferUsed) fillFromDisk();

            _cluster.Data.ToByteArray(clusterOffset, buffer, bufferOffset, bytesToRead);

            return bytesToRead;
        }

        public int WriteFile(byte[] buffer, long filePosition) {
            int bufferOffset = 0;
            int count = buffer.Length;
            int totalBytesWritten = 0;

            while (count > 0) {
                int bytesWritten = write(buffer, bufferOffset, count, filePosition);
                if (bytesWritten == 0) return totalBytesWritten;
                bufferOffset += bytesWritten;
                filePosition += bytesWritten;
                totalBytesWritten += bytesWritten;
                count -= bytesWritten;
            }

            return totalBytesWritten;
        }

        private int write(byte[] source, int sourceOffset, int count, long filePosition) {

            Debug.Assert(source != null);
            Debug.Assert(sourceOffset >= 0 && sourceOffset <= source.Length);
            Debug.Assert(count >= 0 && sourceOffset + count <= source.Length);
            Debug.Assert(filePosition >= 0);

            if (count == 0) return 0;

            int clusterNumber;
            int clusterOffset;

            if (filePosition < _firstClusterDataLength) {
                clusterNumber = 0;
                clusterOffset = (int)filePosition;
            } else {
                long offsetFilePosition = filePosition - _firstClusterDataLength;
                clusterNumber = (int)(offsetFilePosition / _clusterDataLength) + 1;
                clusterOffset = (int)(offsetFilePosition % _clusterDataLength);
            }

            if (filePosition > _file.Length) SetEndOfFile(filePosition);
            if (!Load(clusterNumber)) return 0;

            // The total number of bytes to write, limited by the size of the buffer.  This should never be zero.
            int bytesToWrite = Math.Min(count, _cluster.Data.Length - clusterOffset);

            if (clusterOffset > _bufferUsed) fillFromDisk();

            _cluster.Data.Set(clusterOffset, source, sourceOffset, bytesToWrite);
            _bufferUsed = Math.Max(_bufferUsed, clusterOffset + bytesToWrite);
            int bytesAppended = Math.Max(0, _bufferUsed - _cluster.BytesUsed);
            if (bytesAppended > 0) {
                _file.Length += bytesAppended;
                _cluster.BytesUsed = _bufferUsed;
            }

            _isModified = true;
            return bytesToWrite;
        }

        private void fillFromDisk() {
            // This shouldn't be called unless there is a cluster loaded
            if (_logicalClusterNumber == Constants.NoAddress) throw new InvalidOperationException();
            // If there are no bytes to read, then do nothing.
            if (_bufferUsed == _cluster.BytesUsed) return;

            // If we've filled up this cluster with more data than was originally in it, there's no need to load.
            int clusterAddress = _clusterAddresses[_logicalClusterNumber];
            int oldClusterBytesUsed = _fileSystem.GetBytesUsed(clusterAddress);
            if (_bufferUsed >= oldClusterBytesUsed) return;

            // Load the cluster
            FileBaseCluster tempCluster = 
                _logicalClusterNumber == 0 ? 
                (FileBaseCluster)new FileHeaderCluster(clusterAddress) : 
                new FileDataCluster(clusterAddress);
            _fileSystem.ClusterIO.Load(tempCluster);

            // Fill up with data from the cluster, keeping in mind that we could have changed the cluster's bytes used.
            int bytesToFillFromCluster = Math.Min(oldClusterBytesUsed, _cluster.BytesUsed) - _bufferUsed;
            _cluster.Data.Set(_bufferUsed, tempCluster.Data, _bufferUsed, bytesToFillFromCluster);
            _bufferUsed += bytesToFillFromCluster;

            // If we've increased the number of bytes used so that it is more than what is contained in the cluster, zero the new data.
            int bytesToZero = Math.Max(_cluster.BytesUsed - oldClusterBytesUsed, 0);
            _cluster.Data.Clear(_bufferUsed, bytesToZero);
            _bufferUsed = _cluster.BytesUsed;
        }

        #endregion
        #region Fields

        private FileSystem _fileSystem;

        private bool _isDisposed = false;

//        private bool _isDataModified = false;

        private List<int> _clusterAddresses;

        private FileBaseCluster _cluster;

        private int _firstClusterDataLength;
        private int _clusterDataLength;

        private File _file;

        private int _bufferUsed;
        private bool _isModified;

        #endregion
    }
}
