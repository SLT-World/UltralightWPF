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

        public static IntPtr ToWin32Cursor(this ULCursor e)
        {
            if (e == ULCursor.None)
                return IntPtr.Zero;
            uint Result = e switch
            {
                ULCursor.Pointer => DllUtils.IDC_ARROW,
                ULCursor.Cross => DllUtils.IDC_CROSS,
                ULCursor.Hand => DllUtils.IDC_HAND,
                ULCursor.IBeam => DllUtils.IDC_IBEAM,
                ULCursor.Wait => DllUtils.IDC_WAIT,
                ULCursor.Help => DllUtils.IDC_HELP,
                ULCursor.EastResize => DllUtils.IDC_SIZEWE,
                ULCursor.NorthResize => DllUtils.IDC_SIZENS,
                ULCursor.NorthEastResize => DllUtils.IDC_SIZENESW,
                ULCursor.NorthWestResize => DllUtils.IDC_SIZENWSE,
                ULCursor.SouthResize => DllUtils.IDC_SIZENS,
                ULCursor.SouthEastResize => DllUtils.IDC_SIZENWSE,
                ULCursor.SouthWestResize => DllUtils.IDC_SIZENESW,
                ULCursor.WestResize => DllUtils.IDC_SIZEWE,
                ULCursor.NorthSouthResize => DllUtils.IDC_SIZENS,
                ULCursor.EastWestResize => DllUtils.IDC_SIZEWE,
                ULCursor.NorthEastSouthWestResize => DllUtils.IDC_SIZENESW,
                ULCursor.NorthWestSouthEastResize => DllUtils.IDC_SIZENWSE,
                ULCursor.ColumnResize => DllUtils.IDC_SIZEWE,//TODO
                ULCursor.RowResize => DllUtils.IDC_SIZENS,//TODO
                ULCursor.MiddlePanning => DllUtils.IDC_SCROLLALL,
                ULCursor.EastPanning => DllUtils.IDC_SCROLLE,
                ULCursor.NorthPanning => DllUtils.IDC_SCROLLN,
                ULCursor.NorthEastPanning => DllUtils.IDC_SCROLLNE,
                ULCursor.NorthWestPanning => DllUtils.IDC_SCROLLNW,
                ULCursor.SouthPanning => DllUtils.IDC_SCROLLS,
                ULCursor.SouthEastPanning => DllUtils.IDC_SCROLLSE,
                ULCursor.SouthWestPanning => DllUtils.IDC_SCROLLSW,
                ULCursor.WestPanning => DllUtils.IDC_SCROLLW,
                ULCursor.Move => DllUtils.IDC_SCROLLALL,//TODO
                ULCursor.VerticalText => DllUtils.IDC_IBEAM,//TODO
                ULCursor.Cell => DllUtils.IDC_CROSS,//TODO
                ULCursor.ContextMenu => DllUtils.IDC_ARROW,//TODO
                ULCursor.Alias => DllUtils.IDC_ARROW,//TODO
                ULCursor.Progress => DllUtils.IDC_APPSTARTING,
                ULCursor.NoDrop => DllUtils.IDC_NO,
                ULCursor.Copy => DllUtils.IDC_ARROW,//TODO
                ULCursor.NotAllowed => DllUtils.IDC_NO,
                ULCursor.ZoomIn => DllUtils.IDC_ARROW,//TODO
                ULCursor.ZoomOut => DllUtils.IDC_ARROW,//TODO
                ULCursor.Grab => DllUtils.IDC_SCROLLALL,//TODO
                ULCursor.Grabbing => DllUtils.IDC_SCROLLALL,//TODO
                ULCursor.Custom => DllUtils.IDC_ARROW,//TODO
                _ => DllUtils.IDC_ARROW,
            };
            return DllUtils.LoadCursor(IntPtr.Zero, (IntPtr)Result);
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
        //https://learn.microsoft.com/en-us/windows/win32/menurc/about-cursors
        public const int WM_SETCURSOR = 0x0020;
        public const uint IDC_ARROW = 32512;
        public const uint IDC_IBEAM = 32513;
        public const uint IDC_WAIT = 32514;
        public const uint IDC_CROSS = 32515;
        public const uint IDC_UPARROW = 32516;
        public const uint IDC_SIZE = 32640;
        public const uint IDC_ICON = 32641;
        public const uint IDC_SIZENWSE = 32642;
        public const uint IDC_SIZENESW = 32643;
        public const uint IDC_SIZEWE = 32644;
        public const uint IDC_SIZENS = 32645;
        public const uint IDC_SIZEALL = 32646;
        public const uint IDC_NO = 32648;
        public const uint IDC_APPSTARTING = 32650;
        public const uint IDC_HAND = 32649;
        public const uint IDC_HELP = 32516;
        public const uint IDC_SCROLLNS = 32652;
        public const uint IDC_SCROLLWE = 32653;
        public const uint IDC_SCROLLALL = 32654;
        public const uint IDC_SCROLLN = 32655;
        public const uint IDC_SCROLLS = 32656;
        public const uint IDC_SCROLLW = 32657;
        public const uint IDC_SCROLLE = 32658;
        public const uint IDC_SCROLLNW = 32659;
        public const uint IDC_SCROLLNE = 32660;
        public const uint IDC_SCROLLSW = 32661;
        public const uint IDC_SCROLLSE = 32662;

        [DllImport("user32.dll", EntryPoint = "SetCursor")]
        public static extern IntPtr NativeSetCursor(IntPtr hCursor);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        public const int GWL_WNDPROC = -4;

        public delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

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

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        public const uint PAGE_READWRITE = 0x04;
        public const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

        /*[DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern short VkKeyScan(char ch);*/

        public const int WS_CHILD = 0x40000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_OVERLAPPED = 0x00000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_CLIPCHILDREN = 0x02000000;

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const int WS_MINIMIZE = 0x20000000;
        public const int WS_MAXIMIZE = 0x01000000;
        public const int WS_SYSMENU = 0x00080000;

        public const int WS_EX_DLGMODALFRAME = 0x00000001;
        public const int WS_EX_CLIENTEDGE = 0x00000200;
        public const int WS_EX_STATICEDGE = 0x00020000;
        public const int WS_EX_APPWINDOW = 0x00040000;

        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOSIZE = 0x0001;
    }
}
