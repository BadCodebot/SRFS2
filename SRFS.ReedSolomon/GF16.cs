using System;
using System.Runtime.InteropServices;

namespace SRFS.ReedSolomon {

    public unsafe class GF16 {

        public static ushort Multiply(ushort x, ushort y) => GF16_Multiply(x, y);
        public static ushort Add(ushort x, ushort y) => GF16_Add(x, y);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort GF16_Multiply(ushort x, ushort y);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort GF16_Add(ushort x, ushort y);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort GF16_Inverse(ushort x);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort GF16_Power(ushort x, int a);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort GF16_Exp(int a);

        [DllImport("ReedSolomon.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GF16_Log(ushort x);
    }
}
