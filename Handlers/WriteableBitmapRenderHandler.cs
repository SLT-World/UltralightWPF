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
        public bool ClearDirty { get; set; }

        public void AllocateBitmap(Image _Image, int _Width, int _Height)
        {
            if (_Bitmap == null || _Bitmap.PixelWidth != _Width || _Bitmap.PixelHeight != _Height)
            {
                ReleaseBitmap();
                _Bitmap = new(_Width, _Height, 96, 96, PixelFormats.Bgra32, null);
                _Image.Source = _Bitmap;
            }
        }
        private byte[] PixelBuffer;
        private ULIntRect PendingDirty;
        private bool HasPendingDirty;
        public unsafe void CaptureBitmap(View View)
        {
            if (_Bitmap == null || View.Surface == null) return;

            ULSurface Surface = View.Surface.Value;
            ULIntRect DirtyRect = Surface.DirtyBounds;
            if (DirtyRect.IsEmpty) return;

            int TotalWidth = (int)Surface.Width;
            int TotalHeight = (int)Surface.Height;

            if (PixelBuffer == null || PixelBuffer.Length != TotalWidth * TotalHeight * 4)
                PixelBuffer = new byte[TotalWidth * TotalHeight * 4];

            byte* pSrcPixels = Surface.LockPixels();
            try
            {

                uint srcStride = Surface.RowBytes;

                fixed (byte* dstBase = PixelBuffer)
                {
                    for (int Y = DirtyRect.Top; Y < DirtyRect.Bottom; Y++)
                    {
                        byte* srcRow = pSrcPixels + (Y * srcStride) + DirtyRect.Left * 4;
                        byte* dstRow = dstBase + (Y * TotalWidth * 4) + DirtyRect.Left * 4;
                        Buffer.MemoryCopy(srcRow, dstRow, (DirtyRect.Right - DirtyRect.Left) * 4, (DirtyRect.Right - DirtyRect.Left) * 4);
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

        public unsafe void UpdateBitmap(View View)
        {
            if (_Bitmap == null || !HasPendingDirty || PendingDirty.IsEmpty || PixelBuffer == null)
                return;

            _Bitmap.Lock();

            try
            {
                ClearDirty = true;

                int X = PendingDirty.Left;
                int Y = PendingDirty.Top;
                int Width = PendingDirty.Right - X;
                int Height = PendingDirty.Bottom - Y;

                //Debug.WriteLine($"{Width} {Height}");

                fixed (byte* pSrcPixels = PixelBuffer)
                {
                    for (int i = 0; i < Height; i++)
                    {
                        byte* srcRow = pSrcPixels + ((Y + i) * _Bitmap.PixelWidth * 4) + X * 4;
                        byte* dstRow = (byte*)_Bitmap.BackBuffer + ((Y + i) * _Bitmap.BackBufferStride) + X * 4;
                        Buffer.MemoryCopy(srcRow, dstRow, Width * 4, Width * 4);
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
        }

        public void Dispose()
        {
            ReleaseBitmap();
        }
    }
}