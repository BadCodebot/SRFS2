using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.IO;

namespace SRFS.Model {

    public static class Configuration {

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

        public static bool IsGeometryInitialized => _geometry != null;

        private static Geometry _geometry = null;

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

        public static bool IsFileSystemIDInitialized => _fileSystemID.HasValue;

        private static Guid? _fileSystemID = null;

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

        public static bool IsCryptoSettingsInitialized => _cryptoSettings != null;

        private static CryptoSettings _cryptoSettings = null;

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

        private static bool IsOptionsInitialized => _options.HasValue;

        private static Options? _options = null;

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

        public static bool IsPartitionInitialized => _partition != null;

        private static Partition _partition = null;
    }

    public class Geometry {

        public Geometry(int bytesPerCluster) {
            _bytesPerCluster = bytesPerCluster;
        }

        public int BytesPerCluster => _bytesPerCluster;
        private readonly int _bytesPerCluster;
    }
}
