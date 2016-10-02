using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SRFS.Model;
using System.Security.Principal;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SRFS.Tests.Model {

    [TestClass]
    public class ByteBlockTests {

        [TestMethod]
        public void ByteBlockSetAndReadTest() {
            byte[] bytes = new byte[bufferSize];
            ByteBlock block = new ByteBlock(bytes);

            Set1(block);
            Read1(block);
        }

        [TestMethod]
        public void ByteBlockHashTest() {
            byte[] bytes = new byte[bufferSize];
            Random r = new Random();
            r.NextBytes(bytes);

            int offset = bufferSize / 2;
            int length = bufferSize - offset;

            ByteBlock b1 = new ByteBlock(bytes);
            ByteBlock b2 = new ByteBlock(b1, offset);

            byte[] h0;
            using (var h = new SHA256Cng()) {
                h.TransformFinalBlock(bytes, offset, length);
                h0 = h.Hash;
            }

            byte[] h1;
            using (var h = new SHA256Cng()) {
                h.TransformFinalBlock(b1, offset, length);
                h1 = h.Hash;
            }

            byte[] h2;
            using (var h = new SHA256Cng()) {
                h.TransformFinalBlock(b2, 0, b2.Length);
                h2 = h.Hash;
            }

            Assert.IsTrue(h0.SequenceEqual(h1));
            Assert.IsTrue(h0.SequenceEqual(h2));
        }

        private void Set1(ByteBlock block) {
            int offset = 0;
            block.Set(offset, byteTestValue);
            offset += sizeof(byte);
            block.Set(offset, sidTestValue);
            offset += Constants.SecurityIdentifierLength;
            block.Set(offset, byteArrayTestValue);
            offset += byteArrayTestValue.Length;
            block.Set(offset, intTestValue);
            offset += sizeof(int);
            block.Set(offset, stringTestValue);
            offset += stringTestValue.Length * sizeof(char);
            block.Set(offset, longTestValue);
            offset += sizeof(long);
            block.Set(offset, boolTestValue);
            offset += sizeof(bool);
        }

        private void Set2(ByteBlock block) {
            ByteBlock subBlock = block;
            int size = block.Length;

            subBlock.Set(0, byteTestValue);
            subBlock = new ByteBlock(subBlock, sizeof(byte));
            size -= sizeof(byte);
            Assert.AreEqual(size, subBlock.Length);

            subBlock.Set(0, sidTestValue);
            subBlock = new ByteBlock(subBlock, Constants.SecurityIdentifierLength);
            size -= Constants.SecurityIdentifierLength;
            Assert.AreEqual(size, subBlock.Length);

            subBlock.Set(0, byteArrayTestValue);
            subBlock = new ByteBlock(subBlock, byteArrayTestValue.Length * sizeof(byte));
            size -= byteArrayTestValue.Length * sizeof(byte);
            Assert.AreEqual(size, subBlock.Length);

            subBlock.Set(0, intTestValue);
            subBlock = new ByteBlock(subBlock, sizeof(int));
            size -= sizeof(int);
            Assert.AreEqual(size, subBlock.Length);

            subBlock.Set(0, stringTestValue);
            subBlock = new ByteBlock(subBlock, stringTestValue.Length * sizeof(char));
            size -= stringTestValue.Length * sizeof(char);
            Assert.AreEqual(size, subBlock.Length);

            subBlock.Set(0, longTestValue);
            subBlock = new ByteBlock(subBlock, sizeof(long));
            size -= sizeof(long);
            Assert.AreEqual(size, subBlock.Length);

            subBlock.Set(0, boolTestValue);
            subBlock = new ByteBlock(subBlock, sizeof(bool));
            size -= sizeof(bool);
            Assert.AreEqual(size, subBlock.Length);
        }

        private void Read1(ByteBlock mainBlock) {
            int offset = 0;
            Assert.AreEqual(byteTestValue, mainBlock.ToByte(offset), "Bytes not equal");
            offset += sizeof(byte);
            Assert.AreEqual(sidTestValue, mainBlock.ToSecurityIdentifier(offset), "SecurityIdentifiers not equal");
            offset += Constants.SecurityIdentifierLength;
            Assert.IsTrue(byteArrayTestValue.SequenceEqual(mainBlock.ToByteArray(offset, byteArrayTestValue.Length)));
            offset += byteArrayTestValue.Length;
            Assert.AreEqual(intTestValue, mainBlock.ToInt32(offset));
            offset += sizeof(int);
            Assert.AreEqual(stringTestValue, mainBlock.ToString(offset, stringTestValue.Length));
            offset += stringTestValue.Length * sizeof(char);
            Assert.AreEqual(longTestValue, mainBlock.ToInt64(offset));
            offset += sizeof(long);
            Assert.AreEqual(boolTestValue, mainBlock.ToBoolean(offset));
            offset += sizeof(bool);
        }

        [TestMethod]
        public void ByteBlockContainedBlockTest() {
            byte[] bytes = new byte[bufferSize];
            ByteBlock mainBlock = new ByteBlock(bytes);

            Set2(mainBlock);
            Read1(mainBlock);
        }

        private static int bufferSize = 1024;
        private static readonly byte byteTestValue = 0x5a;
        private static readonly SecurityIdentifier sidTestValue = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        private static readonly byte[] byteArrayTestValue = new byte[] { 13, 87, 144, 189 };
        private static readonly int intTestValue = 45;
        private static readonly string stringTestValue = "a test\0 string";
        private static readonly long longTestValue = 12498775;
        private static readonly bool boolTestValue = true;
    }
}
