using System.Windows;
using UltralightNet;
using UltralightNet.AppCore;

namespace UltralightWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Renderer _Renderer;
        private ULApp UltralightApp;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //AppCoreMethods.ulEnableDefaultLogger("./log.txt");
            AppCoreMethods.SetPlatformFontLoader();
            ULPlatform.FileSystem = ULPlatform.DefaultFileSystem;
            UltralightApp = ULApp.Create(new ULSettings() { ForceCPURenderer = false }, new ULConfig());
            _Renderer = UltralightApp.Renderer;
            //TODO: Investigate nonfunctional GPU acceleration.
            BrowserView.Initialize(_Renderer, new ULViewConfig() { IsAccelerated = false });
            //BrowserView.Navigate("https://www.w3schools.com/cssref/tryit.php?filename=trycss_cursor");
            BrowserView.Navigate("https://slt-world.github.io/tests/");
            //BrowserView.Navigate("https://keyboardchecker.com/");
            BrowserView.TitleChanged += BrowserView_TitleChanged;
            //BrowserView.ToolTipChanged += BrowserView_ToolTipChanged;
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