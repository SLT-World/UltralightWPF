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

        public UltralightManager()
        {
            Instance = this;
        }

        public bool Initialize(ULSettings? Settings = null, ULConfig? Config = null)
        {
            if (GlobalRenderer != null)
                return true;
            Settings ??= new() { ForceCPURenderer = false, LoadShadersFromFileSystem = true };
            Config ??= new();
            AppCoreMethods.SetPlatformFontLoader();
            //AppCoreMethods.ulEnablePlatformFileSystem("./assets");
            ULPlatform.FileSystem = ULPlatform.DefaultFileSystem;
            GlobalApp = ULApp.Create(Settings.Value, Config.Value);
            TargetFramePeriod = TimeSpan.FromSeconds(Config.Value.AnimationTimerDelay);
            //WARNING: Disables native clipboard functionality.
            //GlobalRenderer = ULPlatform.CreateRenderer(Config.Value);
            GlobalRenderer = GlobalApp.Renderer;
            //GlobalRenderer.TryStartRemoteInspectorServer("127.0.0.1", 7676);
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
            Application.Current.Exit -= OnApplicationExit;
            CompositionTarget.Rendering -= OnCompositionRendering;
            for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                ActiveWebViews[i].Dispose();
            GlobalRenderer?.Dispose();
            GlobalApp?.Quit();
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
                GlobalRenderer.Update();
                GlobalRenderer.Render();
                for (int i = ActiveWebViews.Count - 1; i >= 0; i--)
                    ActiveWebViews[i].UpdateSurfaceTexture();
            }
        }
    }
}
