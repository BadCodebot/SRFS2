using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SRFS.ReedSolomon;
using System.Collections.Generic;
using System.Linq;

namespace SRFS.Tests.ReedSolomon {

    [TestClass]
    public class ReedSolomonParityTests {

        [TestMethod]
        public void ReedsolomonRepairTest() {
            int nData = 100;
            int nParity = 25;
            int nMessages = 1000;
            int nErrors = 20;

            Random r = new Random(1234);

            byte[][] data = new byte[nData][];
            byte[][] original = new byte[nData][];

            for (int i = 0; i < nData; i++) {
                data[i] = new byte[nMessages];
                original[i] = new byte[nMessages];
                r.NextBytes(data[i]);
                for (int j = 0; j < nMessages; j++) original[i][j] = data[i][j];
            }

            byte[][] parity = new byte[nParity][];
            for (int i = 0; i < nParity; i++) parity[i] = new byte[nMessages * sizeof(ushort)];

            using (Parity p = new Parity(nData, nParity, nMessages / 2)) {
                for (int i = 0; i < nData; i++) p.Calculate(data[i], 0, nData + nParity - 1 - i);
                for (int i = 0; i < nParity; i++) p.GetParity(parity[i], 0, nParity - 1 - i);
            }

            List<int> choices = Enumerable.Range(0, nData + nParity).ToList();
            List<int> errorExponents = new List<int>();
            for (int i = 0; i < nErrors; i++) {
                int l = r.Next(choices.Count);
                errorExponents.Add(choices[l]);
                choices.RemoveAt(l);
            }

            foreach (var errorExponent in errorExponents) {
                byte[] d = errorExponent < nParity ? parity[nParity - 1 - errorExponent] : data[nData + nParity - 1 - errorExponent];
                r.NextBytes(d);
            }

            using (Syndrome s = new Syndrome(nData, nParity, nMessages / 2)) {

                for (int i = 0; i < nData; i++) s.AddCodewordSlice(data[i], 0, nData + nParity - 1 - i);
                for (int i = 0; i < nParity; i++) s.AddCodewordSlice(parity[i], 0, nParity - 1 - i);

                using (Repair repair = new Repair(s, nData + nParity, errorExponents)) {
                    int index = 0;
                    foreach (var errorExponent in errorExponents) {
                        byte[] d = errorExponent < nParity ? parity[nParity - 1 - errorExponent] : data[nData + nParity - 1 - errorExponent];
                        repair.Correction(index, d, 0);
                        index++;
                    }
                }
            }

            for (int i = 0; i < nData; i++) {
                Assert.IsTrue(data[i].SequenceEqual(original[i]));
            }
        }
    }
}
