using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace D2DLibrary
{
    class GDI32
    {
        [DllImport("GDI32.dll")]
        public static extern bool BitBlt(int hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, int hdcSrc, int nXSrc, int nYSrc, int dwRop);
        [DllImport("GDI32.dll")]
        public static extern int CreateCompatibleBitmap(int hdc, int nWidth, int nHeight);
        [DllImport("GDI32.dll")]
        public static extern int CreateCompatibleDC(int hdc);
        [DllImport("GDI32.dll")]
        public static extern bool DeleteDC(int hdc);
        [DllImport("GDI32.dll")]
        public static extern bool DeleteObject(int hObject);
        [DllImport("GDI32.dll")]
        public static extern int GetDeviceCaps(int hdc, int nIndex);
        [DllImport("GDI32.dll")]
        public static extern int SelectObject(int hdc, int hgdiobj);
    }
        class User32
        {
            [DllImport("User32.dll")]
            public static extern int GetDesktopWindow();
            [DllImport("User32.dll")]
            public static extern int GetWindowDC(int hWnd);
            [DllImport("User32.dll")]
            public static extern int ReleaseDC(int hWnd, int hDC);
        }
        public class ScreenCapture
        {

            public static Bitmap CaptureScreen()
            {
                int hdcSrc = User32.GetWindowDC(User32.GetDesktopWindow()), // Get a handle to the desktop window
                hdcDest = GDI32.CreateCompatibleDC(hdcSrc), // Create a memory device context
                hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, // Create a bitmap and place it in the memory DC
                GDI32.GetDeviceCaps(hdcSrc, 8), GDI32.GetDeviceCaps(hdcSrc, 10));
                // GDI32.GetDeviceCaps(hdcSrc,8) returns the width of the desktop window
                // GDI32.GetDeviceCaps(hdcSrc,10) returns the height of the desktop window
                GDI32.SelectObject(hdcDest, hBitmap); // Required to create a color bitmap
                GDI32.BitBlt(hdcDest, 0, 0, GDI32.GetDeviceCaps(hdcSrc, 8), // Copy the on-screen image into the memory DC
                GDI32.GetDeviceCaps(hdcSrc, 10), hdcSrc, 0, 0, 0x00CC0020);
                Bitmap bitmapa = new Bitmap(Image.FromHbitmap(new IntPtr(hBitmap)),
                    Image.FromHbitmap(new IntPtr(hBitmap)).Width,
                    Image.FromHbitmap(new IntPtr(hBitmap)).Height);
                Cleanup(hBitmap, hdcSrc, hdcDest); // Free system resources
                return bitmapa;
            }

            private static void Cleanup(int hBitmap, int hdcSrc, int hdcDest)
            {
                // Release the device context resources back to the system
                User32.ReleaseDC(User32.GetDesktopWindow(), hdcSrc);
                GDI32.DeleteDC(hdcDest);
                GDI32.DeleteObject(hBitmap);
            }
        }











    public class ScreenStateLogger
    {
        private byte[] _previousScreen;
        private bool _run, _init;

        public int Size { get; private set; }
        public ScreenStateLogger()
        {

        }

        public void Start()
        {
            _run = true;
            Factory1 factory = new Factory1();
            //Get first adapter
            Adapter1 adapter = factory.GetAdapter1(0);
            //Get device from adapter
            SharpDX.Direct3D11.Device device = new SharpDX.Direct3D11.Device(adapter);
            //Get front buffer of the adapter
            Output output = adapter.GetOutput(0);
            Output1 output1 = output.QueryInterface<Output1>();

            // Width/Height of desktop to capture
            int width = output.Description.DesktopBounds.Right;
            int height = output.Description.DesktopBounds.Bottom;

            // Create Staging texture CPU-accessible
            Texture2DDescription textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            Texture2D screenTexture = new Texture2D(device, textureDesc);

            Task.Factory.StartNew(() =>
            {
                // Duplicate the output
                using (OutputDuplication duplicatedOutput = output1.DuplicateOutput(device))
                {
                    //try
                    //{

                    Thread.Sleep(2000);
                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            SharpDX.DXGI.Resource screenResource;
                            OutputDuplicateFrameInformation duplicateFrameInformation;

                            // Try to get duplicated frame within given time is ms
                            duplicatedOutput.AcquireNextFrame(15, out duplicateFrameInformation, out screenResource);

                            // copy resource into memory that can be accessed by the CPU
                            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            {
                                device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                            }

                            // Get the desktop capture texture
                            var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                            // Create Drawing.Bitmap
                            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                            {
                                var boundsRect = new Rectangle(0, 0, width, height);

                                // Copy pixels from screen capture Texture to GDI bitmap
                                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                                var sourcePtr = mapSource.DataPointer;
                                var destPtr = mapDest.Scan0;
                                for (int y = 0; y < height; y++)
                                {
                                    // Copy a single line 
                                    Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                                    // Advance pointers
                                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                                }

                                // Release source and dest locks
                                bitmap.UnlockBits(mapDest);
                                device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                                bitmap.Save("inputOG.png", ImageFormat.Png);
                                //using (var ms = new MemoryStream())
                                //{
                                //    bitmap.Save(ms, ImageFormat.Bmp);
                                //    ScreenRefreshed?.Invoke(this, ms.ToArray());
                                //    _init = true;
                                //}
                            }
                            screenResource.Dispose();
                            duplicatedOutput.ReleaseFrame();


                            Console.WriteLine(sw.Elapsed.TotalMilliseconds);

                        //}
                        //catch (SharpDXException e)
                        //{
                        //    if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                        //    {
                        //        Trace.TraceError(e.Message);
                        //        Trace.TraceError(e.StackTrace);
                        //    }
                        //}
                    
                }
            });
        }

        public void Stop()
        {
            _run = false;
        }

        public EventHandler<byte[]> ScreenRefreshed;
    }








}

