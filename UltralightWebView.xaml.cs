using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UltralightNet;

namespace UltralightWPF
{
    /// <summary>
    /// Interaction logic for UltralightWebView.xaml
    /// </summary>
    public partial class UltralightWebView : UserControl, IDisposable
    {
        private Renderer? _Renderer;
        private View? _View;
        private bool _Disposed;
        private WriteableBitmap? _TargetBitmap;

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

        public void NavigateToString(string Text)
        {
            if (_View != null)
                _View.HTML = Text;
        }
        public void GoBack() => _View?.GoBack();
        public void GoForward() => _View?.GoForward();
        public void Reload() => _View?.Reload();
        public void Stop() => _View?.Stop();
        public View? GetView() => _View;
        
        public double ZoomLevel
        {
            get => _View?.DeviceScale ?? 1;
            set
            {
                if (_View != null)
                {
                    if (value < 0.25 || value > 5.25)
                        return;
                    _View.DeviceScale = value;
                }
            }
        }
        public double ZoomFactor = 1.1;
        public void ZoomIn() =>
            ZoomLevel *= ZoomFactor;
        public void ZoomOut() =>
            ZoomLevel /= ZoomFactor;
        public void ZoomReset() =>
            ZoomLevel = 1;

        public UltralightWebView()
        {
            InitializeComponent();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            uint _Width = (uint)Math.Max(1, sizeInfo.NewSize.Width);
            uint _Height = (uint)Math.Max(1, sizeInfo.NewSize.Height);
            _View?.Resize(_Width, _Height);

            _TargetBitmap = new WriteableBitmap((int)_Width, (int)_Height, 96, 96, PixelFormats.Bgra32, null);
            RenderImage.Source = _TargetBitmap;
        }

        public void UpdateSurfaceTexture()
        {
            if (_Renderer == null || _View == null || _TargetBitmap == null || _View.Surface == null) return;

            ULBitmap Bitmap = _View.Surface.Value.Bitmap;
            int Width = (int)Bitmap.Width;
            int Height = (int)Bitmap.Height;

            if (_TargetBitmap.PixelWidth != Width || _TargetBitmap.PixelHeight != Height) return;
            IntPtr Pixels;
            unsafe
            {
                Pixels = (IntPtr)Bitmap.LockPixels();
            }
            try
            {
                _TargetBitmap.Lock();
                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)Pixels,
                        (void*)_TargetBitmap.BackBuffer,
                        (ulong)(_TargetBitmap.BackBufferStride * Height),
                        Bitmap.Size
                    );
                }

