namespace SRFS.Model.Clusters {

    public class LongArrayCluster : ArrayCluster<long> {

        // Public
        #region Constructors

        public LongArrayCluster() : base(sizeof(long)) { }

        #endregion

        // Protected
        #region Methods

        protected override void WriteElement(long value, ByteBlock byteBlock, int offset) {
            byteBlock.Set(offset, value);
        }

        protected override long ReadElement(ByteBlock byteBlock, int offset) {
            return byteBlock.ToInt64(offset);
        }

        #endregion
    }
}
