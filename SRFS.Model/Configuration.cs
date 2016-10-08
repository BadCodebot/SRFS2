using SRFS.IO;
using System;

namespace SRFS.Model {

    public static class Configuration {

        public static void Reset() {
            _partition = null;
            _options = null;
            _cryptoSettings = null;
            _fileSystemID = null;
            _geometry = null;
            _volumeName = null;
        }

        public static Geometry Geometry {
            get {
                if (!IsGeometryInitialized) throw new InvalidOperationException();
                return _geometry;
            }
            set {
                if (IsGeometryInitialized) throw new InvalidOperationException();
                _geometry = value;
            }
        }

        public static Guid FileSystemID {
            get {
                if (!IsFileSystemIDInitialized) throw new InvalidOperationException();
                return _fileSystemID.Value;
            }
            set {
                if (IsFileSystemIDInitialized) throw new InvalidOperationException();
                _fileSystemID = value;
            }
        }

        public static CryptoSettings CryptoSettings {
            get {
                if (!IsCryptoSettingsInitialized) throw new InvalidOperationException();
                return _cryptoSettings;
            }
            set {
                if (IsCryptoSettingsInitialized) throw new InvalidOperationException();
                _cryptoSettings = value;
            }
        }

        public static Options Options {
            get {
                if (!IsOptionsInitialized) throw new InvalidOperationException();
                return _options.Value;
            }
            set {
                if (IsOptionsInitialized) throw new InvalidOperationException();
                _options = value;
            }
        }

        public static string VolumeName {
            get {
                if (!IsVolumeNameInitialized) throw new InvalidOperationException();
                return _volumeName;
            }
            set {
                if (IsVolumeNameInitialized) throw new InvalidOperationException();
                _volumeName = value;
            }
        }

        public static Partition Partition {
            get {
                if (!IsPartitionInitialized) throw new InvalidOperationException();
                return _partition;
            }
            set {
                if (IsPartitionInitialized) throw new InvalidOperationException();
                _partition = value;
            }
        }

        public static bool IsVolumeNameInitialized => _volumeName != null;

        public static bool IsGeometryInitialized => _geometry != null;

        public static bool IsFileSystemIDInitialized => _fileSystemID.HasValue;

        public static bool IsCryptoSettingsInitialized => _cryptoSettings != null;

        public static bool IsPartitionInitialized => _partition != null;

        public static bool IsOptionsInitialized => _options.HasValue;

        private static Partition _partition = null;
        private static Options? _options = null;
        private static CryptoSettings _cryptoSettings = null;
        private static Guid? _fileSystemID = null;
        private static Geometry _geometry = null;
        private static string _volumeName = null;
    }
}