                _TargetBitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
            }
            finally
            {
                _TargetBitmap.Unlock();
                Bitmap.UnlockPixels();
            }
        }

        public void Initialize(ULViewConfig? Config = null)
        {
            if (UltralightManager.Instance == null)
                new UltralightManager().Initialize();

            Config ??= UltralightManager.DefaultViewConfig;

            _Renderer = UltralightManager.Instance.GlobalRenderer;
            uint _Width = (uint)Math.Max(1, ActualWidth);
            uint _Height = (uint)Math.Max(1, ActualHeight);
            _View = _Renderer.CreateView(_Width, _Height, Config);
            _View.Focus();
            _View.OnChangeTitle += View_OnChangeTitle;
            _View.OnChangeURL += View_OnChangeURL;
            //_View.OnChangeTooltip += View_OnChangeTooltip;
            _View.OnChangeCursor += View_OnChangeCursor;
            UltralightManager.Instance.RegisterView(this);
        }

        private void View_OnChangeURL(string e)
        {
            UrlChanged.RaiseUIAsync(this, e);
        }

        private void View_OnChangeCursor(ULCursor e)
        {
            Cursor = e.ToCursor();
        }

        public event EventHandler<string> UrlChanged;
        public event EventHandler<string> TitleChanged;
        /*public event EventHandler<string> ToolTipChanged;

        private void View_OnChangeTooltip(string e)
        {
            ToolTipChanged.RaiseUIAsync(this, e);
        }*/

        private void View_OnChangeTitle(string e)
        {
            TitleChanged.RaiseUIAsync(this, e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!e.Handled && _View != null)
            {
                bool IsShiftKeyDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                _View?.FireScrollEvent(new ULScrollEvent() { DeltaX = IsShiftKeyDown ? e.Delta : 0, DeltaY = IsShiftKeyDown ? 0 : e.Delta });
                e.Handled = true;
            }
            base.OnMouseWheel(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!e.Handled && _View != null)
            {
                string Text = Helpers.KeyToString(e.Key, Keyboard.Modifiers);
                string Unmodified = Helpers.KeyToString(e.Key, ModifierKeys.None);
                int KeyCode = KeyInterop.VirtualKeyFromKey(e.Key);
                _View.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.RawKeyDown, Keyboard.Modifiers.ToULKeyEventModifiers(), KeyCode, Helpers.GetNativeScanCode((uint)KeyCode), Text, Unmodified, e.Key >= Key.NumPad0 && e.Key <= Key.Divide, e.IsRepeat, e.Key == Key.System));
                if (e.Key == Key.Tab || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control))
                    e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (!e.Handled && _View != null)
            {
                string Text = Helpers.KeyToString(e.Key, Keyboard.Modifiers);
                string Unmodified = Helpers.KeyToString(e.Key, ModifierKeys.None);
                int KeyCode = KeyInterop.VirtualKeyFromKey(e.Key);
                _View.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.KeyUp, Keyboard.Modifiers.ToULKeyEventModifiers(), KeyCode, Helpers.GetNativeScanCode((uint)KeyCode), Text, Unmodified, e.Key >= Key.NumPad0 && e.Key <= Key.Divide, e.IsRepeat, e.Key == Key.System));
                if (e.Key == Key.Tab || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control))
                    e.Handled = true;
            }
            base.OnPreviewKeyUp(e);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!e.Handled && _View != null)
            {
                for (int i = 0; i < e.Text.Length; i++)
                {
                    string Text = e.Text[i].ToString();
                    _View.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.Char, Keyboard.Modifiers.ToULKeyEventModifiers(), 0, 0, Text, Text, false, false, false));
                }
                e.Handled = true;
            }
            base.OnPreviewTextInput(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!e.Handled && _View != null && e.StylusDevice == null)
            {
                Point Coordinate = e.GetPosition(this);
                _View.FireMouseEvent(new ULMouseEvent() { Button = e.GetMouseEvents(), Type = ULMouseEventType.MouseMoved, X = (int)(Coordinate.X / _View.DeviceScale), Y = (int)(Coordinate.Y / _View.DeviceScale) });
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.StylusDevice == null)
            {
                Focus();
                _View?.Focus();
                OnMouseButton(e);
                if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
                    CaptureMouse();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.StylusDevice == null)
            {
                OnMouseButton(e);
                if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                    ReleaseMouseCapture();
            }
            base.OnMouseUp(e);
        }

        private void OnMouseButton(MouseButtonEventArgs e)
        {
            if (!e.Handled && _View != null)
            {
                bool MouseUp = e.ButtonState == MouseButtonState.Released;
                if (e.ChangedButton == MouseButton.XButton1)
                {
                    if (CanGoBack && MouseUp)
                        GoBack();
                }
                else if (e.ChangedButton == MouseButton.XButton2)
                {
                    if (CanGoForward && MouseUp)
                        GoForward();
                }
                else
                {
                    Point Coordinate = e.GetPosition(this);
                    _View.FireMouseEvent(new ULMouseEvent() { Button = e.GetMouseEvents(), Type = MouseUp ? ULMouseEventType.MouseUp : ULMouseEventType.MouseDown, X = (int)(Coordinate.X / _View.DeviceScale), Y = (int)(Coordinate.Y / _View.DeviceScale) });
                }
                e.Handled = true;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (!e.Handled && _View != null && e.StylusDevice == null)
            {
                Point Coordinate = e.GetPosition(this);
                _View.FireMouseEvent(new ULMouseEvent() { Button = ULMouseEventButton.None, Type = ULMouseEventType.MouseMoved, X = (int)(Coordinate.X / _View.DeviceScale), Y = (int)(Coordinate.Y / _View.DeviceScale) });
            }
            base.OnMouseLeave(e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (_View != null)
                {
                    _View.OnChangeTitle -= View_OnChangeTitle;
                    _View.OnChangeURL -= View_OnChangeURL;
                    _View.OnChangeCursor -= View_OnChangeCursor;
                    _View.Dispose();
                    _View = null;
                }
                _Renderer = null;
                UltralightManager.Instance.UnregisterView(this);

                _Disposed = true;
            }
        }

        ~UltralightWebView()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
