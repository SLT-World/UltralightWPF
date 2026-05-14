using System.Windows.Controls;
using UltralightNet;

namespace UltralightWPF
{
    public interface IRenderHandler : IDisposable
    {
        void AllocateBitmap(Image _Image, int _Width, int _Height);
        void UpdateBitmap(ULSurface Surface);
        void ReleaseBitmap();
    }
}
