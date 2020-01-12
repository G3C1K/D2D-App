using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace TCPSender
{
    public class CompressedScreen
    {
        public int Size;
        public byte[] Data;
        public CompressedScreen(int size)
        {
            this.Data = new byte[size];
            this.Size = 4;
        }
    }

    public class CompressScreen
    {
        private Rectangle screenBounds;
        private Bitmap prev;
        private Bitmap cur;
        private byte[] compressionBuffer;

        private CompressedScreen compressedScreen;

        private int n = 0;

        public CompressScreen()
        {
            this.screenBounds = Screen.PrimaryScreen.Bounds;

            prev = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            cur = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(prev))
            {
                g.Clear(Color.Black);
            }

            compressionBuffer = new byte[screenBounds.Width * screenBounds.Height * 4];

            int backBufSize = LZ4.LZ4Codec.MaximumOutputLength(compressionBuffer.Length) + 4;
            compressedScreen = new CompressedScreen(backBufSize);
        }

        private void Capture(Bitmap output)
        {

            Graphics.FromImage(cur).CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
            //using (var gfxScreenshot = Graphics.FromImage(output))
            //{
            //    gfxScreenshot.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
                
            //}
        }

        public void Iterate()
        {

            // Capture(cur);
            Graphics.FromImage(cur).CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);

            var locked1 = cur.LockBits(new Rectangle(0, 0, cur.Width, cur.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var locked2 = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                XOR.CountDifference(locked2, locked1, this.compressionBuffer);


                compressedScreen.Size = LZ4.LZ4Codec.Encode(compressionBuffer, 0, compressionBuffer.Length, compressedScreen.Data, 0, compressedScreen.Data.Length);


                var tmp = cur;
                cur = prev;
                prev = tmp;
            }
            finally
            {
                cur.UnlockBits(locked1);
                prev.UnlockBits(locked2);
            }
        }

        public void PerformanceTest()
        {
            Stopwatch sw = Stopwatch.StartNew();


            //Capture(cur);
            Graphics.FromImage(cur).CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);

            TimeSpan timetoCapture = sw.Elapsed;
            var locked1 = cur.LockBits(new Rectangle(0, 0, cur.Width, cur.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var locked2 = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                XOR.CountDifference(locked2, locked1, this.compressionBuffer);

                TimeSpan timeToXor = sw.Elapsed;

                compressedScreen.Size = LZ4.LZ4Codec.Encode(compressionBuffer, 0, compressionBuffer.Length, compressedScreen.Data, 0, compressedScreen.Data.Length);

                TimeSpan timeToCompress = sw.Elapsed;


                Console.WriteLine("Iteration: {0}ms, {1}ms, {2}ms, " + "{3} Kb => {4:0.0} FPS     \r", timetoCapture.TotalMilliseconds, timeToXor.TotalMilliseconds,
                        timeToCompress.TotalMilliseconds, compressedScreen.Size / 1024, 1.0 / sw.Elapsed.TotalSeconds);


                var tmp = cur;
                cur = prev;
                prev = tmp;
            }
            finally
            {
                cur.UnlockBits(locked1);
                prev.UnlockBits(locked2);
            }
        }

        public CompressedScreen getData()
        {
            return compressedScreen;
        }

    }

    public class DecompressScreen
    {
        public byte[] decompressed;
        public DecompressScreen(CompressedScreen compressed)
        {
            decompressed = new byte[LZ4.LZ4Codec.MaximumOutputLength(compressed.Data.Length)];
            LZ4.LZ4Codec.Decode(compressed.Data, 0, compressed.Size,
                decompressed, 0, decompressed.Length);
        }



        //Converting byte[] into bitmap
        public Bitmap DAiB(byte[] input)
        {
            Bitmap outbmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);

            //BitmapData outbmpData = outbmp.LockBits(new Rectangle(0, 0, outbmp.Width, outbmp.Height), ImageLockMode.WriteOnly, outbmp.PixelFormat);

            //Marshal.Copy(input, 0, outbmpData.Scan0, input.Length);

            Bitmap bmp;
            var screenBounds = Screen.PrimaryScreen.Bounds;
            bmp = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
            }
            var locked1 = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            XOR.ApplyDifference(locked1, input);
            bmp.UnlockBits(locked1);
            bmp.Save("image.png", ImageFormat.Png);

            //outbmp.UnlockBits(outbmpData);


            
            return bmp;
        }
    }

    public static class XOR
    {
        public static unsafe void CountDifference(BitmapData previous, BitmapData current, byte[] outputArray)
        {
            byte* prev0 = (byte*)previous.Scan0.ToPointer();
            byte* cur0 = (byte*)current.Scan0.ToPointer();

            int height = previous.Height;
            int width = previous.Width;
            int halfwidth = width / 2;
            //fixed (byte* target = this.compressionBuffer)

            fixed (byte* target = outputArray)
            {
                ulong* dst = (ulong*)target;

                for (int y = 0; y < height; ++y)
                {
                    ulong* prevRow = (ulong*)(prev0 + previous.Stride * y);
                    ulong* curRow = (ulong*)(cur0 + current.Stride * y);

                    for (int x = 0; x < halfwidth; ++x)
                    {
                        *(dst++) = curRow[x] ^ prevRow[x];
                    }
                }
            }
        }

        public static unsafe void ApplyDifference(BitmapData imageInput, byte[] _xorDiff)
        {
            byte* image0 = (byte*)imageInput.Scan0.ToPointer();
            //byte* cur0 = (byte*)current.Scan0.ToPointer();

            int height = imageInput.Height;
            int width = imageInput.Width;
            int halfwidth = width / 2;
            fixed (byte* xorDiff = _xorDiff)
            {
                ulong* dst = (ulong*)xorDiff;

                for (int y = 0; y < height; ++y)
                {
                    ulong* imageRow = (ulong*)(image0 + imageInput.Stride * y);
                    //ulong* curRow = (ulong*)(cur0 + current.Stride * y);

                    for (int x = 0; x < halfwidth; ++x)
                    {
                        //*(dst++) = target[x] ^ prevRow[x];
                        //Console.WriteLine(imageRow[x].ToString() + "   " + (*dst).ToString());
                        imageRow[x] = *(dst++) ^ imageRow[x];
                        //Console.WriteLine(imageRow[x].ToString() + "   " + (*dst).ToString());

                    }
                }
            }
        }
    }


}
