using Blocks.CommandLine;
using SRFS.IO;
using SRFS.Model;
using System;
using System.ComponentModel;
using SRFS.Model.Clusters;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using System.IO;

namespace SRFS.Commands {

    [Command(Name = "filemkfs", Description = "Make an SRFS filesystem as a file")]
    public class FileMkfsCommand {

        [Parameter(ShortForm = 'f', LongForm = "filePath", Type ="STRING", IsRequired = true, Description = "Filename")]
        public string FilePath { get; private set; }

        [OptionGroup]
        public CryptoSettingsOptions CryptoSettingsOptions { get; private set; } = new CryptoSettingsOptions();

        [Parameter(ShortForm = 'c', LongForm = "trackCount", Type = "INT", IsRequired = true, Description = "Number of tracks")]
        public int TrackCount { get; private set; }

        [TypeConverter(typeof(ByteSizeConverter))]
        [Parameter(ShortForm = 's', LongForm = "bytesPerSlice", Type = "BYTES", IsRequired = true, Description = "bytes per slice")]
        public long BytesPerSlice { get; private set; }

        [Parameter(ShortForm = 'r', LongForm = "reedsolomon", Type = "n,k", IsRequired = true, Description = "Reed-Solomon parameters")]
        public ReedSolomon ReedSolomon { get; private set; }

        [Parameter(ShortForm = 'n', LongForm = "volumeName", Type = "STRING", IsRequired = true, Description = "Volume name")]
        public string VolumeName { get; private set; }

        [Switch(ShortForm = 'h', LongForm = "skipHashVerify", Description = "Skip verification of cluster hashes")]
        public bool doNotVerifyHashes { get; private set; } = false;

        [Switch(ShortForm = 'g', LongForm = "skipSignatureVerify", Description = "Skip verification of cluster signatures")]
        public bool doNotVerifySignatures { get; private set; } = false;

        [Invoke]
        public void Invoke() {

            int blockSize = 512;

            Configuration.VolumeName = FilePath;

            if (BytesPerSlice > int.MaxValue) throw new CommandLineArgumentException("Argument to -s is too large.");
            int bytesPerSlice = (int)BytesPerSlice;

            Geometry g = new Geometry(bytesPerSlice, ReedSolomon.N, ReedSolomon.K, TrackCount);
            Configuration.Geometry = g;

            Configuration.FileSystemID = Guid.NewGuid();

            Options options = Options.None;
            if (doNotVerifyHashes) options |= Options.DoNotVerifyClusterHashes;
            if (doNotVerifySignatures) options |= Options.DoNotVerifyClusterSignatures;
            Configuration.Options = options;

            CngKey encryptionKey = CryptoSettingsOptions.GetEncryptionKey();
            CngKey decryptionKey = CryptoSettingsOptions.GetDecryptionKey();
            CngKey signingKey = CryptoSettingsOptions.GetSigningKey();
            Configuration.CryptoSettings = new CryptoSettings(decryptionKey, signingKey, encryptionKey);

            long size = g.CalculateFileSystemSize(blockSize);
            if (size > int.MaxValue) throw new CommandLineArgumentException("File system size is too large.");

            using (MemoryIO io = new MemoryIO((int)size, blockSize))
            using (FileStream fio = new FileStream(FilePath, FileMode.Create, FileAccess.Write)) {
                using (var f = FileSystem.Create(io)) { }
                fio.Write(io.Bytes, 0, (int)io.SizeBytes);
            }
        }
    }
}
