
using System;
using System.Collections.Generic;
using System.IO;
using zlib;

namespace TTF2Woff
{

    public class WOFF_OFFSET
    {
        public const int MAGIC = 0;
        public const int FLAVOR = 4;
        public const int SIZE = 8;
        public const int NUM_TABLES = 12;
        public const int RESERVED = 14;
        public const int SFNT_SIZE = 16;
        public const int VERSION_MAJ = 20;
        public const int VERSION_MIN = 22;
        public const int META_OFFSET = 24;
        public const int META_LENGTH = 28;
        public const int META_ORIG_LENGTH = 32;
        public const int PRIV_OFFSET = 36;
        public const int PRIV_LENGTH = 40;
    }

    public class WOFF_ENTRY_OFFSET
    {
        public const int TAG = 0;
        public const int OFFSET = 4;
        public const int COMPR_LENGTH = 8;
        public const int LENGTH = 12;
        public const int CHECKSUM = 16;
    };

    public class SFNT_OFFSET
    {
        public const int TAG = 0;
        public const int CHECKSUM = 4;
        public const int OFFSET = 8;
        public const int LENGTH = 12;
    };

    public class SFNT_ENTRY_OFFSET
    {
        public const int FLAVOR = 0;
        public const int VERSION_MAJ = 4;
        public const int VERSION_MIN = 6;
        public const int CHECKSUM_ADJUSTMENT = 8;
    };

    public class MAGIC
    {
        public const int WOFF = 0x774F4646;
        public const uint CHECKSUM_ADJUSTMENT = 0xB1B0AFBA;
    };

    public class SIZEOF
    {
        public const int WOFF_HEADER = 44;
        public const int WOFF_ENTRY = 20;
        public const int SFNT_HEADER = 12;
        public const int SFNT_TABLE_ENTRY = 16;
    };
    public class version
    {
        public static short maj = 0;
        public static short min = 1;
    }
    public class TTF2Woff
    {


        public long @ulong(long t)
        {

            t &= 0xffffffff;
            if (t < 0)
            {
                t += 0x100000000;
            }
            return t;
        }

        public int longAlign(int n)
        {

            return (n + 3) & ~3;
        }

        public int calc_checksum(ByteBuffer buf)
        {
            int sum = 0;
            var nlongs = buf.Length / 4;

            for (var i = 0; i < nlongs; ++i)
            {
                var t = buf.GetUint32(i * 4);

                sum = (sum + t);
            }
            return sum;
        }



        private byte[] deflate(byte[] sourceStream)
        {
            MemoryStream streamOut = new MemoryStream();
            ZOutputStream streamZOut = new ZOutputStream(streamOut, zlibConst.Z_DEFAULT_COMPRESSION);
            streamOut.Write(sourceStream, 0, sourceStream.Length);
            streamZOut.finish();
            return streamOut.ToArray();
        }

