using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace SRFS.ReedSolomon {

    public unsafe class Repair : IDisposable {

        public Repair(Syndrome syndrome, int nCodewords, IEnumerable<int> errorExponents) {
            int[] e = errorExponents.ToArray();
            fixed (int* pE = e) {
                _rsp = Repair_Construct(syndrome.InternalPointer, nCodewords, pE, e.Length);
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                if (disposing) { }
                Repair_Destruct(_rsp);
                isDisposed = true;
            }
        }

        public void Correction(int errorExponentIndex, ushort[] data, int offset) {
            fixed (ushort* pData = data) Repair_Correction(_rsp, errorExponentIndex, pData + offset);
        }

        public void Correction(int errorExponentIndex, byte[] data, int offset) {
            fixed (byte* pData = data) Repair_Correction(_rsp, errorExponentIndex, (ushort*)(pData + offset));
        }


        ~Repair() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed = false;
        private IntPtr _rsp;

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Repair_Construct(IntPtr syndrome, int nCodewords, int* errorLocations, int errorCount);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Repair_Destruct(IntPtr repair);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Repair_Correction(IntPtr repair, int errorExponentIndex, ushort* data);
    }
}
