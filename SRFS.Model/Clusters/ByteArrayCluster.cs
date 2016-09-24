namespace SRFS.Model.Clusters {

    public class ByteArrayCluster : ArrayCluster<byte> {

        // Public
        #region Constructors

        public ByteArrayCluster() : base(sizeof(byte)) { }

        #endregion

        // Protected
        #region Methods

        protected override void WriteElement(byte value, ByteBlock byteBlock, int offset) {
            byteBlock.Set(offset, value);
        }

        protected override byte ReadElement(ByteBlock byteBlock, int offset) {
            return byteBlock.ToByte(offset);
        }

        #endregion
    }
}
