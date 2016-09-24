using System;

namespace SRFS.IO {

    /// <summary>
    /// This interface represents a class that reads and writes data in blocks.
    /// </summary>
    /// <remarks>
    /// All operations are done on a block-by-block bases.  That is, it reads and writes full blocks only and the
    /// position is set as a number of blocks.  There is no mechanism to add or delete blocks.
    /// </remarks>
    public interface IBlockIO : IDisposable {

        /// <summary>
        /// The number of bytes per block.
        /// </summary>
        int BlockSizeBytes { get; }

        /// <summary>
        /// The number of blocks available.
        /// </summary>
        int NBlocks { get; }

        void Read(long position, byte[] buffer, int bufferOffset, int blockCount);

        void Write(long position, byte[] buffer, int bufferOffset, int blockCount);
    }
}
