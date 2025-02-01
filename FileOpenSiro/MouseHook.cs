using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FileOpenSiro
{
    internal class MouseHook
    {
        public abstract class AbstractInterceptMouse
        {
            #region Win32 Constants
            protected const int WH_MOUSE_LL = 0x000E;
            protected const int WM_LBUTTONDOWN = 0x0201;
            protected const int WM_LBUTTONUP = 0x0202;
            protected const int WM_LBUTTONDBLCLK = 0x0203;
            protected const int WM_RBUTTONDOWN = 0x0204;
            protected const int WM_RBUTTONUP = 0x0205;
            protected const int WM_RBUTTONDBLCLK = 0x0206;
            protected const int WM_MOUSEWHEEL = 0x020A;
            protected const int WM_XBUTTONDOWN = 0x020B;
            protected const int WM_XBUTTONUP = 0x020C;
            protected const int WM_XBUTTONDBLCLK = 0x020D;
            protected const int XBUTTON1 = 0x0001;
            protected const int XBUTTON2 = 0x0002;
            #endregion

            #region Win32API Structures
            [StructLayout(LayoutKind.Sequential)]
            public class POINT
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            public class MSLLHOOKSTRUCT
            {
                public POINT pt;
                public uint mouseData;
                public uint flags;
                public uint time;
                public UIntPtr dwExtraInfo;
            }
            #endregion

            #region Win32 Methods
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, MouseProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);
            #endregion

            #region Delegate
            private delegate IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam);
            #endregion

            #region Fields
            private MouseProc proc;
            private IntPtr hookId = IntPtr.Zero;
            #endregion

            public void Hook()
            {
                if (hookId == IntPtr.Zero)
                {
                    proc = HookProcedure;
                    using (var curProcess = Process.GetCurrentProcess())
                    {
                        using (ProcessModule curModule = curProcess.MainModule)
                        {
                            hookId = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                        }
                    }
                }
            }

            public void UnHook()
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }

            public virtual IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
            {
                return CallNextHookEx(hookId, nCode, wParam, lParam);
            }
        }

        public class InterceptMouse : AbstractInterceptMouse
        {
            #region InputEvent
            public class OriginalMouseEventArg : EventArgs
            {
                public System.Drawing.Point Point { get; }
                public MouseButtons Button { get; }

                public OriginalMouseEventArg(System.Drawing.Point pt, MouseButtons ms)
                {
                    Point = pt;
                    Button = ms;
                }
            }
            public delegate void MouseEventHandler(object sender, OriginalMouseEventArg e);
            public event MouseEventHandler MouseDownEvent;
            public event MouseEventHandler MouseUpEvent;

            protected void OnMouseDownEvent(System.Drawing.Point pt, MouseButtons ms)
            {
                MouseDownEvent?.Invoke(this, new OriginalMouseEventArg(pt, ms));
            }
            protected void OnMouseUpEvent(System.Drawing.Point pt, MouseButtons ms)
            {
                MouseUpEvent?.Invoke(this, new OriginalMouseEventArg(pt, ms));
            }
            #endregion

            public override IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONUP))
                {
                    var mo = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    OnMouseUpEvent(new System.Drawing.Point(mo.pt.x, mo.pt.y), MouseButtons.Left);
                }
                else if (nCode >= 0 && (wParam == (IntPtr)WM_RBUTTONUP))
                {
                    var mo = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    OnMouseUpEvent(new System.Drawing.Point(mo.pt.x, mo.pt.y), MouseButtons.Right);
                }
                else if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN))
                {
                    var mo = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    OnMouseDownEvent(new System.Drawing.Point(mo.pt.x, mo.pt.y), MouseButtons.Left);
                }
                else if (nCode >= 0 && (wParam == (IntPtr)WM_RBUTTONDOWN))
                {
                    var mo = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    OnMouseDownEvent(new System.Drawing.Point(mo.pt.x, mo.pt.y), MouseButtons.Right);
                }

                return base.HookProcedure(nCode, wParam, lParam);
            }
        }
    }
}
