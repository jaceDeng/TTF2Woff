namespace TTF2Woff
{
    internal class TableEntry
    {
        internal int checkSum;

        public ByteBuffer Tag { get; internal set; }
        public int Offset { get; internal set; }
        public int Length { get; internal set; }
    }
}