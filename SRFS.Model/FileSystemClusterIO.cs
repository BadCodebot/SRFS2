using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.IO;

namespace SRFS.Model {

    public class FileSystemClusterIO : SimpleClusterIO {

        public FileSystemClusterIO(FileSystem fs, IBlockIO io) : base(io, FileSystemHeaderCluster.CalculateClusterSize(io.BlockSizeBytes)) {
            _fileSystem = fs;
        }

        public override void Load(Cluster c) {
            if (c is FileBaseCluster fb) {
                Console.WriteLine($"Loading FileBaseCluster {fb.Address}");
            } else if (c is ArrayCluster a) {
                Console.WriteLine($"Loading ArrayCluster {a.Address}");
            }
            base.Load(c);
        }
        public override void Save(Cluster c) {
            if (_fileSystem.ReadOnly) throw new NotSupportedException();

            base.Save(c);
            if (c is FileBaseCluster fb) {
                Console.WriteLine($"Saved FileBaseCluster {fb.Address}");
                _fileSystem.SetBytesUsed(fb.Address, fb.BytesUsed);
                _fileSystem.SetNextClusterAddress(fb.Address, fb.NextClusterAddress);
                _fileSystem.SetClusterState(fb.Address, _fileSystem.GetClusterState(fb.Address) | ClusterState.Modified);
            } else if (c is ArrayCluster a) {
                Console.WriteLine($"Saved ArrayCluster {a.Address}");
                _fileSystem.SetNextClusterAddress(a.Address, a.NextClusterAddress);
                _fileSystem.SetClusterState(a.Address, _fileSystem.GetClusterState(a.Address) | ClusterState.Modified);
            }
        }

        private FileSystem _fileSystem;
    }
}
