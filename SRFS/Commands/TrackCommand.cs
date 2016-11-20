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
using System.Collections.Generic;
using SRFS.Model.Data;
using System.Collections;
using System.Linq;
using SRFS.ReedSolomon;

namespace SRFS.Commands {

    [Command(Name = "track", Description = "Mount an SRFS filesystem")]
    public class TrackCommand {

        [OptionGroup]
        public PartitionOptions PartitionOptions { get; private set; } = new PartitionOptions();

        [OptionGroup]
        public CryptoSettingsOptions CryptoSettingsOptions { get; private set; } = new CryptoSettingsOptions();

        [Switch(ShortForm = 'h', LongForm = "skipHashVerify", Description = "Skip verification of cluster hashes")]
        public bool doNotVerifyHashes { get; private set; } = false;

        [Switch(ShortForm = 'g', LongForm = "skipSignatureVerify", Description = "Skip verification of cluster signatures")]
        public bool doNotVerifySignatures { get; private set; } = false;

        [Parameter(ShortForm = 't', LongForm = "track", Type = "INT", Description = "track number", IsRequired = true)]
        public int Track { get; private set; }

        [Parameter(ShortForm = 'n', LongForm = "count", Type = "INT", Description = "track count", IsRequired = false)]
        public int TrackCount { get; private set; } = 1;

        [Switch(ShortForm = 'c', LongForm = "calculateParity", Description = "Calculate Parity")]
        public bool CalculateParity { get; private set; } = false;

        [Switch(ShortForm = 'v', LongForm = "verifyParity", Description = "Verify Parity")]
        public bool VerifyParity { get; private set; } = false;

        [Switch(ShortForm = 'r', LongForm = "repair", Description = "Repair")]
        public bool Repair { get; private set; } = false;

        [Switch(ShortForm = 'f', LongForm = "force", Description = "Force")]
        public bool Force { get; private set; } = false;

        [Invoke]
        public void Invoke() {

            Options options = Options.None;
            if (doNotVerifyHashes) options |= Options.DoNotVerifyClusterHashes;
            if (doNotVerifySignatures) options |= Options.DoNotVerifyClusterSignatures;
            Configuration.Options = options;

            CngKey encryptionKey = CryptoSettingsOptions.GetEncryptionKey();
            CngKey decryptionKey = CryptoSettingsOptions.GetDecryptionKey();
            CngKey signingKey = CryptoSettingsOptions.GetSigningKey();
            Configuration.CryptoSettings = new CryptoSettings(decryptionKey, signingKey, encryptionKey);

            using (var pio = new PartitionIO(PartitionOptions.GetPartition()))
            using (var fs = FileSystem.Mount(pio)) {
                for (int tn = Track; tn < Track + TrackCount; tn++) {
                    Track t = new Track(fs, tn);

                    Console.WriteLine($"Track Number: {t.Number}");
                    Console.WriteLine($"Used: {t.Used}");
                    Console.WriteLine($"Parity Written: {t.ParityWritten}");
                    Console.WriteLine($"Up-to-date: {t.UpToDate}");

                    Console.WriteLine();

                    Console.WriteLine($"Data Clusters: {t.DataClusters.ToRanges()}");
                    var c = from i in t.DataClusters where fs.GetClusterState(i).IsUsed() select i;
                    Console.WriteLine($"  Used ({c.Count()}): {c.ToRanges()}");
                    c = from i in t.DataClusters where fs.GetClusterState(i).IsModified() select i;
                    Console.WriteLine($"  Modified ({c.Count()}): {c.ToRanges()}");
                    c = from i in t.DataClusters where fs.GetClusterState(i).IsUnwritten() select i;
                    Console.WriteLine($"  Unwritten ({c.Count()}): {c.ToRanges()}");

                    Console.WriteLine();

                    Console.WriteLine($"Parity Clusters: {t.ParityClusters.ToRanges()}");
                    c = from i in t.ParityClusters where fs.GetClusterState(i).IsUnwritten() select i;
                    Console.WriteLine($"  Unwritten ({c.Count()}): {c.ToRanges()}");

                    if (CalculateParity) t.UpdateParity(Force);
                    if (VerifyParity) {
                        Console.WriteLine("Verifying Parity");
                        if (t.VerifyParity()) Console.WriteLine("Verified OK");
                        else Console.WriteLine("Corrupt");
                    } else if (Repair) {
                        Console.WriteLine("Repairing");
                        if (t.Repair()) Console.WriteLine("Repair OK");
                        else Console.WriteLine("Repair Failed");
                    }
                    if (CalculateParity || Repair) fs.Flush();
                }
            }
        }
    }
}
