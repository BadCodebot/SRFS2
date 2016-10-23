using System;
using System.Runtime.InteropServices;

namespace SRFS.ReedSolomon {

    public unsafe class ReedSolomonParity : IDisposable {

        private const int BUFFER_ALIGNMENT = 16;

        public ReedSolomonParity(uint nDataCodewords, uint nParityCodewords, uint codewordsPerSlice) {
            _rsp = Parity_Construct(nDataCodewords, nParityCodewords, codewordsPerSlice);
        }

        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                if (disposing) { }
                Parity_Destruct(_rsp);
                isDisposed = true;
            }
        }

        ~ReedSolomonParity() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Calculate(byte[] data, int offset, uint codewordIndex) {
            fixed (byte* pData = data) Parity_Calculate(_rsp, pData + offset, codewordIndex);
        }

        private byte* Parity => Parity_GetFirstParityBlock(_rsp);

        public uint NParityCodeWords => Parity_GetNParityCodewords(_rsp);

        public uint NDataCodeWords => Parity_GetNDataCodewords(_rsp);

        public uint NParityBlocks => Parity_GetNParityBlocks(_rsp);

        public uint CodewordsPerSlice => Parity_GetCodewordsPerSlice(_rsp);

        private bool isDisposed = false;
        private IntPtr _rsp;

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Parity_Construct(uint nDataCodewords, uint nParityCodewords, uint codewordsPerSlice);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Parity_Destruct(IntPtr rsc);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Parity_Calculate(IntPtr rsc, byte* data, uint codewordIndex);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Parity_Reset(IntPtr rsc);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint Parity_GetNParityCodewords(IntPtr rsc);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint Parity_GetNDataCodewords(IntPtr rsc);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint Parity_GetNParityBlocks(IntPtr rsc);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint Parity_GetCodewordsPerSlice(IntPtr rsc);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte* Parity_GetFirstParityBlock(IntPtr rsc);
    }
}
