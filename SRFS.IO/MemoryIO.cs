using Blocks.Windows;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SRFS.IO {

    public class MemoryIO : IBlockIO {

        public MemoryIO(int size, int blockSize) {
            _memory = new byte[size];
            _blockSize = blockSize;
        }

        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                if (disposing) { }
                isDisposed = true;
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public int BlockSizeBytes => _blockSize;

        /// <inheritdoc />
        public long SizeBytes => _memory.Length;

        /// <inheritdoc />
        public void Read(long position, byte[] buffer, int offset, long bytesToRead) {
            if (bytesToRead < 0) throw new ArgumentOutOfRangeException(nameof(bytesToRead));

            Buffer.BlockCopy(_memory, (int)position, buffer, offset, (int)bytesToRead);
        }

        /// <inheritdoc />
        public void Write(long position, byte[] buffer, int offset, long bytesToWrite) {
            if (bytesToWrite < 0) throw new ArgumentOutOfRangeException(nameof(bytesToWrite));

            Buffer.BlockCopy(buffer, offset, _memory, (int)position, (int)bytesToWrite);
        }

        private bool isDisposed = false;

        public byte[] Bytes => _memory;

        private byte[] _memory;
        private int _blockSize;
    }
}
