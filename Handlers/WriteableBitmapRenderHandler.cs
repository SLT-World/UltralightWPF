using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UltralightNet;

namespace UltralightWPF.Handlers
{
    public class WriteableBitmapRenderHandler : IRenderHandler
    {
        private WriteableBitmap? _Bitmap;
        public void AllocateBitmap(Image _Image, int _Width, int _Height)
        {
            if (_Bitmap == null || _Bitmap.PixelWidth != _Width || _Bitmap.PixelHeight != _Height)
            {
                _Bitmap = new WriteableBitmap(_Width, _Height, 96, 96, PixelFormats.Bgra32, null);
                _Image.Source = _Bitmap;
            }
        }

        public unsafe void UpdateBitmap(ULSurface Surface)
        {
            if (_Bitmap == null) return;
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
            {
                double DirtyArea = Width * Height;
                double TotalArea = TotalWidth * TotalHeight;
                CopyFullFrame = (DirtyArea / TotalArea) > 0.25;
            }

            byte* pSrcPixels = Surface.LockPixels();
            try
            {
                _Bitmap.Lock();
                byte* pDestBase = (byte*)_Bitmap.BackBuffer;
                if (CopyFullFrame)
                {
                    Buffer.MemoryCopy(pSrcPixels, pDestBase, (ulong)(_Bitmap.BackBufferStride * TotalHeight), Surface.Size);
                    _Bitmap.AddDirtyRect(new Int32Rect(0, 0, TotalWidth, TotalHeight));
                }
                else
                {
                    int X = DirtyRect.Left;
                    int Y = DirtyRect.Top;
                    int destStride = _Bitmap.BackBufferStride;
                    uint srcStride = Surface.RowBytes;
                    int bytesPerPixel = 4;
                    byte* pSrc = pSrcPixels + (Y * srcStride) + (X * bytesPerPixel);
                    byte* pDest = pDestBase + (Y * destStride) + (X * bytesPerPixel);
                    ulong lineLengthInBytes = (ulong)(Width * bytesPerPixel);
                    for (int i = 0; i < Height; i++)
                    {
                        Unsafe.CopyBlockUnaligned(pDest, pSrc, (uint)lineLengthInBytes);
                        pSrc += srcStride;
                        pDest += destStride;
                    }
                    _Bitmap.AddDirtyRect(new Int32Rect(X, Y, Width, Height));
                }
            }
            finally
            {
                _Bitmap.Unlock();
                Surface.UnlockPixels();
                Surface.ClearDirtyBounds();
            }
        }

        public void Dispose()
        {
            _Bitmap = null;
        }
    }
}
