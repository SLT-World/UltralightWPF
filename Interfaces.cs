using System.Windows.Controls;
using UltralightNet;

namespace UltralightWPF
{
    public interface IRenderHandler : IDisposable
    {
        bool ClearDirty { get; set; }
        void AllocateBitmap(Image _Image, int _Width, int _Height);
        void CaptureBitmap(View View);
        void UpdateBitmap(View View);
        void ReleaseBitmap();
    }
}
