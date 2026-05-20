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
    public partial class UltralightWebView : UserControl, IUltralightWebView
    {
        private Renderer? _Renderer;
        private View? _View;
        private bool _Disposed;
        public IRenderHandler RenderHandler;

        public string Title { get; private set; } = "";
        public bool CanGoBack { get; private set; } = false;
        public bool CanGoForward { get; private set; } = false;
        public bool CanReload => IsBrowserInitialized;
        public bool IsLoading { get; private set; } = false;
        public bool IsBrowserInitialized => _View != null;
        private string CurrentUrl = "about:blank";
        public string Url
        {
            get => CurrentUrl;
            set => Navigate(value);
        }

        public void Navigate(string Url)
        {
            UltralightManager.Instance?.Invoke(() =>
            {
                if (_View != null)
                    _View.URL = Url;
            });
        }

        public void NavigateToString(string Text)
        {
            UltralightManager.Instance?.Invoke(() =>
            {
                if (_View != null)
                    _View.HTML = Text;
            });
        }
        public void GoBack() => UltralightManager.Instance?.Invoke(() => _View?.GoBack());
        public void GoForward() => UltralightManager.Instance?.Invoke(() => _View?.GoForward());
        public void Reload() => UltralightManager.Instance?.Invoke(() => _View?.Reload());
        public void Stop() => UltralightManager.Instance?.Invoke(() => _View?.Stop());
        public View? GetView() => _View;

        private double ActualZoomLevel = 1;
        public double ZoomLevel
        {
            //get => _View?.DeviceScale ?? 1;
            get => ActualZoomLevel;
            set
            {
                if (_View != null)
                {
                    if (value < 0.25 || value > 5.25)
                        return;
                    ActualZoomLevel = value;
                    UltralightManager.Instance?.Invoke(() => _View.DeviceScale = value);
                }
            }
        }
        public double ZoomFactor { get; set; } = 1.1;
        public void ZoomIn() =>
            ZoomLevel *= ZoomFactor;
        public void ZoomOut() =>
            ZoomLevel /= ZoomFactor;
        public void ZoomReset() =>
            ZoomLevel = 1;

        public UltralightWebView()
        {
            InitializeComponent();
            //RenderHandler = new InteropBitmapRenderHandler();
            RenderHandler = new WriteableBitmapRenderHandler();
            //RenderHandler = new D3DImageRenderHandler();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            int _Width = (int)Math.Max(1, sizeInfo.NewSize.Width);
            int _Height = (int)Math.Max(1, sizeInfo.NewSize.Height);
            UltralightManager.Instance?.Invoke(() => _View?.Resize((uint)_Width, (uint)_Height));
            RenderHandler.AllocateBitmap(RenderImage, _Width, _Height);
        }

        public void CaptureSurfaceTexture()
        {
            if (_View == null) return;
            RenderHandler.CaptureBitmap(_View!);
        }

        public void UpdateSurfaceTexture()
        {
            if (_View == null) return;
            RenderHandler.UpdateBitmap(_View);
        }

        public void Initialize(ULViewConfig? Config = null, View? InitialView = null)
        {
            if (_Disposed)
                throw new ObjectDisposedException("Disposed");
            if (UltralightManager.Instance == null)
                new UltralightManager().Initialize();
            else if (UltralightManager.Instance.IsHwndHost)
                throw new InvalidOperationException("An HwndHost web renderer implementation has already been registered. The application must exclusively use the HwndHost implementation.");

            Config ??= UltralightManager.DefaultViewConfig;
            ActualZoomLevel = Config.Value.InitialDeviceScale;
            _Renderer = UltralightManager.Instance.GlobalRenderer!;
            UltralightManager.Instance.Invoke(() =>
            {
                _View = InitialView ?? _Renderer.CreateView((uint)Math.Max(1, ActualWidth), (uint)Math.Max(1, ActualHeight), Config);
                _View.OnChangeTitle += View_OnChangeTitle;
                _View.OnChangeURL += View_OnChangeURL;
                //_View.OnChangeTooltip += View_OnChangeTooltip;
                _View.OnChangeCursor += View_OnChangeCursor;
                _View.OnBeginLoading += View_OnBeginLoading;
                _View.OnFinishLoading += View_OnFinishLoading;
                _View.OnFailLoading += View_OnFailLoading;
                UltralightManager.Instance.RegisterView(this);
            });
            Focus();
        }

        private void View_OnFailLoading(ulong frameId, bool isMainFrame, string url, string description, string errorDomain, int errorCode)
        {
            CanGoBack = _View?.CanGoBack ?? false;
            CanGoForward = _View?.CanGoForward ?? false;
            IsLoading = false;
            LoadingStateChanged.RaiseUIAsync(this, new LoadingStateResult(url, frameId, isMainFrame, false));
        }

        private void View_OnFinishLoading(ulong frameId, bool isMainFrame, string url)
        {
            CanGoBack = _View?.CanGoBack ?? false;
            CanGoForward = _View?.CanGoForward ?? false;
            IsLoading = false;
            LoadingStateChanged.RaiseUIAsync(this, new LoadingStateResult(url, frameId, isMainFrame, false));
        }

        private void View_OnBeginLoading(ulong frameId, bool isMainFrame, string url)
        {
            CanGoBack = _View?.CanGoBack ?? false;
            CanGoForward = _View?.CanGoForward ?? false;
            IsLoading = true;
            LoadingStateChanged.RaiseUIAsync(this, new LoadingStateResult(url, frameId, isMainFrame, true));
        }

        private void View_OnChangeTitle(string e)
        {
            Title = e;
            TitleChanged.RaiseUIAsync(this, e);
        }

        private void View_OnChangeURL(string e)
        {
            CurrentUrl = e;
            UrlChanged.RaiseUIAsync(this, e);
        }


        ULCursor CurrentCursor = ULCursor.None;
        private void View_OnChangeCursor(ULCursor e)
        {
            if (CurrentCursor != e)
            {
                Application.Current.Dispatcher.Invoke(() => Cursor = e.ToCursor());
                CurrentCursor = e;
            }
        }

        public event EventHandler<LoadingStateResult> LoadingStateChanged;
        public event EventHandler<string> UrlChanged;
        public event EventHandler<string> TitleChanged;
        /*public event EventHandler<string> ToolTipChanged;

        private void View_OnChangeTooltip(string e)
        {
            ToolTipChanged.RaiseUIAsync(this, e);
        }*/

        //TODO: Investigate inoperability.
        /*public void ShowInspector(UltralightWebView InspectorWebView, ULViewConfig? Config = null)
        {
            View? InspectorView = _View?.CreateLocalInspectorView();
            Debug.WriteLine(InspectorView.URL);
            if (InspectorView != null)
                InspectorWebView.Initialize(Config, InspectorView);*/
        /*Window InspectorWindow = new()
        {
            Title = $"Ultralight Developer Tools - {InspectedUrl}",
            Width = 800,
            Height = 600
        };*/
        //}

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!e.Handled && _View != null)
            {
                bool IsShiftKeyDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                UltralightManager.Instance?.Invoke(() => _View?.FireScrollEvent(new ULScrollEvent() { DeltaX = IsShiftKeyDown ? e.Delta : 0, DeltaY = IsShiftKeyDown ? 0 : e.Delta, Type = ULScrollEventType.ByPixel }));
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
                UltralightManager.Instance?.Invoke(() => _View.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.RawKeyDown, Keyboard.Modifiers.ToULKeyEventModifiers(), KeyCode, 0, Text, Unmodified, e.Key >= Key.NumPad0 && e.Key <= Key.Divide, e.IsRepeat, e.Key == Key.System)));
                //_View.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.RawKeyDown, Keyboard.Modifiers.ToULKeyEventModifiers(), KeyCode, Helpers.GetNativeScanCode((uint)KeyCode), Text, Unmodified, e.Key >= Key.NumPad0 && e.Key <= Key.Divide, e.IsRepeat, e.Key == Key.System));
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
                UltralightManager.Instance?.Invoke(() => _View.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.KeyUp, Keyboard.Modifiers.ToULKeyEventModifiers(), KeyCode, 0, Text, Unmodified, e.Key >= Key.NumPad0 && e.Key <= Key.Divide, e.IsRepeat, e.Key == Key.System)));
                //_View.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.KeyUp, Keyboard.Modifiers.ToULKeyEventModifiers(), KeyCode, Helpers.GetNativeScanCode((uint)KeyCode), Text, Unmodified, e.Key >= Key.NumPad0 && e.Key <= Key.Divide, e.IsRepeat, e.Key == Key.System));
                if (e.Key == Key.Tab || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control))
                    e.Handled = true;
            }
            base.OnPreviewKeyUp(e);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!e.Handled && _View != null)
            {
                OnTextInput(e.Text);
                e.Handled = true;
            }
            base.OnPreviewTextInput(e);
        }

        private void OnTextInput(string Text)
        {
            for (int i = 0; i < Text.Length; i++)
            {
                //char Character = Text[i];
                string CharString = Text[i].ToString();
                //int KeyCode = Helpers.CharToKeyCode(Character);
                UltralightManager.Instance?.Invoke(() => _View?.FireKeyEvent(ULKeyEvent.Create(ULKeyEventType.Char, Keyboard.Modifiers.ToULKeyEventModifiers(), 0, 0, CharString, CharString, false, false, false)));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!e.Handled && _View != null && e.StylusDevice == null)
            {
                Point Coordinate = e.GetPosition(this);
                UltralightManager.Instance?.Invoke(() => _View.FireMouseEvent(new ULMouseEvent() { Button = e.GetMouseEvents(), Type = ULMouseEventType.MouseMoved, X = (int)(Coordinate.X / _View.DeviceScale), Y = (int)(Coordinate.Y / _View.DeviceScale) }));
            }
            base.OnMouseMove(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            UltralightManager.Instance?.Invoke(() => _View?.Focus());
            base.OnGotFocus(e);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            UltralightManager.Instance?.Invoke(() => _View?.Focus());
            base.OnGotKeyboardFocus(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            UltralightManager.Instance?.Invoke(() => _View?.Unfocus());
            base.OnLostKeyboardFocus(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.StylusDevice == null)
            {
                Focus();
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
                //NOTE: Back/forward navigation controls is omitted here to allow for custom user implementation.
                /*bool MouseUp = e.ButtonState == MouseButtonState.Released;
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
                else*/
                if (e.ChangedButton != MouseButton.XButton1 && e.ChangedButton != MouseButton.XButton2)
                {
                    Point Coordinate = e.GetPosition(this);
                    UltralightManager.Instance?.Invoke(() => _View.FireMouseEvent(new ULMouseEvent() { Button = e.GetMouseEvents(), Type = e.ButtonState == MouseButtonState.Released ? ULMouseEventType.MouseUp : ULMouseEventType.MouseDown, X = (int)(Coordinate.X / _View.DeviceScale), Y = (int)(Coordinate.Y / _View.DeviceScale) }));
                }
                e.Handled = true;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (!e.Handled && _View != null && e.StylusDevice == null)
            {
                Point Coordinate = e.GetPosition(this);
                UltralightManager.Instance?.Invoke(() => _View.FireMouseEvent(new ULMouseEvent() { Button = ULMouseEventButton.None, Type = ULMouseEventType.MouseMoved, X = (int)(Coordinate.X / _View.DeviceScale), Y = (int)(Coordinate.Y / _View.DeviceScale) }));
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