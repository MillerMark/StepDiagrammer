using System;
using System.Text;
using System.Runtime.InteropServices;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace StepDiagrammer
{
  public static class Win
  {
    public delegate int HookProc(int nCode, int wParam, IntPtr lParam);
    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GUITHREADINFO
    {
      public uint cbSize;
      public uint flags;
      public IntPtr hwndActive;
      public IntPtr hwndFocus;
      public IntPtr hwndCapture;
      public IntPtr hwndMenuOwner;
      public IntPtr hwndMoveSize;
      public IntPtr hwndCaret;
      public RECT rcCaret;
    };

    public const int WH_MOUSE_LL = 14;
    public const int WH_KEYBOARD_LL = 13;
    public const uint WINEVENT_OUTOFCONTEXT = 0;
    public const uint EVENT_SYSTEM_FOREGROUND = 3;
    public const int WM_LBUTTONDOWN = 0x201;
    public const int WM_RBUTTONDOWN = 0x204;
    public const int WM_MBUTTONDOWN = 0x207;
    public const int WM_LBUTTONUP = 0x202;
    public const int WM_RBUTTONUP = 0x205;
    public const int WM_MBUTTONUP = 0x208;
    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_KEYDOWN = 0x100;
    public const int WM_KEYUP = 0x101;
    public const int WM_SYSKEYDOWN = 0x104;
    public const int WM_SYSKEYUP = 0x105;

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);
    
    [DllImport("user32.dll")]
    public static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, System.Drawing.CopyPixelOperation rop);
    
    [DllImport("user32.dll")]
    public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    
    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    
    [DllImport("gdi32.dll")]
    public static extern IntPtr DeleteDC(IntPtr hDc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    public static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern int GetDoubleClickTime();

    [DllImport("user32.dll")]
    public static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "GetGUIThreadInfo")]
    public static extern bool GetGUIThreadInfo(uint tId, out GUITHREADINFO threadInfo);

    [DllImport("user32.dll")]
    public static extern int GetKeyboardState(byte[] pbKeyState);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern short GetKeyState(int vKey);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetSystemTimes(out FileTime lpIdleTime, out FileTime lpKernelTime, out FileTime lpUserTime);
    
    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr ptr);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    [DllImport("user32.dll")]
    public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int UnhookWindowsHookEx(int idHook);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int UnhookWinEvent(int hWinEventHook);

    /// <summary>
    /// Returns the active control for the active window.
    /// </summary>
    /// <param name="handle">The handle to the current application's window.</param>
    public static IntPtr GetActiveControl(IntPtr handle)
    {
      IntPtr thisWindowThread = GetWindowThreadProcessId(handle, IntPtr.Zero);
      IntPtr activeWindowThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
      
      IntPtr activeControlHandle = IntPtr.Zero;

      AttachThreadInput(activeWindowThread, thisWindowThread, true);    // Hijack thread.
      try
      {
        activeControlHandle = GetFocus();
      }
      finally
      {
        AttachThreadInput(activeWindowThread, thisWindowThread, false);   // Restore normal operation.
      }
      
      return activeControlHandle;
    }
  }
}
