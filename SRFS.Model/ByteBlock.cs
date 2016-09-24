﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace SRFS.Model {

    public class ByteBlock {

        public ByteBlock(byte[] block) {
            _block = block;
            _offset = 0;
            _length = block.Length;
            _isModifed = true;
        }

        public ByteBlock(byte[] block, int offset, int length) {
            _block = block;
            _offset = offset;
            _length = length;
            _isModifed = true;
        }

        public ByteBlock(ByteBlock block, int offset, int length) {
            if (offset < 0 || offset > block.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || length > block.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > block.Length) throw new ArgumentException();

            _block = block._block;
            _offset = block._offset + offset;
            _length = length;
            _isModifed = true;
        }

        public byte ToByte(int offset) => _block[_offset + offset];
        public void Set(int offset, byte b) {
            _block[_offset + offset] = b;
            _isModifed = true;
        }

        public byte[] ToByteArray(int offset, int length) {
            if (offset + length > _length) throw new ArgumentException();
            byte[] r = new byte[length];
            Buffer.BlockCopy(_block, _offset + offset, r, 0, length);
            return r;
        }

        public void Set(int offset, byte[] b) {
            if (offset + b.Length > _length) throw new ArgumentException();
            Buffer.BlockCopy(b, 0, _block, _offset + offset, b.Length);
            _isModifed = true;
        }


        public int ToInt32(int offset) => BitConverter.ToInt32(_block, offset + _offset);

        public void Set(int offset, int s) {
            if (offset < 0 || offset > _length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + sizeof(int) > _length) throw new ArgumentException();
            Buffer.BlockCopy(BitConverter.GetBytes(s), 0, _block, _offset + offset, sizeof(int));
            _isModifed = true;
        }

        public long ToInt64(int offset) => BitConverter.ToInt64(_block, offset + _offset);

        public void Set(int offset, long s) {
            if (offset < 0 || offset > _length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + sizeof(long) > _length) throw new ArgumentException();
            Buffer.BlockCopy(BitConverter.GetBytes(s), 0, _block, _offset + offset, sizeof(long));
            _isModifed = true;
        }

        public string ToString(int offset, int lengthChars) {
            return Encoding.Unicode.GetString(_block, _offset + offset, lengthChars * sizeof(char));
        }

        public void Set(int offset, string s) {
            if (s.Length == 0) return;
            char[] chars = s.ToCharArray();
            Encoding.Unicode.GetBytes(chars, 0, chars.Length, _block, _offset + offset);
            _isModifed = true;
        }

        public void Clear(int offset, int length) {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > _length) throw new ArgumentException();

            if (length == 0) return;

            Array.Clear(_block, _offset + offset, _length);
            _isModifed = true;
        }

        private byte[] _block;
        private int _offset;

        public int Length => _length;
        private int _length;

        public bool IsModified {
            get {
                return _isModifed;
            }
            set {
                _isModifed = value;
            }
        }

        private bool _isModifed;

        public byte[] TransformFinalBlock(ICryptoTransform t, int offset, int length) {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            // Of course, offset + length could overflow...
            if (offset + length > _length) throw new ArithmeticException();
            return t.TransformFinalBlock(_block, _offset + offset, length);
        }
    }

    public static class ByteBlockExtensions {

        public static byte[] TransformFinalBlock(this ICryptoTransform t, ByteBlock b, int offset, int length) => b.TransformFinalBlock(t, offset, length);
    }
}