using System;
using System.IO;

namespace TTF2Woff
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] fontData2 = new TTF2Woff().ttf2woff(System.IO.File.ReadAllBytes("AndroidClock.ttf"));
            using (FileStream outStream = File.Create("outfont2.woff"))
            {
                outStream.Write(fontData2, 0, fontData2.Length);
            }
        }
    }
}
