using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using UltralightNet;
using UltralightNet.AppCore;

namespace UltralightWPF
{
    public class UltralightHwndWebView : HwndHost, IUltralightWebView
    {
        private IntPtr ContainerHwnd = IntPtr.Zero;
        private IntPtr ChildHwnd = IntPtr.Zero;
        private ULWindow? LocalWindow;
        private ULOverlay? LocalOverlay;
        private View? _View;
        private bool _Disposed;

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

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            ContainerHwnd = DllUtils.CreateWindowEx(0, "static", "", DllUtils.WS_CHILD | DllUtils.WS_VISIBLE | DllUtils.WS_CLIPCHILDREN, 0, 0, (int)ActualWidth, (int)ActualHeight, hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            CurrentWin32Cursor = DllUtils.LoadCursor(IntPtr.Zero, (IntPtr)DllUtils.IDC_ARROW);
            return new HandleRef(this, ContainerHwnd);
        }
        private IntPtr CurrentWin32Cursor = IntPtr.Zero;

        private DllUtils.WndProcDelegate ChildWndProcDelegate;
        private IntPtr OldChildWndProc = IntPtr.Zero;

        public unsafe void Initialize(ULViewConfig? Config = null, View? InitialView = null)
        {
            if (_Disposed)
                throw new ObjectDisposedException("Disposed");
            if (ContainerHwnd == IntPtr.Zero) return;
            int InitialWidth = (int)Math.Max(1, ActualWidth);
            int InitialHeight = (int)Math.Max(1, ActualHeight);
            if (UltralightManager.Instance == null)
                new UltralightManager().Initialize(_IsHwndHost: true);
            else if (!UltralightManager.Instance.IsHwndHost)
                throw new InvalidOperationException("A Bitmap web renderer implementation has already been registered. The application must exclusively use the Bitmap implementation.");
            //Config ??= UltralightManager.DefaultViewConfig;
            ActualZoomLevel = (Config ?? UltralightManager.DefaultViewConfig).InitialDeviceScale;
            UltralightManager.Instance.Invoke(() =>
            {
                LocalWindow = UltralightManager.Instance.GlobalApp.MainMonitor.CreateWindow((uint)InitialWidth, (uint)InitialHeight, false, ULWindowFlags.Borderless | ULWindowFlags.Resizable);
                ChildHwnd = (IntPtr)LocalWindow.NativeWindowHandle;

                int WindowStyle = DllUtils.GetWindowLong(ChildHwnd, DllUtils.GWL_STYLE);
                WindowStyle &= ~(DllUtils.WS_POPUP | DllUtils.WS_OVERLAPPED | DllUtils.WS_CAPTION | DllUtils.WS_THICKFRAME | DllUtils.WS_MINIMIZEBOX | DllUtils.WS_MAXIMIZEBOX | DllUtils.WS_SYSMENU);
                WindowStyle |= DllUtils.WS_VISIBLE;
                DllUtils.SetWindowLong(ChildHwnd, DllUtils.GWL_STYLE, WindowStyle);

                int WindowExStyle = DllUtils.GetWindowLong(ChildHwnd, DllUtils.GWL_EXSTYLE);
                WindowExStyle &= ~(DllUtils.WS_EX_DLGMODALFRAME | DllUtils.WS_EX_CLIENTEDGE | DllUtils.WS_EX_STATICEDGE);
                WindowExStyle &= ~DllUtils.WS_EX_APPWINDOW;
                DllUtils.SetWindowLong(ChildHwnd, DllUtils.GWL_EXSTYLE, WindowExStyle);

                DllUtils.SetParent(ChildHwnd, ContainerHwnd);
                DllUtils.SetWindowPos(ChildHwnd, IntPtr.Zero, 0, 0, InitialWidth, InitialHeight, DllUtils.SWP_NOZORDER | DllUtils.SWP_FRAMECHANGED);

                if (Config != null)
                {
                    _View = UltralightManager.Instance.GlobalRenderer.CreateView((uint)Math.Max(1, ActualWidth), (uint)Math.Max(1, ActualHeight), Config);
                    LocalOverlay = LocalWindow.CreateOverlay(_View);
                }
                else if (InitialView != null)
                {
                    _View = InitialView;
                    LocalOverlay = LocalWindow.CreateOverlay(InitialView);
                }
                else
                {
                    LocalOverlay = LocalWindow.CreateOverlay(LocalWindow.ScreenWidth, LocalWindow.ScreenHeight);
                    _View = LocalOverlay.View;
                }
                LocalWindow.OnResize += LocalOverlay.Resize;
                _View.OnChangeTitle += View_OnChangeTitle;
                _View.OnChangeURL += View_OnChangeURL;
                //_View.OnChangeTooltip += View_OnChangeTooltip;
                _View.OnChangeCursor += View_OnChangeCursor;
                _View.OnBeginLoading += View_OnBeginLoading;
                _View.OnFinishLoading += View_OnFinishLoading;
                _View.OnFailLoading += View_OnFailLoading;
                //_View.URL = "about:blank";
                ChildWndProcDelegate = new DllUtils.WndProcDelegate(NativeChildWndProc);
                OldChildWndProc = DllUtils.SetWindowLongPtr(ChildHwnd, DllUtils.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(ChildWndProcDelegate));

                UltralightManager.Instance.RegisterView(this);
            });
        }

