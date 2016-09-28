using System;

namespace SRFS.IO {

    public class SuperBlockIO : IBlockIO {

        #region Construction / Destruction

        public SuperBlockIO(IBlockIO blockReaderWriter, int blockSizeBytes, int skipBytes = 0, bool shouldDisposeBlockReaderWriter = false) {
            _isDisposed = false;
            _blockReaderWriter = blockReaderWriter;
            _blockSizeBytes = blockSizeBytes;
            _shouldDisposeBlockReaderWriter = shouldDisposeBlockReaderWriter;
            _skipBytes = skipBytes;
        }

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    if (_shouldDisposeBlockReaderWriter) _blockReaderWriter.Dispose();
                }

                _isDisposed = true;
            }
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        #endregion
        #region Properties

        /// <inheritdoc />
        public int BlockSizeBytes => _blockSizeBytes;
        private int _blockSizeBytes;

        /// <inheritdoc />
        public long SizeBytes => _blockReaderWriter.SizeBytes;

        #endregion
        #region Methods

        /// <inheritdoc />
        public void Read(long position, byte[] buffer, int bufferOffset, long bytesToRead) {
            if (position + bytesToRead > SizeBytes) throw new System.IO.IOException();
            _blockReaderWriter.Read(position + _skipBytes, buffer, bufferOffset, bytesToRead);
        }

        /// <inheritdoc />
        public void Write(long position, byte[] buffer, int bufferOffset, long bytesToWrite) {
            if (position + bytesToWrite > SizeBytes) throw new System.IO.IOException();
            _blockReaderWriter.Write(position + _skipBytes, buffer, bufferOffset, bytesToWrite);
        }

        #endregion
        #region Fields

        private bool _isDisposed;
        private IBlockIO _blockReaderWriter;
        private int _blocksPerSuperBlock;
        private int _skipBytes;
        private bool _shouldDisposeBlockReaderWriter;

        #endregion
    }
}
