using System;
using System.Runtime.InteropServices;

namespace SRFS.ReedSolomon {

    public unsafe class Syndrome : IDisposable {

        public Syndrome(int nDataCodewords, int nParityCodewords, int codewordsPerSlice) {
            _rsp = Syndrome_Construct((uint)nDataCodewords, (uint)nParityCodewords, (uint)codewordsPerSlice);
        }

        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                if (disposing) { }
                Syndrome_Destruct(_rsp);
                isDisposed = true;
            }
        }

        ~Syndrome() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void AddCodewordSlice(byte[] data, int offset, int exponent) {
            fixed (byte* pData = data) {
                Syndrome_AddCodewordSlice(_rsp, (ushort*)(pData + offset), (uint)exponent);
            }
        }

        public void AddCodewordSlice(ushort[] data, int offset, int exponent) {
            fixed (ushort* pData = data) {
                Syndrome_AddCodewordSlice(_rsp, pData + offset, (uint)exponent);
            }
        }

        public void GetSyndromeSlice(byte[] data, int offset, int exponent) {
            fixed (byte* pData = data) {
                Syndrome_GetSyndromeSlice(_rsp, (ushort*)(pData + offset), (uint)exponent);
            }
        }

        public void GetSyndromeSlice(ushort[] data, int offset, int exponent) {
            fixed (ushort* pData = data) {
                Syndrome_GetSyndromeSlice(_rsp, pData + offset, (uint)exponent);
            }
        }

        internal IntPtr InternalPointer => _rsp;

        private bool isDisposed = false;
        private IntPtr _rsp;

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Syndrome_Construct(uint nDataCodewords, uint nParityCodewords, uint codewordsPerSlice);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Syndrome_Destruct(IntPtr syndrome);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Syndrome_AddCodewordSlice(IntPtr syndrome, ushort* data, uint exponent);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Syndrome_GetSyndromeSlice(IntPtr syndrome, ushort* data, uint exponent);
    }
}
