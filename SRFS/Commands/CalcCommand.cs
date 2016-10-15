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

    [Command(Name = "calc", Description = "Make an SRFS filesystem")]
    public class CalcCommand {

        [OptionGroup]
        public PartitionOptions PartitionOptions { get; private set; } = new PartitionOptions();

        [TypeConverter(typeof(ByteSizeConverter))]
        [Parameter(ShortForm = 's', LongForm = "bytesPerSlice", Type = "BYTES", IsRequired = true, Description = "bytes per slice")]
        public long BytesPerSlice { get; private set; }

        [Parameter(ShortForm = 'r', LongForm = "reedsolomon", Type = "n,k", IsRequired = true, Description = "Reed-Solomon parameters")]
        public ReedSolomon ReedSolomon { get; private set; }

        [Invoke]
        public void Invoke() {

            Partition p = PartitionOptions.GetPartition();

            if (BytesPerSlice > int.MaxValue) throw new CommandLineArgumentException("Argument to -s is too large.");
            int bytesPerSlice = (int)BytesPerSlice;

            int fileSystemHeaderClusterSize = FileSystemHeaderCluster.CalculateClusterSize(p.BytesPerBlock);
            long bytesAvailable = p.SizeBytes - fileSystemHeaderClusterSize;
            Geometry g = new Geometry(bytesPerSlice, ReedSolomon.N, ReedSolomon.K, 1);
            long totalTracks = bytesAvailable / g.CalculateTrackSizeBytes(p.BytesPerBlock);
            if (totalTracks > int.MaxValue)
                throw new CommandLineArgumentException("Too many tracks, please use a larger value of Reed Solomon N and/or bytesPerCluster.");
            g = new Geometry(bytesPerSlice, ReedSolomon.N, ReedSolomon.K, (int)totalTracks);
            Configuration.Geometry = g;

            Console.WriteLine($"Bytes per Cluster: {g.BytesPerCluster.ToFileSize()}");
            Console.WriteLine($"Data Clusters per Track: {g.DataClustersPerTrack}");
            Console.WriteLine($"Parity Clusters per Track: {g.ParityClustersPerTrack}");
            Console.WriteLine($"Total Clusters per Track: {g.ClustersPerTrack}");
            Console.WriteLine($"Resiliency: {(double)g.ParityClustersPerTrack / g.ClustersPerTrack:%#0.00}");
            Console.WriteLine($"Total Tracks: {g.TrackCount}");
            Console.WriteLine($"Bytes Used: {(g.CalculateFileSystemSize(p.BytesPerBlock)).ToFileSize()}");
            Console.WriteLine($"Bytes Available: {((long)g.DataClustersPerTrack * g.TrackCount * (g.BytesPerCluster - FileDataCluster.HeaderLength)).ToFileSize()}");
            Console.WriteLine($"Bytes Wasted: {(p.SizeBytes - g.CalculateFileSystemSize(p.BytesPerBlock)).ToFileSize()}");
        }
    }
}
