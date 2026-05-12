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
                _View.FireMouseEvent(new ULMouseEvent() { Button = e.GetMouseEvents(), Type = ULMouseEventType.MouseMoved, X = (int)Coordinate.X, Y = (int)Coordinate.Y });
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
                    _View.FireMouseEvent(new ULMouseEvent() { Button = e.GetMouseEvents(), Type = MouseUp ? ULMouseEventType.MouseUp : ULMouseEventType.MouseDown, X = (int)Coordinate.X, Y = (int)Coordinate.Y });
                }
                e.Handled = true;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (!e.Handled && _View != null && e.StylusDevice == null)
            {
                Point Coordinate = e.GetPosition(this);
                _View.FireMouseEvent(new ULMouseEvent() { Button = ULMouseEventButton.None, Type = ULMouseEventType.MouseMoved, X = (int)Coordinate.X, Y = (int)Coordinate.Y });
            }
            base.OnMouseLeave(e);
        }
    }
}
