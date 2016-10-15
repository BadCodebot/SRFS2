using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model;
using System.Security.Cryptography;
using SRFS.IO;

namespace SRFS.Tests.Model {
    public class ConfigurationTest {

        private static int BlockSize = 512;
        private static int BytesPerCluster = 16 * 1024;
        private static int ClustersPerTrack = 256;
        private static int DataClustersPerTrack = 256 - 32;
        private static int TrackCount = 32;

        public static MemoryIO CreateMemoryIO() {
            return new MemoryIO(RequiredSize, BlockSize);
        }

        public static int RequiredSize {
            get {
                return (BytesPerCluster + 1024) * ClustersPerTrack * TrackCount + 2 * BlockSize;
            }
        }

        public static void Initialize() {
            if (_isInitialized) return;

            Configuration.Geometry = new Geometry(BytesPerCluster, ClustersPerTrack, DataClustersPerTrack, TrackCount);
            Configuration.FileSystemID = Guid.NewGuid();
            Configuration.VolumeName = "Memory Test Volume";
            Configuration.Options = Options.None;
            CngKey pk = CngKey.Create(CngAlgorithm.ECDiffieHellmanP521, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport });
            Configuration.CryptoSettings = new CryptoSettings(pk, pk, pk);

            _isInitialized = true;
        }

        private static bool _isInitialized = false;
    }
}
