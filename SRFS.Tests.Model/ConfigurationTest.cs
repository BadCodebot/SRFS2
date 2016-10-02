using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model;
using System.Security.Cryptography;

namespace SRFS.Tests.Model {
    public class ConfigurationTest {

        public static void Initialize() {
            if (_isInitialized) return;

            Configuration.Geometry = new Geometry(1024 * 1024, 10, 9, 2);
            Configuration.FileSystemID = new Guid();
            Configuration.VolumeName = "Memory Test Volume";
            Configuration.Options = Options.None;
            CngKey pk = CngKey.Create(CngAlgorithm.ECDiffieHellmanP521, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport });
            Configuration.CryptoSettings = new CryptoSettings(pk, pk, pk);

            _isInitialized = true;
        }

        private static bool _isInitialized = false;
    }
}
