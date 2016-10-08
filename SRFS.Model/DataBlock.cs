using System;
using System.Security.Cryptography;
using System.Text;
using System.Security.Principal;

namespace SRFS.Model {

    public delegate void ChangedEventHandler(object sender);

    public interface INotifyChanged {
        event ChangedEventHandler Changed;
    }

    public class DataBlock : INotifyChanged {

        public event ChangedEventHandler Changed;

        public DataBlock(byte[] block) {
            _block = block;
            _offset = 0;
            _length = block.Length;
        }

        public DataBlock(byte[] block, int offset, int length) {
            _block = block;
            _offset = offset;
            _length = length;
        }

        private void notifyChanged() {
            Changed?.Invoke(this);
            _parent?.notifyChanged();
        }

        public DataBlock(DataBlock block, int offset) {
            if (offset < 0 || offset > block.Length) throw new ArgumentOutOfRangeException(nameof(offset));

            _block = block._block;
            _offset = block._offset + offset;
            _length = block.Length - offset;
            _parent = block;
        }

        public DataBlock(DataBlock block, int offset, int length) {
            if (offset < 0 || offset > block.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || length > block.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > block.Length) throw new ArgumentException();

            _block = block._block;
            _offset = block._offset + offset;
            _length = length;
            _parent = block;
        }

        public byte ToByte(int offset) => _block[_offset + offset];

        public void Set(int offset, byte b) {
            _block[_offset + offset] = b;
            notifyChanged();
        }

        public SecurityIdentifier ToSecurityIdentifier(int offset) {
            if (offset + Constants.SecurityIdentifierLength > _length) throw new ArgumentException();
            return new SecurityIdentifier(_block, _offset + offset);
        }

        public void Set(int offset, SecurityIdentifier id) {
            if (offset + Constants.SecurityIdentifierLength > _length) throw new ArgumentException();
            id.GetBinaryForm(_block, _offset + offset);
            Array.Clear(_block, _offset + offset + id.BinaryLength, Constants.SecurityIdentifierLength - id.BinaryLength);
            notifyChanged();
        }

        public byte[] ToByteArray(int offset, int length) {
            if (offset + length > _length) throw new ArgumentException();
            byte[] r = new byte[length];
            Buffer.BlockCopy(_block, _offset + offset, r, 0, length);
            return r;
        }

        public void ToByteArray(int offset, byte[] destination, int destinationOffset, int count) {
            if (offset + count > _length) throw new ArgumentException();
            if (destinationOffset + count > destination.Length) throw new ArgumentException();

            Buffer.BlockCopy(_block, _offset + offset, destination, destinationOffset, count);
        }

        public void Set(int offset, byte[] b) {
            if (offset + b.Length > _length) throw new ArgumentException();
            Buffer.BlockCopy(b, 0, _block, _offset + offset, b.Length);
            notifyChanged();
        }

        public void Set(int offset, byte[] b, int sourceOffset, int count) {
            if (offset + count > _length) throw new ArgumentException();
            if (sourceOffset + count > b.Length) throw new ArgumentException();

            Buffer.BlockCopy(b, sourceOffset, _block, _offset + offset, count);
            notifyChanged();
        }

        public void Set(int offset, DataBlock source, int sourceOffset, int count) {
            if (offset + count > _length) throw new ArgumentException();
            if (sourceOffset + count > source._length) throw new ArgumentException();
            Buffer.BlockCopy(source._block, source._offset + sourceOffset, _block, _offset + offset, count);
            notifyChanged();
        }

        public int ToInt32(int offset) => BitConverter.ToInt32(_block, offset + _offset);

        public void Set(int offset, int s) {
            if (offset < 0 || offset > _length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + sizeof(int) > _length) throw new ArgumentException();
            Buffer.BlockCopy(BitConverter.GetBytes(s), 0, _block, _offset + offset, sizeof(int));
            notifyChanged();
        }

        public bool ToBoolean(int offset) => BitConverter.ToBoolean(_block, offset + _offset);

        public void Set(int offset, bool s) {
            if (offset < 0 || offset > _length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + sizeof(bool) > _length) throw new ArgumentException();
            Buffer.BlockCopy(BitConverter.GetBytes(s), 0, _block, _offset + offset, sizeof(bool));
            notifyChanged();
        }

        public long ToInt64(int offset) => BitConverter.ToInt64(_block, offset + _offset);

        public void Set(int offset, long s) {
            if (offset < 0 || offset > _length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + sizeof(long) > _length) throw new ArgumentException();
            Buffer.BlockCopy(BitConverter.GetBytes(s), 0, _block, _offset + offset, sizeof(long));
            notifyChanged();
        }

        public string ToString(int offset, int lengthChars) {
            return Encoding.Unicode.GetString(_block, _offset + offset, lengthChars * sizeof(char));
        }

        public void Set(int offset, string s) {
            if (s.Length == 0) return;
            char[] chars = s.ToCharArray();
            Encoding.Unicode.GetBytes(chars, 0, chars.Length, _block, _offset + offset);
            notifyChanged();
        }

        public void Clear(int offset, int length) {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > _length) throw new ArgumentException();

            if (length == 0) return;

            Array.Clear(_block, _offset + offset, length);
            notifyChanged();
        }

        private DataBlock _parent = null;
        private byte[] _block;
        private int _offset;

        public int Length => _length;
        private int _length;

        public byte[] TransformFinalBlock(ICryptoTransform t, int offset, int length) {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            // Of course, offset + length could overflow...
            if (offset + length > _length) throw new ArithmeticException();
            return t.TransformFinalBlock(_block, _offset + offset, length);
        }

        public byte[] TransformFinalBlock(HashAlgorithm t, int offset, int length) {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            // Of course, offset + length could overflow...
            if (offset + length > _length) throw new ArithmeticException();
            return t.TransformFinalBlock(_block, _offset + offset, length);
        }
    }

    public static class ByteBlockExtensions {

        public static byte[] TransformFinalBlock(this HashAlgorithm t, DataBlock b, int offset, int length) => b.TransformFinalBlock(t, offset, length);

        public static byte[] TransformFinalBlock(this ICryptoTransform t, DataBlock b, int offset, int length) => b.TransformFinalBlock(t, offset, length);
    }
}
