using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model;
using System.ComponentModel;
using Blocks.CommandLine;
using SRFS.IO;

namespace SRFS.Commands {

    [Command(Name = "listPartitions", Description = "List the available partitions")]
    public class ListPartitionsCommand {

        [Invoke]
        public void Invoke() {
            foreach (Drive d in Drive.Drives.OrderBy(x => x.DeviceID)) {
                Console.WriteLine($"Disk {d.Index} ({d.SerialNumber.Trim()}, {d.Caption}, {d.Size.ToFileSize()})");
                foreach (Partition p in d.Partitions) {
                    Console.WriteLine($"  Partition {p.Index} ({(p.LogicalDriveLetter.Length > 0 ? $"{p.LogicalDriveLetter}, " : "")}{p.SizeBytes.ToFileSize()})");
                }
            }
        }
    }
}