        private IntPtr NativeChildWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == DllUtils.WM_SETCURSOR)
            {
                if (CurrentWin32Cursor != IntPtr.Zero)
                {
                    DllUtils.NativeSetCursor(CurrentWin32Cursor);
                    return 1;
                }
            }
            return DllUtils.CallWindowProc(OldChildWndProc, hwnd, msg, wParam, lParam);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            int Width = (int)sizeInfo.NewSize.Width;
            int Height = (int)sizeInfo.NewSize.Height;
            if (ContainerHwnd != IntPtr.Zero)
                DllUtils.SetWindowPos(ContainerHwnd, IntPtr.Zero, 0, 0, Width, Height, DllUtils.SWP_NOMOVE | DllUtils.SWP_NOZORDER | DllUtils.SWP_NOACTIVATE);
            if (ChildHwnd != IntPtr.Zero)
                DllUtils.SetWindowPos(ChildHwnd, IntPtr.Zero, 0, 0, Width, Height, DllUtils.SWP_NOMOVE | DllUtils.SWP_NOZORDER | DllUtils.SWP_FRAMECHANGED | DllUtils.SWP_NOACTIVATE);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Dispose();
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
                IntPtr NativeCursor = e.ToWin32Cursor();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentWin32Cursor = NativeCursor;
                    DllUtils.NativeSetCursor(CurrentWin32Cursor);
                });
                CurrentCursor = e;
            }
        }


        public event EventHandler<LoadingStateResult> LoadingStateChanged;
        public event EventHandler<string> UrlChanged;
        public event EventHandler<string> TitleChanged;

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
                if (LocalWindow != null && LocalOverlay != null)
                    LocalWindow.OnResize -= LocalOverlay.Resize;
                LocalOverlay?.Dispose();
                LocalWindow?.Dispose();
                if (ChildHwnd != IntPtr.Zero)
                {
                    if (OldChildWndProc != IntPtr.Zero)
                    {
                        DllUtils.SetWindowLongPtr(ChildHwnd, DllUtils.GWL_WNDPROC, OldChildWndProc);
                        OldChildWndProc = IntPtr.Zero;
                    }
                    DllUtils.DestroyWindow(ChildHwnd);
                }
                if (ContainerHwnd != IntPtr.Zero)
                    DllUtils.DestroyWindow(ContainerHwnd);
                _Disposed = true;
            }
        }

        ~UltralightHwndWebView()
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
