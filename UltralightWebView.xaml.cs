using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UltralightNet;
using UltralightWPF.Handlers;

namespace UltralightWPF
{
    public readonly struct LoadingStateResult(string _Url, ulong _FrameId, bool _IsMainFrame, bool _IsLoading)
    {
        public string Url { get; } = _Url;
        public ulong FrameId { get; } = _FrameId;
        public bool IsMainFrame { get; } = _IsMainFrame;
        public bool IsLoading { get; } = _IsLoading;
    }

    /// <summary>
    /// Interaction logic for UltralightWebView.xaml
    /// </summary>
    public partial class UltralightWebView : UserControl, IDisposable
    {
        private Renderer? _Renderer;
        private View? _View;
        private bool _Disposed;
        private IRenderHandler RenderHandler;

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
            RenderHandler = new WriteableBitmapRenderHandler();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            int _Width = (int)Math.Max(1, sizeInfo.NewSize.Width);
            int _Height = (int)Math.Max(1, sizeInfo.NewSize.Height);
            _View?.Resize((uint)_Width, (uint)_Height);
            RenderHandler.AllocateBitmap(RenderImage, _Width, _Height);
        }

        public void UpdateSurfaceTexture()
        {
            if (_View?.Surface == null) return;
            RenderHandler.UpdateBitmap(_View.Surface.Value);
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
            _View.OnBeginLoading += View_OnBeginLoading;
            _View.OnFinishLoading += View_OnFinishLoading;
            _View.OnFailLoading += View_OnFailLoading;
            UltralightManager.Instance.RegisterView(this);
        }

        private void View_OnFailLoading(ulong frameId, bool isMainFrame, string url, string description, string errorDomain, int errorCode)
        {
            LoadingStateChanged.RaiseUIAsync(this, new LoadingStateResult(url, frameId, isMainFrame, false));
        }

        private void View_OnFinishLoading(ulong frameId, bool isMainFrame, string url)
        {
            LoadingStateChanged.RaiseUIAsync(this, new LoadingStateResult(url, frameId, isMainFrame, false));
        }

        private void View_OnBeginLoading(ulong frameId, bool isMainFrame, string url)
        {
            LoadingStateChanged.RaiseUIAsync(this, new LoadingStateResult(url, frameId, isMainFrame, true));
        }

        private void View_OnChangeURL(string e)
        {
            UrlChanged.RaiseUIAsync(this, e);
        }

        private void View_OnChangeCursor(ULCursor e)
        {
            Cursor = e.ToCursor();
        }

        public event EventHandler<LoadingStateResult> LoadingStateChanged;
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
                _View?.FireScrollEvent(new ULScrollEvent() { DeltaX = IsShiftKeyDown ? e.Delta : 0, DeltaY = IsShiftKeyDown ? 0 : e.Delta, Type = ULScrollEventType.ByPixel });
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
                UltralightManager.Instance.UnregisterView(this);
                if (_View != null)
                {
                    _View.OnChangeTitle -= View_OnChangeTitle;
                    _View.OnChangeURL -= View_OnChangeURL;
                    _View.OnChangeCursor -= View_OnChangeCursor;
                    _View.Dispose();
                    _View = null;
                }
                RenderHandler.Dispose();
                _Renderer = null;
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
