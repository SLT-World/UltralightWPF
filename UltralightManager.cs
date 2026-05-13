using System.Diagnostics;
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
        private Stopwatch FrameStopwatch = new();
        private TimeSpan TargetFramePeriod;
        private TimeSpan LastFrameTime = TimeSpan.Zero;
        private bool IsLooping;

        public static readonly ULViewConfig DefaultViewConfig = new() { IsAccelerated = false };

        public UltralightManager()
        {
            Instance = this;
        }

        public Renderer Initialize(ULSettings? Settings = null, ULConfig? Config = null)
        {
            Settings ??= new() { ForceCPURenderer = false, LoadShadersFromFileSystem = true };
            Config ??= new();
            //AppCoreMethods.ulEnableDefaultLogger("./log.txt");
            AppCoreMethods.SetPlatformFontLoader();
            ULPlatform.FileSystem = ULPlatform.DefaultFileSystem;
            GlobalApp = ULApp.Create(Settings.Value, Config.Value);
            TargetFramePeriod = TimeSpan.FromSeconds(Config.Value.AnimationTimerDelay);
            GlobalRenderer = GlobalApp.Renderer;

            FrameStopwatch.Start();
            StartLoop();

            return GlobalRenderer!;
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

        private void StartLoop()
        {
            if (IsLooping) return;
            IsLooping = true;
            CompositionTarget.Rendering += OnCompositionRendering;
        }

        private void OnCompositionRendering(object? sender, EventArgs e)
        {
            if (GlobalRenderer == null) return;

            TimeSpan CurrentElapsedTime = FrameStopwatch.Elapsed;
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
