using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            AppCoreMethods.ulEnableDefaultLogger("./log.txt");
            AppCoreMethods.SetPlatformFontLoader();
            ULPlatform.FileSystem = ULPlatform.DefaultFileSystem;
            UltralightApp = ULApp.Create(new ULSettings(), new ULConfig());
            _Renderer = UltralightApp.Renderer;
            BrowserView.Initialize(_Renderer);
            BrowserView.Navigate("https://slt-world.github.io/tests/inputs.html");
        }
    }
}