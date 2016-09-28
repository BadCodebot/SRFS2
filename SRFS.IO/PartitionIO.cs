using Blocks.Windows;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SRFS.IO {

    public unsafe class PartitionIO : IBlockIO {

        public PartitionIO(Partition partition) {

            _partition = partition;

            _handle = Kernel32.CreateFile(
                _partition.Drive.DeviceID,
                Kernel32.EFileAccess.GenericRead | Kernel32.EFileAccess.GenericWrite,
                Kernel32.EFileShare.Read | Kernel32.EFileShare.Write,
                IntPtr.Zero,
                Kernel32.ECreationDisposition.OpenExisting,
                Kernel32.EFileAttributes.Normal,
                IntPtr.Zero);

            var e = Marshal.GetLastWin32Error();
            if (e != 0) throw new System.IO.IOException("Create File Exception", e);

            // Make sure our cast in the BlockSize property will work
            if (partition.Drive.BytesPerSector > int.MaxValue) throw new OverflowException();
            if (partition.SizeBytes / partition.Drive.BytesPerSector > int.MaxValue) throw new OverflowException();
        }

        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                if (disposing) { }

                _handle.Dispose();
                isDisposed = true;
            }
        }

        ~PartitionIO() {
            Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public int BlockSizeBytes => (int)_partition.Drive.BytesPerSector;

        /// <inheritdoc />
        public long SizeBytes => _partition.SizeBytes;

        private void setPosition(long position) {
            if (!Kernel32.SetFilePointerEx(_handle, _partition.StartingOffset + position, null, Kernel32.EMoveMethod.Begin)) {
                throw new IOException("SetFilePointerEx failed.", Marshal.GetLastWin32Error());
            }
        }

        /// <inheritdoc />
        public void Read(long position, byte[] buffer, int offset, long bytesToRead) {
            if (bytesToRead < 0) throw new ArgumentOutOfRangeException(nameof(bytesToRead));
            uint bytesToReadUInt = (uint)bytesToRead;

            lock (_handle) {
                setPosition(position);

                fixed (byte* pBuffer = buffer) {
                    uint bytesRead;

                    if (!Kernel32.ReadFile(_handle, pBuffer + offset, bytesToReadUInt, &bytesRead, null)) {
                        int error = Marshal.GetLastWin32Error();
                        throw new IOException("ReadFile call failed.", error);
                    }

                    if (bytesToRead != bytesRead) throw new IOException();
                }
            }
        }

        /// <inheritdoc />
        public void Write(long position, byte[] buffer, int offset, long bytesToWrite) {
            if (bytesToWrite < 0) throw new ArgumentOutOfRangeException(nameof(bytesToWrite));
            uint bytesToWriteUInt = (uint)bytesToWrite;

            lock (_handle) {
                setPosition(position);

                fixed (byte* pBuffer = buffer) {
                    uint bytesWritten;

                    if (!Kernel32.WriteFile(_handle, pBuffer + offset, bytesToWriteUInt, &bytesWritten, null)) {
                        int error = Marshal.GetLastWin32Error();
                        throw new IOException("ReadFile call failed.", error);
                    }

                    // If this throws, we are in an invalid state
                    if (bytesToWrite != bytesWritten) throw new IOException();
                }
            }
        }

        public Partition PartitionInfo => _partition;
        private Partition _partition;
        private SafeFileHandle _handle;
        private bool isDisposed = false;
    }
}
