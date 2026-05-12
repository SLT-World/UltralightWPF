using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UltralightNet;

namespace UltralightWPF
{
    /// <summary>
    /// Interaction logic for UltralightWebView.xaml
    /// </summary>
    public partial class UltralightWebView : UserControl
    {
        private Renderer? _Renderer;
        private View? _View;
        public string Title => _View?.Title ?? "";
        public bool CanGoBack => _View?.CanGoBack ?? false;
        public bool CanGoForward => _View?.CanGoForward ?? false;
        public bool CanReload => IsBrowserInitialized;
        public bool IsLoading => _View?.IsLoading ?? false;
        public bool IsBrowserInitialized => _View != null;
        public string Url
        {
            get => _View?.URL ?? "about:blank";
            set => Navigate(value);
        }

        public void Navigate(string Url)
        {
            if (_View != null)
                _View.URL = Url;
        }
        public void GoBack() => _View?.GoBack();
        public void GoForward() => _View?.GoForward();
        public void Reload() => _View?.Reload();
        public void Stop() => _View?.Stop();
        public View? GetView() => _View;

        public UltralightWebView()
        {
            InitializeComponent();
        }

        public void Initialize(Renderer _Renderer, ULViewConfig? Config = null)
        {
            Config ??= new ULViewConfig();

            this._Renderer = _Renderer;
            uint _Width = (ActualWidth > 0) ? (uint)ActualWidth : 1;
            uint _Height = (ActualHeight > 0) ? (uint)ActualHeight : 1;
            _View = _Renderer.CreateView(_Width, _Height, Config);
            _View?.Focus();
            MouseWheel += (sender, e) =>
            {
                _View?.FireScrollEvent(new ULScrollEvent() { DeltaY = e.Delta });
            };
            CompositionTarget.Rendering += (s, args) => {
                InvalidateVisual();
            };
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            _View?.Resize((uint)sizeInfo.NewSize.Width, (uint)sizeInfo.NewSize.Height);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_Renderer == null) return;

            _Renderer.Update();
            _Renderer.Render();

            if (_View == null || _View.Surface == null) return;

            ULBitmap Bitmap = _View.Surface.Value.Bitmap;
            IntPtr Pixels;
            unsafe
            {
                Pixels = (IntPtr)Bitmap.LockPixels();
            }
            try
            {
                RenderImage.Source = BitmapSource.Create((int)Bitmap.Width, (int)Bitmap.Height, 96, 96, PixelFormats.Bgra32, null, Pixels, (int)Bitmap.Size, (int)Bitmap.RowBytes);
            }
            finally
            {
                Bitmap.UnlockPixels();
            }
        }

        public void Destroy()
        {
            _Renderer = null;
        }
    }
}
