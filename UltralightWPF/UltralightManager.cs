using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using UltralightNet;
using UltralightNet.AppCore;

namespace UltralightWPF
{
    public class UltralightManager
    {
        public static UltralightManager Instance;

        public Renderer? GlobalRenderer;
        public ULApp? GlobalApp;
        private List<IUltralightWebView> ActiveWebViews = [];
        private TimeSpan TargetFramePeriod;
        private TimeSpan LastFrameTime = TimeSpan.Zero;

        public static readonly ULViewConfig DefaultViewConfig = new() { IsAccelerated = false };
        private readonly ConcurrentQueue<Action> DispatchQueue = new();

        private Thread? UltralightThread;

        public UltralightManager()
        {
            Instance = this;
        }
        public void Invoke(Action action)
        {
            DispatchQueue.Enqueue(action);
        }

        bool IsRunning = false;
        public bool IsHwndHost = false;

        public bool Initialize(ULSettings? Settings = null, ULConfig? Config = null, bool _IsHwndHost = false)
        {
            if (GlobalRenderer != null)
                return true;
            Settings ??= new() { ForceCPURenderer = false };
            Config ??= new();
            TargetFramePeriod = TimeSpan.FromSeconds(Config.Value.AnimationTimerDelay);
            ManualResetEvent ReadyEvent = new(false);
            IsRunning = true;

            UltralightThread = new Thread(() =>
            {
                AppCoreMethods.SetPlatformFontLoader();
                ULPlatform.FileSystem = ULPlatform.DefaultFileSystem;
                GlobalApp = ULApp.Create(Settings.Value, Config.Value);
                GlobalRenderer = GlobalApp.Renderer;

                if (_IsHwndHost)
                {
                    IsHwndHost = true;
                    GlobalApp.OnUpdate += () =>
                    {
                        while (DispatchQueue.TryDequeue(out Action _Action))
                            _Action();
                    };
                    ReadyEvent.Set();
                    GlobalApp.Run();
                }
                else
                {
                    ReadyEvent.Set();
                    while (IsRunning)
                    {
                        while (DispatchQueue.TryDequeue(out Action _Action))
                            _Action();
                        GlobalRenderer.Update();
                        for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                        {
                            if (ActiveWebViews[i] is UltralightWebView View)
                            {
                                if (View.RenderHandler.ClearDirty)
                                {
                                    View.RenderHandler.ClearDirty = false;
                                    View.GetView()?.Surface?.ClearDirtyBounds();
                                }
                            }
                        }
                        GlobalRenderer.Render();
                        for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                        {
                            if (ActiveWebViews[i] is UltralightWebView View)
                                View.CaptureSurfaceTexture();
                        }
                        Thread.Sleep(1);
                    }
                }
                Shutdown();
            });

            UltralightThread.SetApartmentState(ApartmentState.STA);
            UltralightThread.IsBackground = true;
            UltralightThread.Start();

            ReadyEvent.WaitOne();
            Application.Current.Exit += OnApplicationExit;
            if (!_IsHwndHost)
                CompositionTarget.Rendering += OnCompositionRendering;
            return true;
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            Shutdown();
        }

        public void Shutdown()
        {
            IsRunning = false;
            Application.Current.Exit -= OnApplicationExit;
            if (!IsHwndHost)
                CompositionTarget.Rendering -= OnCompositionRendering;
            for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                ActiveWebViews[i].Dispose();
            GlobalRenderer?.Dispose();
            GlobalApp?.Quit();
            UltralightThread?.Join();
        }

        public void RegisterView(IUltralightWebView View)
        {
            if (!ActiveWebViews.Contains(View))
                ActiveWebViews.Add(View);
        }

        public void UnregisterView(IUltralightWebView View)
        {
            ActiveWebViews.Remove(View);
        }

        private void OnCompositionRendering(object? sender, EventArgs e)
        {
            if (GlobalRenderer == null || IsHwndHost) return;
            TimeSpan CurrentElapsedTime = ((RenderingEventArgs)e).RenderingTime;
            if (CurrentElapsedTime - LastFrameTime >= TargetFramePeriod)
            {
                LastFrameTime = CurrentElapsedTime;
                for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                {
                    if (ActiveWebViews[i] is UltralightWebView View)
                        View.UpdateSurfaceTexture();
                }
            }
        }
    }
}