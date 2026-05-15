using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using UltralightNet;

namespace UltralightWPF.Handlers
{
    public class InteropBitmapRenderHandler : IRenderHandler
    {
        private InteropBitmap? _Bitmap;
        private IntPtr _SectionHandle = IntPtr.Zero;
        private IntPtr _MapPointer = IntPtr.Zero;
        private uint _BufferSize = 0;

        public void AllocateBitmap(Image _Image, int _Width, int _Height)
        {
            if (_Bitmap == null || _Bitmap.PixelWidth != _Width || _Bitmap.PixelHeight != _Height)
            {
                ReleaseBitmap();
                int Stride = _Width * 4;
                _BufferSize = (uint)(Stride * _Height);

                _SectionHandle = DllUtils.CreateFileMapping(DllUtils.INVALID_HANDLE_VALUE, IntPtr.Zero, DllUtils.PAGE_READWRITE, 0, _BufferSize, null);
                if (_SectionHandle == IntPtr.Zero) return;
                _MapPointer = DllUtils.MapViewOfFile(_SectionHandle, DllUtils.FILE_MAP_ALL_ACCESS, 0, 0, _BufferSize);
                if (_MapPointer == IntPtr.Zero) return;

                _Bitmap = (InteropBitmap)Imaging.CreateBitmapSourceFromMemorySection(_SectionHandle, _Width, _Height, PixelFormats.Bgra32, Stride, 0);
                _Image.Source = _Bitmap;
            }
        }

        public unsafe void UpdateBitmap(View View)
        {
            if (_Bitmap == null || View.Surface == null || _MapPointer == IntPtr.Zero) return;
            ULSurface Surface = View.Surface.Value;
            ULIntRect DirtyRect = Surface.DirtyBounds;
            if (DirtyRect.IsEmpty) return;

            int TotalWidth = (int)Surface.Width;
            int TotalHeight = (int)Surface.Height;

            if (_Bitmap.PixelWidth != TotalWidth || _Bitmap.PixelHeight != TotalHeight) return;

            int Width = DirtyRect.Right - DirtyRect.Left;
            int Height = DirtyRect.Bottom - DirtyRect.Top;

            if (Width <= 0 || Height <= 0) return;
            bool CopyFullFrame = TotalWidth == Width && TotalHeight == Height;
            if (!CopyFullFrame)
                CopyFullFrame = ((double)(Width * Height) / (TotalWidth * TotalHeight)) > 0.25;

            byte* pSrcPixels = Surface.LockPixels();
            try
            {
                //TODO: Tearing observed.
                byte* pDestBase = (byte*)_MapPointer;
                if (CopyFullFrame)
                {
                    Buffer.MemoryCopy(pSrcPixels, pDestBase, _BufferSize, (long)Surface.Size);
                    _Bitmap.Invalidate();
                }
                else
                {
                    int X = DirtyRect.Left;
                    int Y = DirtyRect.Top;
                    int destStride = TotalWidth * 4;
                    uint srcStride = Surface.RowBytes;
                    int bytesPerPixel = 4;
                    byte* pSrc = pSrcPixels + (Y * srcStride) + (X * bytesPerPixel);
                    byte* pDest = pDestBase + (Y * destStride) + (X * bytesPerPixel);
                    uint lineLengthInBytes = (uint)(Width * bytesPerPixel);
                    for (int i = 0; i < Height; i++)
                    {
                        Unsafe.CopyBlockUnaligned(pDest, pSrc, lineLengthInBytes);
                        pSrc += srcStride;
                        pDest += destStride;
                    }
                    _Bitmap.Invalidate(new Int32Rect(X, Y, Width, Height));
                }
            }
            finally
            {
                Surface.UnlockPixels();
                Surface.ClearDirtyBounds();
            }
        }

        public void ReleaseBitmap()
        {
            _Bitmap = null;
            if (_MapPointer != IntPtr.Zero)
            {
                DllUtils.UnmapViewOfFile(_MapPointer);
                _MapPointer = IntPtr.Zero;
            }
            if (_SectionHandle != IntPtr.Zero)
            {
                DllUtils.CloseHandle(_SectionHandle);
                _SectionHandle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseBitmap();
        }
    }
}
