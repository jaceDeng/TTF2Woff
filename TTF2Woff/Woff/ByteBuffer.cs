using System;
using System.IO;
using System.Linq;
using System.Text;

namespace TTF2Woff
{
    public class ByteBuffer
    {

        private int offset;

        public ByteBuffer(int len)
        {
            Buffer = new byte[len];
        }

        public ByteBuffer(byte[] arr)
        {
            Buffer = arr;
        }

        public ByteBuffer(byte[] arr, int start)
        {
            Buffer = arr.Skip(start).ToArray();
        }

        public ByteBuffer(ByteBuffer data, int start, int len)
        {
            Buffer = data.Buffer.Skip(start).Take(len).ToArray();
        }

        public ByteBuffer(byte[] arr, int start, int len)
        {
            Buffer = arr.Skip(start).Take(len).ToArray();
        }

        public int Length { get { return Buffer.Length; } }
        public byte[] Buffer { get; private set; }

        internal int GetUint32(int offset)
        {
            return (Buffer[offset] << 24) | ((Buffer[offset + 1] & 0xFF) << 16) | ((Buffer[offset + 2] & 0xFF) << 8) | (Buffer[offset + 3] & 0xFF);
        }



        internal void WriteBytes(byte[] data)
        {
            var offset = this.offset + 0;

            for (var i = 0; i < data.Length; i++)
            {
                Buffer[i + offset] = data[i];
            }

            this.offset += data.Length;
        }

        internal byte[] ToArray()
        {
            return Buffer;
        }

        internal short GetUint16(int offset)
        {
            return (short)(((Buffer[offset] & 0xFF) << 8) | (Buffer[offset + 1] & 0xFF));
        }



        public override string ToString()
        {
            return Encoding.Default.GetString(this.Buffer);
        }

        internal void Fill(byte value)
        {
            for (int i = 0; i < Buffer.Length; i++)
            {
                Buffer[i] = value;
            }
        }

        internal void SetUint32(int pos, long n)
        {
            Buffer[pos] = ((byte)(n >> 24));
            Buffer[pos + 1] = ((byte)(n >> 16));
            Buffer[pos + 2] = ((byte)(n >> 8));
            Buffer[pos + 3] = ((byte)n);

        }

        internal void SetUint16(int pos, short n)
        {

            Buffer[pos] = ((byte)(n >> 8));
            Buffer[pos + 1] = ((byte)n);

        }
    }
}