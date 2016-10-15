using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SRFS.Model {

    [Flags]
    public enum ClusterState : byte {
        Unused = 0x00,
        Used = 0x01,
        Parity = 0x02,
        Modified = 0x04,
        System = 0x08,
        Null = 0x16
    }

    public static class ClusterStateExtensions {

        public static bool IsUsed(this ClusterState s) => (s & ClusterState.Used) != 0;
        public static bool IsModified(this ClusterState s) => (s & ClusterState.Modified) != 0;
        public static bool IsParity(this ClusterState s) => (s & ClusterState.Parity) != 0;
        public static bool IsSystem(this ClusterState s) => (s & ClusterState.System) != 0;

        public static void Save(this ClusterState type, byte[] data, int offset) {
            data[offset] = (byte)type;
        }

        public static void Write(this BinaryWriter writer, ClusterState s) {
            writer.Write((byte)s);
        }

        public static ClusterState ReadClusterState(this BinaryReader reader) {
            return (ClusterState)reader.ReadByte();
        }
    }
}
