namespace SRFS.Model.Clusters {

    public class IntArrayCluster : ArrayCluster<int> {

        // Public
        #region Constructors

        public IntArrayCluster() : base(sizeof(int)) { }

        #endregion

        // Protected
        #region Methods

        protected override void WriteElement(int value, ByteBlock byteBlock, int offset) {
            byteBlock.Set(offset, value);
        }

        protected override int ReadElement(ByteBlock byteBlock, int offset) {
            return byteBlock.ToInt32(offset);
        }

        #endregion
    }
}
