using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UltralightNet;

namespace UltralightWPF.Handlers
{
    public class WriteableBitmapRenderHandler : IRenderHandler
    {
        public Image? Target { get; set; }
        private WriteableBitmap? _Bitmap;
        public bool ClearDirty { get; set; }
        private byte[] PixelBuffer;
        private ULIntRect PendingDirty;
        private bool HasPendingDirty;

        private int CachedWidth;
        private int CachedHeight;
        private int CachedStride;
        private long CachedTotalBytes;

        public void AllocateBitmap(int _Width, int _Height)
        {
            if (Target == null) return;
            if (_Bitmap == null || _Bitmap.PixelWidth != _Width || _Bitmap.PixelHeight != _Height)
            {
                ReleaseBitmap();
                CachedWidth = _Width;
                CachedHeight = _Height;
                CachedStride = _Width * 4;
                CachedTotalBytes = CachedStride * _Height;
                PixelBuffer = new byte[CachedTotalBytes];

                _Bitmap = new(_Width, _Height, 96, 96, PixelFormats.Bgra32, null);
                Target.Source = _Bitmap;
            }
        }
        public unsafe void CaptureBitmap(View View)
        {
            if (Target == null || View.Surface == null) return;

            ULSurface Surface = View.Surface.Value;
            int SurfaceWidth = (int)Surface.Width;
            int SurfaceHeight = (int)Surface.Height;

            if (_Bitmap == null || CachedWidth != SurfaceWidth || CachedHeight != SurfaceHeight || PixelBuffer == null)
            {
                Target.Dispatcher.Invoke(() =>
                {
                    AllocateBitmap(SurfaceWidth, SurfaceHeight);
                });
            }

            ULIntRect DirtyRect = Surface.DirtyBounds;
            if (DirtyRect.IsEmpty) return;

            byte* pSrcPixels = Surface.LockPixels();
            try
            {
                uint srcStride = Surface.RowBytes;
                int X = DirtyRect.Left;
                int Y = DirtyRect.Top;
                int Width = DirtyRect.Right - X;
                int Height = DirtyRect.Bottom - Y;
                long LineLength = Width * 4;

                fixed (byte* dstBase = PixelBuffer)
                {
                    if (Width == CachedWidth && Height == CachedHeight)
                        Buffer.MemoryCopy(pSrcPixels, dstBase, CachedTotalBytes, CachedTotalBytes);
                    else
                    {
                        byte* pSrcStart = pSrcPixels + (Y * srcStride) + (X * 4);
                        byte* pDstStart = dstBase + (Y * CachedStride) + (X * 4);

                        for (int i = 0; i < Height; i++)
                        {
                            Buffer.MemoryCopy(pSrcStart, pDstStart, LineLength, LineLength);
                            pSrcStart += srcStride;
                            pDstStart += CachedStride;
                        }
                    }
                }

                PendingDirty = DirtyRect;
                ClearDirty = false;
                HasPendingDirty = true;
            }
            finally
            {
                Surface.UnlockPixels();
            }
        }

        public unsafe void UpdateBitmap()
        {
            if (Target == null || _Bitmap == null || !HasPendingDirty || PendingDirty.IsEmpty || PixelBuffer == null)
                return;

            _Bitmap.Lock();
            try
            {
                ClearDirty = true;
                int X = PendingDirty.Left;
                int Y = PendingDirty.Top;
                int Width = PendingDirty.Right - X;
                int Height = PendingDirty.Bottom - Y;
                if (X < 0 || Y < 0 || (X + Width) > CachedWidth || (Y + Height) > _Bitmap.PixelHeight || Width <= 0 || Height <= 0)
                    return;
                long LineLength = Width * 4;
                byte* pDestBase = (byte*)_Bitmap.BackBuffer;
                int destStride = _Bitmap.BackBufferStride;

                fixed (byte* pSrcPixels = PixelBuffer)
                {
                    if (Width == CachedWidth && Height == _Bitmap.PixelHeight)
                        Buffer.MemoryCopy(pSrcPixels, pDestBase, CachedTotalBytes, CachedTotalBytes);
                    else
                    {
                        byte* pSrcStart = pSrcPixels + (Y * CachedStride) + (X * 4);
                        byte* pDestStart = pDestBase + (Y * destStride) + (X * 4);
                        long TotalSrcBytes = PixelBuffer.Length;

                        for (int i = 0; i < Height; i++)
                        {
                            Buffer.MemoryCopy(pSrcStart, pDestStart, LineLength, LineLength);
                            pSrcStart += CachedStride;
                            pDestStart += destStride;
                        }
                    }
                }

                _Bitmap.AddDirtyRect(new Int32Rect(X, Y, Width, Height));
            }
            finally
            {
                _Bitmap.Unlock();
                ClearDirty = true;
                HasPendingDirty = false;
                PendingDirty = default;
            }
        }

        public void ReleaseBitmap()
        {
            _Bitmap = null;
            PixelBuffer = null;
        }

        public void Dispose()
        {
            ReleaseBitmap();
        }
    }
}