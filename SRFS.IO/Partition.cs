using System;
using System.Linq;
using System.Management;

namespace SRFS.IO {

    public class Partition {

        public Partition(Drive d, ManagementObject m) {
            _drive = d;
            _managementObject = m;
        }

        public uint Index {
            get {
                if (!_index.HasValue) _index = (uint)_managementObject["Index"];
                return _index.Value;
            }
        }

        public int BytesPerBlock {
            get {
                if (!_bytesPerBlock.HasValue) _bytesPerBlock = (int)((ulong)_managementObject.Properties["BlockSize"].Value);
                return _bytesPerBlock.Value;
            }
        }

        public long SizeBytes {
            get {
                if (!_sizeBytes.HasValue) _sizeBytes = (long)((UInt64)_managementObject.Properties["Size"].Value);
                return _sizeBytes.Value;
            }
        }

        public long StartingOffset {
            get {
                if (!_startingOffset.HasValue) _startingOffset = (long)((UInt64)_managementObject.Properties["StartingOffset"].Value);
                return _startingOffset.Value;
            }
        }

        public string DeviceID {
            get {
                if (_deviceID == null) _deviceID = (string)(_managementObject.Properties["DeviceID"].Value);
                return _deviceID;
            }
        }

        public Drive Drive => _drive;

        public string LogicalDriveLetter {
            get {
                if (_logicalDriveLetter == null) _logicalDriveLetter = 
                        string.Join(", ", from l in _managementObject.GetRelated("Win32_LogicalDisk").Cast<ManagementBaseObject>() select l["Name"]);
                return _logicalDriveLetter;
            }
        }

        public void Refresh() {
            _bytesPerBlock = null;
            _sizeBytes = null;
            _startingOffset = null;
            _deviceID = null;
            _index = null;
            _logicalDriveLetter = null;
        }

        private readonly ManagementObject _managementObject;
        private readonly Drive _drive;

        private int? _bytesPerBlock;
        private long? _sizeBytes;
        private long? _startingOffset;
        private string _deviceID;
        private uint? _index;
        private string _logicalDriveLetter;
    }
}
