using Blocks.CommandLine;
using SRFS.IO;
using System;
using System.Linq;

namespace SRFS.Commands {

    public class PartitionOptions {

        public const string DRIVE_NAME_ENV_VARIABLE = "SRFS_DRIVE_NAME";
        public const string PARTITION_NUMBER_ENV_VARIABLE = "SRFS_PARTITION_NUMBER";

        [Parameter(ShortForm = 'd', LongForm = "drive", Type = "NAME", Description = "drive name", EnvironmentalVariable = DRIVE_NAME_ENV_VARIABLE)]
        public string DriveName { get; protected set; }

        [Parameter(ShortForm = 'p', LongForm = "partition", Description = "partition number", EnvironmentalVariable = PARTITION_NUMBER_ENV_VARIABLE)]
        public int? PartitionNumber { get; protected set; }

        public Partition GetPartition() {
            bool usingDriveNumber = false;
            int driveNumber = 0;

            Drive d = (from x in Drive.Drives where x.SerialNumber.Trim() == DriveName select x).FirstOrDefault();
            if (d == null) {
                if (usingDriveNumber = int.TryParse(DriveName, out driveNumber)) d = (from x in Drive.Drives where x.Index == driveNumber select x).FirstOrDefault();
                if (d == null) throw new ArgumentException($"Drive {(usingDriveNumber ? driveNumber.ToString() : $"\"{DriveName}\"")} not found.");
            }

            Partition p = (from x in d.Partitions where x.Index == PartitionNumber select x).FirstOrDefault();
            if (p == null) throw new ArgumentException($"Partition {PartitionNumber} for drive {(usingDriveNumber ? driveNumber.ToString() : $"\"{DriveName}\"")} not found.");

            return p;
        }
    }
}
