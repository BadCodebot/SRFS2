using System;

namespace SRFS.IO {

    public class SuperBlockIO : IBlockIO {

        #region Construction / Destruction

        public SuperBlockIO(IBlockIO blockReaderWriter, int blocksPerSuperBlock, int skip = 0, bool shouldDisposeBlockReaderWriter = false) {
            _isDisposed = false;
            _blockReaderWriter = blockReaderWriter;
            _blocksPerSuperBlock = blocksPerSuperBlock;
            _shouldDisposeBlockReaderWriter = shouldDisposeBlockReaderWriter;
            _skip = skip;
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
        public int BlockSizeBytes => _blocksPerSuperBlock * _blockReaderWriter.BlockSizeBytes;

        /// <inheritdoc />
        public int NBlocks => (_blockReaderWriter.NBlocks - _skip) / _blocksPerSuperBlock;

        #endregion
        #region Methods

        /// <inheritdoc />
        public void Read(long position, byte[] buffer, int bufferOffset, int blockCount) {
            if (position + blockCount > NBlocks) throw new System.IO.IOException();
            _blockReaderWriter.Read(position * _blocksPerSuperBlock + _skip, buffer, bufferOffset, blockCount * _blocksPerSuperBlock);
        }

        /// <inheritdoc />
        public void Write(long position, byte[] buffer, int bufferOffset, int blockCount) {
            if (position + blockCount > NBlocks) throw new System.IO.IOException();
            _blockReaderWriter.Write(position * _blocksPerSuperBlock + _skip, buffer, bufferOffset, blockCount * _blocksPerSuperBlock);
        }

        #endregion
        #region Fields

        private bool _isDisposed;
        private IBlockIO _blockReaderWriter;
        private int _blocksPerSuperBlock;
        private int _skip;
        private bool _shouldDisposeBlockReaderWriter;

        #endregion
    }
}