        public byte[] ttf2woff(byte[] arr)
        {
            var buf = new ByteBuffer(arr);
            var numTables = buf.GetUint16(4);
            //var sfntVersion = buf.getUint32 (0);
            var flavor = 0x10000;
            var entries = new List<TableEntry>();//[];
            TableEntry tableEntry;
            for (var i = 0; i < numTables; ++i)
            {
                var data = new ByteBuffer(buf.Buffer, SIZEOF.SFNT_HEADER + i * SIZEOF.SFNT_TABLE_ENTRY);
                tableEntry = new TableEntry()
                {
                    Tag = new ByteBuffer(data, 0, 4),
                    checkSum = data.GetUint32(SFNT_OFFSET.CHECKSUM),
                    Offset = data.GetUint32(SFNT_OFFSET.OFFSET),
                    Length = data.GetUint32(SFNT_OFFSET.LENGTH)
                };
                entries.Add(tableEntry);
            }
            //js 的比对大小有点区别
            //entries.Sort((a, b) =>
            //{
            //    var aStr = a.Tag.toString();
            //    var bStr = b.Tag.toString();

            //    return aStr.CompareTo(bStr);// ? 0 : aStr < bStr ? -1 : 1;
            //});

            var offset = SIZEOF.WOFF_HEADER + numTables * SIZEOF.WOFF_ENTRY;
            var woffSize = offset;
            var sfntSize = SIZEOF.SFNT_HEADER + numTables * SIZEOF.SFNT_TABLE_ENTRY;
            var tableBuf = new ByteBuffer(numTables * SIZEOF.WOFF_ENTRY);

            for (var i = 0; i < numTables; ++i)
            {
                tableEntry = entries[i];

                if (tableEntry.Tag.ToString() != "head")
                {
                    var algntable = new ByteBuffer(buf.Buffer, tableEntry.Offset, longAlign(tableEntry.Length));

                    if (calc_checksum(algntable) != tableEntry.checkSum)
                    {
                        throw new Exception("Checksum error in " + tableEntry.Tag.ToString());
                    }
                }
                tableBuf.SetUint32(i * SIZEOF.WOFF_ENTRY + WOFF_ENTRY_OFFSET.TAG, tableEntry.Tag.GetUint32(0));
                tableBuf.SetUint32(i * SIZEOF.WOFF_ENTRY + WOFF_ENTRY_OFFSET.LENGTH, tableEntry.Length);
                tableBuf.SetUint32(i * SIZEOF.WOFF_ENTRY + WOFF_ENTRY_OFFSET.CHECKSUM, tableEntry.checkSum);
                sfntSize += longAlign(tableEntry.Length);
            }

            var sfntOffset = SIZEOF.SFNT_HEADER + entries.Count * SIZEOF.SFNT_TABLE_ENTRY;
            var csum = calc_checksum(new ByteBuffer(buf.Buffer, 0, SIZEOF.SFNT_HEADER));

            for (var i = 0; i < entries.Count; ++i)
            {
                tableEntry = entries[i];

                var b = new ByteBuffer(SIZEOF.SFNT_TABLE_ENTRY);

                b.SetUint32(SFNT_OFFSET.TAG, tableEntry.Tag.GetUint32(0));
                b.SetUint32(SFNT_OFFSET.CHECKSUM, tableEntry.checkSum);
                b.SetUint32(SFNT_OFFSET.OFFSET, sfntOffset);
                b.SetUint32(SFNT_OFFSET.LENGTH, tableEntry.Length);
                sfntOffset += longAlign(tableEntry.Length);
                csum += calc_checksum(b);
                csum += tableEntry.checkSum;
            }

            var checksumAdjustment = @ulong(MAGIC.CHECKSUM_ADJUSTMENT - csum);


            var woffDataChains = new List<ByteBuffer>();// [];

            for (var i = 0; i < entries.Count; ++i)
            {
                tableEntry = entries[i];

                var sfntData = new ByteBuffer(buf.Buffer, tableEntry.Offset, tableEntry.Length);

                if (tableEntry.Tag.ToString() == "head")
                {
                    version.maj = sfntData.GetUint16(SFNT_ENTRY_OFFSET.VERSION_MAJ);
                    version.min = sfntData.GetUint16(SFNT_ENTRY_OFFSET.VERSION_MIN);
                    flavor = sfntData.GetUint32(SFNT_ENTRY_OFFSET.FLAVOR);
                    sfntData.SetUint32(SFNT_ENTRY_OFFSET.CHECKSUM_ADJUSTMENT, checksumAdjustment);
                }

                var res = deflate(sfntData.ToArray());

                 

                // We should use compression only if it really save space (standard requirement).
                // Also, data should be aligned to long (with zeros?).
                int compLength = Math.Min(res.Length, sfntData.Length);

                int len = longAlign(compLength);

                var woffData = new ByteBuffer(len);

                woffData.Fill(0);

                if (res.Length >= sfntData.Length)
                {
                    woffData.WriteBytes(sfntData.ToArray());
                }
                else
                {
                    woffData.WriteBytes(res);
                }

                tableBuf.SetUint32(i * SIZEOF.WOFF_ENTRY + WOFF_ENTRY_OFFSET.OFFSET, offset);

                offset += woffData.Length;
                woffSize += woffData.Length;

                tableBuf.SetUint32(i * SIZEOF.WOFF_ENTRY + WOFF_ENTRY_OFFSET.COMPR_LENGTH, compLength);

                woffDataChains.Add(woffData);
            }


            var woffHeader = new ByteBuffer(SIZEOF.WOFF_HEADER);
            woffHeader.SetUint32(WOFF_OFFSET.MAGIC, MAGIC.WOFF);
            woffHeader.SetUint16(WOFF_OFFSET.NUM_TABLES, numTables);
            woffHeader.SetUint16(WOFF_OFFSET.RESERVED, 0);
            woffHeader.SetUint32(WOFF_OFFSET.SFNT_SIZE, 0);
            woffHeader.SetUint32(WOFF_OFFSET.META_OFFSET, 0);
            woffHeader.SetUint32(WOFF_OFFSET.META_LENGTH, 0);
            woffHeader.SetUint32(WOFF_OFFSET.META_ORIG_LENGTH, 0);
            woffHeader.SetUint32(WOFF_OFFSET.PRIV_OFFSET, 0);
            woffHeader.SetUint32(WOFF_OFFSET.PRIV_LENGTH, 0);
            woffHeader.SetUint32(WOFF_OFFSET.SIZE, woffSize);
            woffHeader.SetUint32(WOFF_OFFSET.SFNT_SIZE, sfntSize);
            woffHeader.SetUint16(WOFF_OFFSET.VERSION_MAJ, version.maj);
            woffHeader.SetUint16(WOFF_OFFSET.VERSION_MIN, version.min);
            woffHeader.SetUint32(WOFF_OFFSET.FLAVOR, flavor);

            var outStream = new MemoryStream(woffSize);
            outStream.Write(woffHeader.Buffer);
            outStream.Write(tableBuf.Buffer);
            for (var i = 0; i < woffDataChains.Count; i++)
            {
                outStream.Write(woffDataChains[i].Buffer);
            }
            return outStream.ToArray();
        }

    }
}
