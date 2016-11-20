using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Principal;
using System.Security.Cryptography;

namespace SRFS.Model {
    public static class Extensions {

        public const int GuidLength = 16;

        public static Guid ReadGuid(this BinaryReader reader) {
            return new Guid(reader.ReadBytes(GuidLength));
        }

        public static void Write(this BinaryWriter writer, Guid guid) {
            writer.Write(guid.ToByteArray());
        }

        public static SecurityIdentifier ReadSecurityIdentifier(this BinaryReader reader) {
            return new SecurityIdentifier(reader.ReadBytes(Constants.SecurityIdentifierLength), 0);
        }

        public static void Write(this BinaryWriter writer, SecurityIdentifier id) {
            byte[] bytes = new byte[Constants.SecurityIdentifierLength];
            id.GetBinaryForm(bytes, 0);
            writer.Write(bytes);
        }

        public static FileAttributes ReadFileAttributes(this BinaryReader reader) {
            return (FileAttributes)reader.ReadInt32();
        }

        public static void Write(this BinaryWriter writer, FileAttributes attributes) {
            writer.Write((int)attributes);
        }

        public static DateTime ReadDateTime(this BinaryReader reader) => new DateTime(reader.ReadInt64());

        public static void Write(this BinaryWriter writer, DateTime dateTime) {
            writer.Write(dateTime.Ticks);
        }

        public static void Write(this BinaryWriter writer, ECDiffieHellmanPublicKey publicKey) {
            writer.Write(publicKey.ToByteArray());
        }

        public static string ReadSrfsString(this BinaryReader reader) {
            int lengthChars = reader.ReadByte();
            byte[] bytes = reader.ReadBytes(Constants.MaximumNameLength * sizeof(char));
            return Encoding.Unicode.GetString(bytes, 0, lengthChars * sizeof(char));
        }

        public static void WriteSrfsString(this BinaryWriter writer, string s) {
            if (s == null) throw new ArgumentNullException();
            if (s.Length > Constants.MaximumNameLength) throw new ArgumentException();

            writer.Write((byte)s.Length);
            char[] chars = s.ToCharArray();
            byte[] bytes = new byte[Constants.MaximumNameLength * sizeof(char)];
            Encoding.Unicode.GetBytes(chars, 0, chars.Length, bytes, 0);
            writer.Write(bytes);
        }


    }
}
