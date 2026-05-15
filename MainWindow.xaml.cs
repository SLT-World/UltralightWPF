using System.Windows;
using System.Windows.Input;
using UltralightNet;
using UltralightNet.AppCore;

namespace UltralightWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            new UltralightManager().Initialize(new ULSettings() { ForceCPURenderer = false }, new ULConfig() { AnimationTimerDelay = 1.0 / 90.0 });

            BrowserView.Initialize(new ULViewConfig() { IsAccelerated = false });
            //BrowserView.Navigate("https://www.w3schools.com/cssref/tryit.php?filename=trycss_cursor");
            //BrowserView.Navigate("https://ultralig.ht");
            //BrowserView.Navigate("file:///inspector/Main.html");
            BrowserView.Navigate("https://slt-world.github.io/tests/");
            //BrowserView.Navigate("https://keyboardchecker.com/");
            BrowserView.TitleChanged += BrowserView_TitleChanged;
            BrowserView.PreviewMouseWheel += BrowserView_PreviewMouseWheel;
            //BrowserView.ToolTipChanged += BrowserView_ToolTipChanged;

            //TODO: HwndHost control, device scale issue encountered.
            /*AppCoreMethods.SetPlatformFontLoader();
            ULPlatform.FileSystem = ULPlatform.DefaultFileSystem;
            using ULApp App = ULApp.Create(new(), new());
            using ULWindow _Window = App.MainMonitor.CreateWindow(512, 512);
            _Window.Title = "AppCore HwndHost";
            using ULOverlay Overlay = _Window.CreateOverlay(_Window.ScreenWidth, _Window.ScreenHeight);
            _Window.OnResize += Overlay.Resize;
            _Window.OnClose += App.Quit;
            View _View = Overlay.View;
            _View.URL = "https://slt-world.github.io/tests/";
            App.Run();*/
        }

        /*protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F12 || e.SystemKey == Key.F12)
            {
                if (InspectorView.Visibility == Visibility.Visible)
                {
                    InspectorView.Visibility = Visibility.Collapsed;
                    InspectorView.Dispose();
                }
                else
                {
                    InspectorView.Visibility = Visibility.Visible;
                    BrowserView.ShowInspector(InspectorView);
                }
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }*/

        private void BrowserView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                    BrowserView.ZoomIn();
                else
                    BrowserView.ZoomOut();
                e.Handled = true;
            }
        }

        /*private void BrowserView_ToolTipChanged(object? sender, string e)
        {
            Debug.WriteLine("ToolTip: " + e);
        }*/

        private void BrowserView_TitleChanged(object? sender, string e)
        {
            Title = e;
        }
    }
}