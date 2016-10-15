using Blocks.CommandLine;
using SRFS.IO;
using SRFS.Model;
using System;
using System.ComponentModel;
using SRFS.Model.Clusters;
using System.Security.Cryptography;
using System.Security;
using System.Text;

namespace SRFS.Commands {

    [Command(Name = "mkfs", Description = "Make an SRFS filesystem")]
    public class MkfsCommand {

        [OptionGroup]
        public PartitionOptions PartitionOptions { get; private set; } = new PartitionOptions();

        [OptionGroup]
        public CryptoSettingsOptions CryptoSettingsOptions { get; private set; } = new CryptoSettingsOptions();

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

            Partition p = PartitionOptions.GetPartition();

            if (BytesPerSlice > int.MaxValue) throw new CommandLineArgumentException("Argument to -s is too large.");
            int bytesPerSlice = (int)BytesPerSlice;

            long bytesAvailable = p.SizeBytes - FileSystemHeaderCluster.CalculateClusterSize(p.BytesPerBlock);
            Geometry g = new Geometry(bytesPerSlice, ReedSolomon.N, ReedSolomon.K, 1);

            long totalTracks = bytesAvailable / g.CalculateTrackSizeBytes(p.BytesPerBlock);

            if (totalTracks > int.MaxValue) throw new CommandLineArgumentException("Too many chains, please use a larger value of n (Reed Solomon parameter) and/or bytesPerSlice.");
            g = new Geometry(bytesPerSlice, ReedSolomon.N, ReedSolomon.K, (int)totalTracks);

            Configuration.Geometry = g;
            Configuration.VolumeName = VolumeName;
            Configuration.FileSystemID = Guid.NewGuid();

            Options options = Options.None;
            if (doNotVerifyHashes) options |= Options.DoNotVerifyClusterHashes;
            if (doNotVerifySignatures) options |= Options.DoNotVerifyClusterSignatures;
            Configuration.Options = options;

            CngKey encryptionKey = CryptoSettingsOptions.GetEncryptionKey();
            CngKey decryptionKey = CryptoSettingsOptions.GetDecryptionKey();
            CngKey signingKey = CryptoSettingsOptions.GetSigningKey();
            Configuration.CryptoSettings = new CryptoSettings(decryptionKey, signingKey, encryptionKey);

            using (var f = FileSystem.Create(new PartitionIO(p))) { }
        }
    }
}
