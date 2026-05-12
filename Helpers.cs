using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UltralightNet;

namespace UltralightWPF
{
    public static class Helpers
    {
        public static ULKeyEventModifiers ToULKeyEventModifiers(this ModifierKeys Modifiers)
        {
            ULKeyEventModifiers ULModifiers = 0;
            if (Modifiers.HasFlag(ModifierKeys.Alt)) ULModifiers |= ULKeyEventModifiers.AltKey;
            if (Modifiers.HasFlag(ModifierKeys.Control)) ULModifiers |= ULKeyEventModifiers.CtrlKey;
            if (Modifiers.HasFlag(ModifierKeys.Shift)) ULModifiers |= ULKeyEventModifiers.ShiftKey;
            if (Modifiers.HasFlag(ModifierKeys.Windows)) ULModifiers |= ULKeyEventModifiers.MetaKey;
            return ULModifiers;
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

        public static int GetNativeScanCode(uint VirtualKey)
        {
            return (int)DllUtils.MapVirtualKey(VirtualKey, 0);
        }
    }

    public static class DllUtils
    {
        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}
