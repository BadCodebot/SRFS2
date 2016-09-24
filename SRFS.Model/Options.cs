namespace SRFS.Model {

    public enum Options : byte {

        None = 0x00,
        DoNotVerifyClusterHashes = 0x01,
        DoNotVerifyClusterSignatures = 0x02
    }

    public static class OptionsExtensions {

        public static bool VerifyClusterHashes(this Options o) => (o & Options.DoNotVerifyClusterHashes) == 0;
        public static bool VerifyClusterSignatures(this Options o) => (o & Options.DoNotVerifyClusterSignatures) == 0;
    }
}
