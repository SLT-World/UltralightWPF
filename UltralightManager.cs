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
        private ULApp? GlobalApp;
        private List<UltralightWebView> ActiveWebViews = [];
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

        public bool Initialize(ULSettings? Settings = null, ULConfig? Config = null)
        {
            if (GlobalRenderer != null)
                return true;
            Settings ??= new() { ForceCPURenderer = false, LoadShadersFromFileSystem = true };
            Config ??= new();
            TargetFramePeriod = TimeSpan.FromSeconds(Config.Value.AnimationTimerDelay);
            ManualResetEvent ReadyEvent = new(false);
            IsRunning = true;

            UltralightThread = new Thread(() =>
            {
                AppCoreMethods.SetPlatformFontLoader();
                //AppCoreMethods.ulEnablePlatformFileSystem("./assets");
                ULPlatform.FileSystem = ULPlatform.DefaultFileSystem;
                GlobalApp = ULApp.Create(Settings.Value, Config.Value);
                //WARNING: Disables native clipboard functionality.
                //GlobalRenderer = ULPlatform.CreateRenderer(Config.Value);
                GlobalRenderer = GlobalApp.Renderer;
                ReadyEvent.Set();
                //GlobalRenderer.TryStartRemoteInspectorServer("127.0.0.1", 7676);

                /*TODO: Hwnd Host functionality
                 * GlobalApp.Run();
                 * WPF UI thread remain unaffected.
                 */

                while (IsRunning)
                {
                    while (DispatchQueue.TryDequeue(out Action _Action))
                        _Action();
                    GlobalRenderer.Update();
                    for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                    {
                        UltralightWebView View = ActiveWebViews[i];
                        if (View.RenderHandler.ClearDirty)
                        {
                            View.RenderHandler.ClearDirty = false;
                            View.GetView()?.Surface?.ClearDirtyBounds();
                        }
                    }
                    GlobalRenderer.Render();
                    for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                        ActiveWebViews[i].CaptureSurfaceTexture();
                    Thread.Sleep(1);
                }
                Shutdown();
            });

            UltralightThread.SetApartmentState(ApartmentState.STA);
            UltralightThread.IsBackground = true;
            UltralightThread.Start();

            ReadyEvent.WaitOne();
            Application.Current.Exit += OnApplicationExit;
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
            CompositionTarget.Rendering -= OnCompositionRendering;
            for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                ActiveWebViews[i].Dispose();
            GlobalRenderer?.Dispose();
            GlobalApp?.Quit();
            UltralightThread?.Join();
        }

        public void RegisterView(UltralightWebView View)
        {
            if (!ActiveWebViews.Contains(View))
                ActiveWebViews.Add(View);
        }

        public void UnregisterView(UltralightWebView View)
        {
            ActiveWebViews.Remove(View);
        }

        private void OnCompositionRendering(object? sender, EventArgs e)
        {
            if (GlobalRenderer == null) return;
            TimeSpan CurrentElapsedTime = ((RenderingEventArgs)e).RenderingTime;
            TimeSpan FrameDelta = CurrentElapsedTime - LastFrameTime;
            if (FrameDelta >= TargetFramePeriod)
            {
                LastFrameTime = CurrentElapsedTime;
                for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                    ActiveWebViews[i].UpdateSurfaceTexture();
            }
        }
    }
}