using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using UltralightNet;

namespace UltralightWPF
{
    public static class Helpers
    {
        public static void RaiseUIAsync(this EventHandler Handler, object? Sender)
        {
            Application.Current?.Dispatcher.BeginInvoke(async () => Handler?.Invoke(Sender, null));
        }
        public static void RaiseUIAsync<T>(this EventHandler<T> Handler, object? Sender, T Args)
        {
            Application.Current?.Dispatcher.BeginInvoke(async () => Handler?.Invoke(Sender, Args));
        }
        public static void RaiseUIAsync<T>(this Action<T> Handler, T Args)
        {
            Application.Current?.Dispatcher.BeginInvoke(async () => Handler?.Invoke(Args));
        }

        public static ULKeyEventModifiers ToULKeyEventModifiers(this ModifierKeys Modifiers)
        {
            ULKeyEventModifiers ULModifiers = 0;
            if (Modifiers.HasFlag(ModifierKeys.Alt)) ULModifiers |= ULKeyEventModifiers.AltKey;
            if (Modifiers.HasFlag(ModifierKeys.Control)) ULModifiers |= ULKeyEventModifiers.CtrlKey;
            if (Modifiers.HasFlag(ModifierKeys.Shift)) ULModifiers |= ULKeyEventModifiers.ShiftKey;
            if (Modifiers.HasFlag(ModifierKeys.Windows)) ULModifiers |= ULKeyEventModifiers.MetaKey;
            return ULModifiers;
        }

        public static Cursor ToCursor(this ULCursor e)
        {
            return e switch
            {
                ULCursor.Pointer => Cursors.Arrow,
                ULCursor.Cross => Cursors.Cross,
                ULCursor.Hand => Cursors.Hand,
                ULCursor.IBeam => Cursors.IBeam,
                ULCursor.Wait => Cursors.Wait,
                ULCursor.Help => Cursors.Help,
                ULCursor.EastResize => Cursors.SizeWE,
                ULCursor.NorthResize => Cursors.SizeNS,
                ULCursor.NorthEastResize => Cursors.SizeNESW,
                ULCursor.NorthWestResize => Cursors.SizeNWSE,
                ULCursor.SouthResize => Cursors.SizeNS,
                ULCursor.SouthEastResize => Cursors.SizeNWSE,
                ULCursor.SouthWestResize => Cursors.SizeNESW,
                ULCursor.WestResize => Cursors.SizeWE,
                ULCursor.NorthSouthResize => Cursors.SizeNS,
                ULCursor.EastWestResize => Cursors.SizeWE,
                ULCursor.NorthEastSouthWestResize => Cursors.SizeNESW,
                ULCursor.NorthWestSouthEastResize => Cursors.SizeNWSE,
                ULCursor.ColumnResize => Cursors.SizeWE,//TODO
                ULCursor.RowResize => Cursors.SizeNS,//TODO
                ULCursor.MiddlePanning => Cursors.ScrollAll,
                ULCursor.EastPanning => Cursors.ScrollE,
                ULCursor.NorthPanning => Cursors.ScrollN,
                ULCursor.NorthEastPanning => Cursors.ScrollNE,
                ULCursor.NorthWestPanning => Cursors.ScrollNW,
                ULCursor.SouthPanning => Cursors.ScrollS,
                ULCursor.SouthEastPanning => Cursors.ScrollSE,
                ULCursor.SouthWestPanning => Cursors.ScrollSW,
                ULCursor.WestPanning => Cursors.ScrollW,
                ULCursor.Move => Cursors.ScrollAll,//TODO
                ULCursor.VerticalText => Cursors.IBeam,//TODO
                ULCursor.Cell => Cursors.Cross,//TODO
                ULCursor.ContextMenu => Cursors.Arrow,//TODO
                ULCursor.Alias => Cursors.Arrow,//TODO
                ULCursor.Progress => Cursors.AppStarting,
                ULCursor.NoDrop => Cursors.No,
                ULCursor.Copy => Cursors.Arrow,//TODO
                ULCursor.None => Cursors.None,
                ULCursor.NotAllowed => Cursors.No,
                ULCursor.ZoomIn => Cursors.Arrow,//TODO
                ULCursor.ZoomOut => Cursors.Arrow,//TODO
                ULCursor.Grab => Cursors.ScrollAll,//TODO
                ULCursor.Grabbing => Cursors.ScrollAll,//TODO
                ULCursor.Custom => Cursors.Arrow,//TODO
                _ => Cursors.Arrow,
            };
        }

        public static ULMouseEventButton GetMouseEvents(this MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                return ULMouseEventButton.Left;
            if (e.ChangedButton == MouseButton.Middle)
                return ULMouseEventButton.Middle;
            if (e.ChangedButton == MouseButton.Right)
                return ULMouseEventButton.Right;
            return ULMouseEventButton.None;
        }

        public static ULMouseEventButton GetMouseEvents(this MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                return ULMouseEventButton.Left;
            if (e.MiddleButton == MouseButtonState.Pressed)
                return ULMouseEventButton.Middle;
            if (e.RightButton == MouseButtonState.Pressed)
                return ULMouseEventButton.Right;
            return ULMouseEventButton.None;
        }

        public static string KeyToString(Key _Key, ModifierKeys Modifiers)
        {
            byte[] KeyboardState = new byte[256];
            DllUtils.GetKeyboardState(KeyboardState);
            KeyboardState[(int)Key.LeftShift] = 0;
            KeyboardState[(int)Key.RightShift] = 0;
            KeyboardState[(int)Key.LeftCtrl] = 0;
            KeyboardState[(int)Key.RightCtrl] = 0;
            KeyboardState[(int)Key.LeftAlt] = 0;
            KeyboardState[(int)Key.RightAlt] = 0;
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                KeyboardState[0x10] = 0x80;
            if (Modifiers.HasFlag(ModifierKeys.Control))
                KeyboardState[0x11] = 0x80;
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                KeyboardState[0x12] = 0x80;
            uint VirtualKey = (uint)KeyInterop.VirtualKeyFromKey(_Key);
            uint ScanCode = DllUtils.MapVirtualKey(VirtualKey, 0);
            StringBuilder StringBuilder = new(5);
            int Result = DllUtils.ToUnicode(VirtualKey, ScanCode, KeyboardState, StringBuilder, StringBuilder.Capacity, 0);
            if (Result > 0)
                return StringBuilder.ToString();
            return string.Empty;
        }

        /*public static int CharToKeyCode(char Character)
        {
            short ScanResult = DllUtils.VkKeyScan(Character);
            int VirtualKeyCode = ScanResult & 0xff;
            if (ScanResult == -1) VirtualKeyCode = 0;
            return VirtualKeyCode;
        }*/

        /*public static int GetNativeScanCode(uint VirtualKey)
        {
            return (int)DllUtils.MapVirtualKey(VirtualKey, 0);
        }*/

        //TODO: Extended cursors.
        /*public static Cursor GetCursor(int CursorID)
        {
            IntPtr Handle = DllUtils.LoadCursor(IntPtr.Zero, CursorID);
            if (Handle == IntPtr.Zero) return Cursors.Arrow;
            return CursorInteropHelper.Create(new SafeFileHandle(Handle, false));
        }*/
    }

    public static class DllUtils
    {
        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        /*[DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);*/

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        public const uint PAGE_READWRITE = 0x04;
        public const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

        /*[DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern short VkKeyScan(char ch);*/
    }
}
