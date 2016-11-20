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

namespace SRFS.Commands {

    [Command(Name = "verify", Description = "Mount an SRFS filesystem")]
    public class VerifyCommand {

        [OptionGroup]
        public PartitionOptions PartitionOptions { get; private set; } = new PartitionOptions();

        [OptionGroup]
        public CryptoSettingsOptions CryptoSettingsOptions { get; private set; } = new CryptoSettingsOptions();

        [Switch(ShortForm = 'h', LongForm = "skipHashVerify", Description = "Skip verification of cluster hashes")]
        public bool doNotVerifyHashes { get; private set; } = false;

        [Switch(ShortForm = 'g', LongForm = "skipSignatureVerify", Description = "Skip verification of cluster signatures")]
        public bool doNotVerifySignatures { get; private set; } = false;

        private IEnumerable<int> getFileSequence(FileSystem fs, File f) {
            int i = f.FirstCluster;
            while (i != Constants.NoAddress) {
                yield return i;
                i = fs.GetNextClusterAddress(i);
            }
        }

        private IEnumerable<int> getMismatchClusters(FileSystem fs, ClusterState[] states) {
            for (int i = 0; i < fs.NumberOfDataClusters; i++) {
                if (states[i].IsUsed() != fs.GetClusterState(i).IsUsed()) yield return i;
            }
        }

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

                int nClusters = fs.NumberOfDataClusters;
                ClusterState[] states = new ClusterState[nClusters];
                for (int i = 0; i < nClusters; i++) states[i] = ClusterState.None;

                foreach (var f in fs.Files) {
                    foreach (var i in getFileSequence(fs, f)) states[i] = ClusterState.Used;
                }

                var systemClusters = from x in Enumerable.Range(0, fs.NumberOfDataClusters)
                                     where fs.GetClusterState(x).IsSystem()
                                     select x;
                Console.WriteLine($"System Clusters: {systemClusters.ToRanges()}");

                Console.WriteLine($"  Cluster States Table: {fs.ClusterStatesTableClusters.ToRanges()}");
                foreach (var i in fs.ClusterStatesTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine($"  Next Cluster Table: {fs.NextClusterTableClusters.ToRanges()}");
                foreach (var i in fs.NextClusterTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine($"  Bytes Used Table: {fs.BytesUsedTableClusters.ToRanges()}");
                foreach (var i in fs.BytesUsedTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine($"  Verify Time Table: {fs.VerifyTimeTableClusters.ToRanges()}");
                foreach (var i in fs.VerifyTimeTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine($"  Directory Table: {fs.DirectoryTableClusters.ToRanges()}");
                foreach (var i in fs.DirectoryTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine($"  File Table: {fs.FileTableClusters.ToRanges()}");
                foreach (var i in fs.FileTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine($"  Access Table: {fs.AccessTableClusters.ToRanges()}");
                foreach (var i in fs.AccessTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine($"  Audit Table: {fs.AuditTableClusters.ToRanges()}");
                foreach (var i in fs.AuditTableClusters) states[i] = ClusterState.Used | ClusterState.System;

                Console.WriteLine();

                var dataClusters = from x in Enumerable.Range(0, fs.NumberOfDataClusters)
                                   where (fs.GetClusterState(x) & ClusterState.Data) != 0
                                   select x;
                Console.WriteLine($"Data Clusters: {dataClusters.ToRanges()}");

                var unwrittenClusters = from x in Enumerable.Range(0, fs.NumberOfDataClusters)
                                        where (fs.GetClusterState(x) & ClusterState.Unwritten) != 0
                                        select x;
                Console.WriteLine($"Unwritten Clusters: {unwrittenClusters.ToRanges()}");

                var usedClusters = from x in Enumerable.Range(0, fs.NumberOfDataClusters)
                                   where fs.GetClusterState(x).IsUsed()
                                   select x;
                Console.WriteLine($"Used Clusters: {usedClusters.ToRanges()}");

                var modifiedClusters = from x in Enumerable.Range(0, fs.NumberOfDataClusters)
                                       where fs.GetClusterState(x).IsModified()
                                       select x;
                Console.WriteLine($"Modified Clusters: {modifiedClusters.ToRanges()}");

                int n = fs.ClusterStates.Count();

                var parityClusters = from x in Enumerable.Range(0, n)
                                     where fs.GetClusterState(x).IsParity()
                                     select x;
                Console.WriteLine($"Parity Clusters: {parityClusters.ToRanges()}");

                var nullClusters = from x in Enumerable.Range(0, n)
                                     where (fs.GetClusterState(x) & ClusterState.Null) != 0
                                     select x;
                Console.WriteLine($"Null Clusters: {nullClusters.ToRanges()}");

                var m = from x in Enumerable.Range(0, fs.NumberOfDataClusters)
                        where states[x].IsUsed() != fs.GetClusterState(x).IsUsed()
                        select x;
                Console.WriteLine($"Mismatch: {m.ToRanges()}");

                Console.WriteLine();

                var usedTracks =
                    from t in
                        from x in Enumerable.Range(0, Configuration.Geometry.TrackCount)
                        select new Track(fs, x)
                    where t.Used
                    select t.Number;
                Console.WriteLine($"Used Tracks: {usedTracks.ToRanges()}");

                var upToDateTracks =
                    from t in
                        from x in Enumerable.Range(0, Configuration.Geometry.TrackCount)
                        select new Track(fs, x)
                    where t.UpToDate
                    select t.Number;
                Console.WriteLine($"Up-to-date Tracks: {upToDateTracks.ToRanges()}");

            }
        }
    }
}
