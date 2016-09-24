using System.Collections.Generic;
using System.Management;

namespace SRFS.IO {

    public class Drive {

        public Drive(ManagementObject m) {
            _managementObject = m;
        }

        public static IEnumerable<Drive> Drives {
            get {
                ManagementObjectSearcher s = new ManagementObjectSearcher("select * from Win32_DiskDrive");
                foreach (ManagementObject m in s.Get()) {
                    yield return new Drive(m);
                }
            }
        }

        public uint BytesPerSector {
            get {
                if (!_bytesPerSector.HasValue) _bytesPerSector = ((uint)_managementObject.Properties["BytesPerSector"].Value);
                return _bytesPerSector.Value;
            }
        }

        public uint Index {
            get {
                if (!_index.HasValue) _index = (uint)_managementObject["Index"];
                return _index.Value;
            }
        }

        public string Name {
            get {
                if (_name == null) _name = (string)_managementObject.Properties["Name"].Value;
                return _name;
            }
        }

        public string DeviceID {
            get {
                if (_deviceID == null) _deviceID = (string)_managementObject.Properties["DeviceID"].Value;
                return _deviceID;
            }
        }

        public string Caption {
            get {
                if (_caption == null) _caption = (string)_managementObject.Properties["Caption"].Value;
                return _caption;
            }
        }

        public ulong Size {
            get {
                if (!_size.HasValue) _size = (ulong)_managementObject.Properties["Size"].Value;
                return _size.Value;
            }
        }

        public string SerialNumber {
            get {
                if (_serialNumber == null) _serialNumber = (string)_managementObject.Properties["SerialNumber"].Value;
                return _serialNumber;
            }
        }

        public IEnumerable<Partition> Partitions {
            get {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{DeviceID}'}} WHERE ResultClass=Win32_DiskPartition");
                foreach (ManagementObject p in searcher.Get()) {
                    yield return new Partition(this, p);
                }
            }
        }

        public void Refresh() {
            _bytesPerSector = null;
            _index = null;
            _name = null;
            _deviceID = null;
            _caption = null;
            _size = null;
            _serialNumber = null;
        }

        private ManagementObject _managementObject;

        private uint? _bytesPerSector;
        private uint? _index;
        private string _name;
        private string _deviceID;
        private string _caption;
        private ulong? _size;
        private string _serialNumber;
    }
}
