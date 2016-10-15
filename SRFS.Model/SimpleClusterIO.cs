using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model.Clusters;
using SRFS.IO;

namespace SRFS.Model {
    public class SimpleClusterIO : IClusterIO {

        public SimpleClusterIO(IBlockIO io, int skipBytes = 0) {
            if (io == null) throw new ArgumentNullException();
            if (Configuration.Geometry.BytesPerCluster % io.BlockSizeBytes != 0) throw new ArgumentException();
            if (skipBytes % io.BlockSizeBytes != 0) throw new ArgumentException();
            if (skipBytes < 0) throw new ArgumentOutOfRangeException();

            _skipBytes = skipBytes;
            _io = io;
            _buffer = new byte[Configuration.Geometry.BytesPerCluster];
        }

        public virtual void Load(Cluster c) {
            lock (_lock) {
                _io.Read(c.AbsoluteAddress + _skipBytes, _buffer, 0, Configuration.Geometry.BytesPerCluster);
                c.Load(_buffer, 0);
            }
        }

        public virtual void Save(Cluster c) {
            lock (_lock) {
                c.Save(_buffer, 0);
                _io.Write(c.AbsoluteAddress + _skipBytes, _buffer, 0, Configuration.Geometry.BytesPerCluster);
            }
        }

        private object _lock = new object();
        private byte[] _buffer;
        private IBlockIO _io;
        private long _skipBytes;
    }
}
