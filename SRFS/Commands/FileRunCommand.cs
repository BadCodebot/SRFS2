using Blocks.CommandLine;
using SRFS.IO;
using SRFS.Model;
using System;
using System.ComponentModel;
using SRFS.Model.Clusters;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using DokanNet;
using System.IO;

namespace SRFS.Commands {

    [Command(Name = "fileRun", Description = "Mount an SRFS filesystem from a file")]
    public class FileRunCommand {

        [Parameter(ShortForm = 'f', LongForm = "filePath", Type = "STRING", IsRequired = true, Description = "Filename")]
        public string FilePath { get; private set; }

        [OptionGroup]
        public CryptoSettingsOptions CryptoSettingsOptions { get; private set; } = new CryptoSettingsOptions();

        [Switch(ShortForm = 'h', LongForm = "skipHashVerify", Description = "Skip verification of cluster hashes")]
        public bool doNotVerifyHashes { get; private set; } = false;

        [Switch(ShortForm = 'g', LongForm = "skipSignatureVerify", Description = "Skip verification of cluster signatures")]
        public bool doNotVerifySignatures { get; private set; } = false;

        [Invoke]
        public void Invoke() {

            int blockSize = 512;

            Options options = Options.None;
            if (doNotVerifyHashes) options |= Options.DoNotVerifyClusterHashes;
            if (doNotVerifySignatures) options |= Options.DoNotVerifyClusterSignatures;
            Configuration.Options = options;

            CngKey encryptionKey = CryptoSettingsOptions.GetEncryptionKey();
            CngKey decryptionKey = CryptoSettingsOptions.GetDecryptionKey();
            CngKey signingKey = CryptoSettingsOptions.GetSigningKey();
            Configuration.CryptoSettings = new CryptoSettings(decryptionKey, signingKey, encryptionKey);

            long size = new FileInfo(FilePath).Length;

            using (MemoryIO io = new MemoryIO((int)size, blockSize))
            using (FileStream fio = new FileStream(FilePath, FileMode.Open)) {
                fio.Read(io.Bytes, 0, (int)io.SizeBytes);

                using (var fs = FileSystem.Mount(io)) {
                    SRFSDokan d = new SRFSDokan(fs);
                    d.Mount("S:\\");
                }

                fio.Position = 0;
                fio.Write(io.Bytes, 0, (int)io.SizeBytes);
            }
        }
    }
}
