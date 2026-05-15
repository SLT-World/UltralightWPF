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
                ReleaseBitmap();
                _Bitmap = new(_Width, _Height, 96, 96, PixelFormats.Bgra32, null);
                _Image.Source = _Bitmap;
            }
        }

        public unsafe void UpdateBitmap(View View)
        {
            if (_Bitmap == null || View.Surface == null) return;
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
                    uint lineLengthInBytes = (uint)(Width * bytesPerPixel);
                    for (int i = 0; i < Height; i++)
                    {
                        Unsafe.CopyBlockUnaligned(pDest, pSrc, lineLengthInBytes);
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

        public void ReleaseBitmap()
        {
            _Bitmap = null;
        }

        public void Dispose()
        {
            ReleaseBitmap();
        }
    }
}
