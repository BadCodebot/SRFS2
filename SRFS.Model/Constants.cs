using System;

namespace SRFS.Model {

    public static class Constants {

        /// <summary>
        /// The ASCII sequence "SRFS" meant to identify the cluster as belonging to the SRFS filesystem.  It appears as the first four bytes of the header.  This
        /// should remain consistent in all versions of the file system.
        /// </summary>
        public static byte[] SrfsMarker { get; } = new byte[] { (byte)'S', (byte)'R', (byte)'F', (byte)'S' };

        /// <summary>
        /// A 2 byte sequence representing the version number of the code which wrote the sector. In order it is Major then Minor. The current version is "1.0".  This is always
        /// the fifth and sixth bytes of the header regardless of version.  The remaining fields may vary with different versions, however.
        /// </summary>
        public static byte[] CurrentVersion { get; } = new byte[] { 2, 0 };

        public const int NoID = -1;

        public const int NoAddress = -1;

        public static readonly DateTime DefaultWriteTime = DateTime.MinValue;

        public const int GuidLength = 16;
    }
}
