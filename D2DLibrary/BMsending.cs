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
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX;

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

        //directX
        Factory1 factory;
        Adapter1 adapter;
        SharpDX.Direct3D11.Device device;
        Output output;
        Output1 output1;
        Texture2DDescription textureDesc;
        Texture2D screenTexture;
        //directX screenCapture
        OutputDuplication duplicatedOutput;
        SharpDX.DXGI.Resource screenResource;
        OutputDuplicateFrameInformation duplicateFrameInformation;

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

            //directX Constructor
            factory = new Factory1();
            adapter = factory.GetAdapter1(0);
            device = new SharpDX.Direct3D11.Device(adapter);
            output = adapter.GetOutput(0);
            output1 = output.QueryInterface<Output1>();
            textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = screenBounds.Width,
                Height = screenBounds.Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            screenTexture = new Texture2D(device, textureDesc);
            duplicatedOutput = output1.DuplicateOutput(device);
        }

        private void Capture(Bitmap output)
        {

            Graphics.FromImage(output).CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
            //using (var gfxScreenshot = Graphics.FromImage(output))
            //{
            //    gfxScreenshot.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
                
            //}
        }

        private void CaptureDX(Bitmap outputBMP)
        {
            // Try to get duplicated frame within given time is ms
            try
            {
                duplicatedOutput.AcquireNextFrame(5, out duplicateFrameInformation, out screenResource);
                // copy resource into memory that can be accessed by the CPU
                using (Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>())
                {
                    device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                }
                // Get the desktop capture texture
                SharpDX.DataBox mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                BitmapData mapDest = outputBMP.LockBits(screenBounds, ImageLockMode.WriteOnly, outputBMP.PixelFormat);

                IntPtr sourcePtr = mapSource.DataPointer;
                IntPtr destPtr = mapDest.Scan0;

                for (int y = 0; y < screenBounds.Height; y++)
                {
                    // Copy a single line 
                    Utilities.CopyMemory(destPtr, sourcePtr, screenBounds.Width * 4);

                    // Advance pointers
                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                outputBMP.UnlockBits(mapDest);
                device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                duplicatedOutput.ReleaseFrame();
            }
            catch (SharpDXException e)
            {
                if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    Trace.TraceError(e.Message);
                    Trace.TraceError(e.StackTrace);
                }
            }

        }

        public void Iterate()
        {

            Capture(cur);

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

        public void IterateDX()
        {

            CaptureDX(cur);

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


            Capture(cur);

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

        public void PerformanceTestDX()
        {
            Stopwatch sw = Stopwatch.StartNew();


            CaptureDX(cur);

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
