using System.Windows;
using System.Windows.Input;
using UltralightNet;
using UltralightNet.AppCore;

namespace UltralightWPF.Example
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
            //new UltralightManager().Initialize(new ULSettings() { ForceCPURenderer = false }, new ULConfig() { AnimationTimerDelay = 1.0 / 90.0 });

            BrowserView.Initialize();
            BrowserView.Navigate("https://ultralig.ht/");
            //BrowserView.Navigate("https://slt-world.github.io/tests/");
            //BrowserView.Navigate("https://keyboardchecker.com/");
            //BrowserView.Navigate("https://www.w3schools.com/cssref/tryit.php?filename=trycss_cursor");
            //BrowserView.Navigate("file:///inspector/Main.html");
            BrowserView.TitleChanged += BrowserView_TitleChanged;
            BrowserView.UrlChanged += BrowserView_UrlChanged;
            BrowserView.LoadingStateChanged += BrowserView_LoadingStateChanged;
            BrowserView.PreviewMouseWheel += BrowserView_PreviewMouseWheel;
            //BrowserView.ToolTipChanged += BrowserView_ToolTipChanged;
        }

        private void BrowserView_UrlChanged(object? sender, string e)
        {
            AddressBox.Text = e;
        }

        private void BrowserView_LoadingStateChanged(object? sender, LoadingStateResult e)
        {
            BackButton.IsEnabled = BrowserView.CanGoBack;
            ForwardButton.IsEnabled = BrowserView.CanGoForward;
            RefreshButton.IsEnabled = BrowserView.CanReload;
            RefreshButtonIcon.Text = e.IsLoading ? "\xF78A" : "\xe72c";
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BrowserView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            BrowserView.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserView.IsLoading)
                BrowserView.Stop();
            else
                BrowserView.Reload();
        }

        private void AddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BrowserView.Navigate(AddressBox.Text);
            }
        }
    }
}