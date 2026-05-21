using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using UltralightNet;

namespace UltralightWPF.Handlers
{
    public class InteropBitmapRenderHandler : IRenderHandler
    {
        public Image? Target { get; set; }
        private InteropBitmap? _Bitmap;
        private IntPtr _SectionHandle = IntPtr.Zero;
        private IntPtr _MapPointer = IntPtr.Zero;
        public bool ClearDirty { get; set; }
        private ULIntRect PendingDirty;
        private bool HasPendingDirty;
        private int CachedWidth;
        private int CachedHeight;
        private int CachedStride;
        private uint CachedTotalBytes;

        private readonly object RenderLock = new();

        public void AllocateBitmap(int _Width, int _Height)
        {
            if (Target == null) return;
            if (_Bitmap == null || _Bitmap.PixelWidth != _Width || _Bitmap.PixelHeight != _Height)
            {
                lock (RenderLock)
                {
                    ReleaseBitmap();
                    CachedWidth = _Width;
                    CachedHeight = _Height;
                    CachedStride = _Width * 4;
                    CachedTotalBytes = (uint)(CachedStride * _Height);

                    _SectionHandle = DllUtils.CreateFileMapping(DllUtils.INVALID_HANDLE_VALUE, IntPtr.Zero, DllUtils.PAGE_READWRITE, 0, CachedTotalBytes, null);
                    if (_SectionHandle == IntPtr.Zero) return;
                    _MapPointer = DllUtils.MapViewOfFile(_SectionHandle, DllUtils.FILE_MAP_ALL_ACCESS, 0, 0, CachedTotalBytes);
                    if (_MapPointer == IntPtr.Zero) return;

                    _Bitmap = (InteropBitmap)Imaging.CreateBitmapSourceFromMemorySection(_SectionHandle, _Width, _Height, PixelFormats.Bgra32, CachedStride, 0);
                    Target.Source = _Bitmap;
                }
            }
        }

        public unsafe void CaptureBitmap(View View)
        {
            if (Target == null || View.Surface == null) return;

            ULSurface Surface = View.Surface.Value;
            int SurfaceWidth = (int)Surface.Width;
            int SurfaceHeight = (int)Surface.Height;

            if (_Bitmap == null || CachedWidth != SurfaceWidth || CachedHeight != SurfaceHeight)
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

                lock (RenderLock)
                {
                    if (_MapPointer == IntPtr.Zero) return;
                    byte* pDestBase = (byte*)_MapPointer;
                    if (Width == CachedWidth && Height == CachedHeight)
                        Buffer.MemoryCopy(pSrcPixels, pDestBase, CachedTotalBytes, CachedTotalBytes);
                    else
                    {
                        byte* pSrcStart = pSrcPixels + (Y * srcStride) + (X * 4);
                        byte* pDstStart = pDestBase + (Y * CachedStride) + (X * 4);

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

        public void UpdateBitmap()
        {
            if (Target == null || _Bitmap == null || !HasPendingDirty || _MapPointer == IntPtr.Zero || PendingDirty.IsEmpty)
                return;
            try
            {
                ClearDirty = true;
                int X = PendingDirty.Left;
                int Y = PendingDirty.Top;
                int Width = PendingDirty.Right - X;
                int Height = PendingDirty.Bottom - Y;
                if (X < 0 || Y < 0 || (X + Width) > CachedWidth || (Y + Height) > CachedHeight || Width <= 0 || Height <= 0)
                    return;

                if (Width == CachedWidth && Height == CachedHeight)
                    _Bitmap.Invalidate();
                else
                    _Bitmap.Invalidate(new Int32Rect(X, Y, Width, Height));
            }
            finally
            {
                ClearDirty = true;
                HasPendingDirty = false;
                PendingDirty = default;
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