using UltralightNet;

namespace UltralightWPF
{
    public interface IUltralightWebView : IDisposable
    {
        string Title { get; }
        bool CanGoBack { get; }
        bool CanGoForward { get; }
        bool CanReload { get; }
        bool IsLoading { get; }
        bool IsBrowserInitialized { get; }
        string Url { get; set; }

        void Navigate(string Url);

        void NavigateToString(string Text);
        void GoBack();
        void GoForward();
        void Reload();
        void Stop();
        View? GetView();

        double ZoomLevel { get; set; }
        double ZoomFactor { get; set; }
        void ZoomIn();
        void ZoomOut();
        void ZoomReset();

        void Initialize(ULViewConfig? Config = null, View? InitialView = null);

        event EventHandler<LoadingStateResult> LoadingStateChanged;
        event EventHandler<string> UrlChanged;
        event EventHandler<string> TitleChanged;
    }
}
