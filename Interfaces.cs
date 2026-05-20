using System.Windows.Controls;
using UltralightNet;

namespace UltralightWPF
{
    public interface IRenderHandler : IDisposable
    {
        Image? Target { get; set; }
        bool ClearDirty { get; set; }
        void AllocateBitmap(int _Width, int _Height);
        void CaptureBitmap(View View);
        void UpdateBitmap();
        void ReleaseBitmap();
    }
}
