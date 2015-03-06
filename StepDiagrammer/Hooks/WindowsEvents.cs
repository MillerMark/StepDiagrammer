using System;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace StepDiagrammer
{
  public static class WindowsEvents
  {
    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
      public int X;
      public int Y;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseLLHookStruct
    {
      public Point Point;
      /// <summary>
      /// If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
      /// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released. This 
      /// value can be one or more of the following values:
      ///     XBUTTON1 ....... The first X button was pressed or released.
      ///     XBUTTON2 ....... The second X button was pressed or released.
      /// </summary>
      public int MouseData;
      /// <summary>
      /// Event-injected flag.
      /// </summary>
      public int Flags;
      /// <summary>
      /// Time stamp for this message.
      /// </summary>
      public int Time;
      /// <summary>
      /// Extra information associated with the message. 
      /// </summary>
      public int ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHookStruct
    {
      /// <summary>
      /// Virtual-key code. The code must be a value in the range 1 to 254. 
      /// </summary>
      public int VirtualKeyCode;
      /// <summary>
      /// Hardware scan code for the key. 
      /// </summary>
      public int ScanCode;
      /// <summary>
      /// Extended-key flag, event-injected flag, context code, and transition-state flag.
      /// </summary>
      public int Flags;
      /// <summary>
      /// Time stamp for this message.
      /// </summary>
      public int Time;
      /// <summary>
      /// Extra information associated with the message. 
      /// </summary>
      public int ExtraInfo;
    }
    #endregion
    #region Callbacks...
    private static int keyboardHookHandle;
    private static int mouseHookHandle;
    private static int windowActivationHookHandle;

    private static Win.HookProc keyboardDelegate;
    private static Win.HookProc mouseDelegate;
    private static Win.WinEventDelegate windowActivationDelegate;

    private static Point previousMousePos = new Point() { X = -999999, Y = -999999 };

    private static int MouseHookProc(int nCode, int wParam, IntPtr lParam)
    {
      if (nCode >= 0)
      {
        MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

        MouseButtons button = MouseButtons.None;
        short mouseDelta = 0;
        int clickCount = 0;
        bool mouseDown = false;
        bool mouseUp = false;

        switch (wParam)
        {
          case Win.WM_LBUTTONDOWN:
            mouseDown = true;
            button = MouseButtons.Left;
            clickCount = 1;
            break;
          case Win.WM_LBUTTONUP:
            mouseUp = true;
            button = MouseButtons.Left;
            clickCount = 1;
            break;
          case Win.WM_RBUTTONDOWN:
            mouseDown = true;
            button = MouseButtons.Right;
            clickCount = 1;
            break;
          case Win.WM_MBUTTONDOWN:
            mouseDown = true;
            button = MouseButtons.Middle;
            clickCount = 1;
            break;
          case Win.WM_MBUTTONUP:
            mouseUp = true;
            button = MouseButtons.Middle;
            clickCount = 1;
            break;
          case Win.WM_RBUTTONUP:
            mouseUp = true;
            button = MouseButtons.Right;
            clickCount = 1;
            break;
          case Win.WM_MOUSEWHEEL:
            // High-order word of MouseData is the wheel delta. 
            mouseDelta = (short)((mouseHookStruct.MouseData >> 16) & 0xffff);
            break;

          // TODO: Handle WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, & WM_NCXBUTTONDBLCLK.
        }

        Point mousePos = mouseHookStruct.Point;
        MouseEventArgs e = new MouseEventArgs(button, clickCount, mousePos.X, mousePos.Y, mouseDelta);

        if (mouseUpHandler != null && mouseUp)
          mouseUpHandler.Invoke(null, e);

        if (mouseDownHandler != null && mouseDown)
          mouseDownHandler.Invoke(null, e);

        if (mouseWheelHandler != null && mouseDelta != 0)
          mouseWheelHandler.Invoke(null, e);

        if (mouseMoveHandler != null && !previousMousePos.Equals(mousePos))
        {
          previousMousePos = mousePos;
          if (mouseMoveHandler != null)
            mouseMoveHandler.Invoke(null, e);
        }

      }

      return Win.CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
    }

    private static void SubscribeToMouseEvents()
    {
      if (mouseHookHandle != 0)    // Already subscribed.
        return;

      mouseDelegate = MouseHookProc;
      mouseHookHandle = Win.SetWindowsHookEx(Win.WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);
      HandleAnyErrors(mouseHookHandle);
    }

    static bool WeHaveMouseEventListeners
    {
      get
      {
        return mouseDownHandler != null || mouseMoveHandler != null || mouseUpHandler != null || mouseWheelHandler != null;
      }
    }

    private static void TryUnsubscribeFromMouseEvents()
    {
      if (mouseHookHandle == 0)    // Already unsubscribed.
        return;

      if (WeHaveMouseEventListeners)
        return;

      int result = Win.UnhookWindowsHookEx(mouseHookHandle);
      mouseHookHandle = 0;
      mouseDelegate = null;
      HandleAnyErrors(result);
    }

    static void TriggerKeyUpEvent(KeyboardHookStruct keyboardHookStruct, ref bool handled)
    {
      Keys keyData = (Keys)keyboardHookStruct.VirtualKeyCode;
      KeyEventArgs e = new KeyEventArgs(keyData);
      keyUpHandler.Invoke(null, e);
      handled |= e.Handled;
    }

    static void TriggerKeyDownEvent(KeyboardHookStruct keyboardHookStruct, ref bool handled)
    {
      Keys keyData = (Keys)keyboardHookStruct.VirtualKeyCode;
      
      KeyEventArgs e = new KeyEventArgs(keyData);
      
       keyDownHandler.Invoke(null, e);
      handled |= e.Handled;
    }

    private static int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
    {
      bool handled = false;

      if (nCode >= 0)
      {
        KeyboardHookStruct keyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

        if (keyDownHandler != null && (wParam == Win.WM_KEYDOWN || wParam == Win.WM_SYSKEYDOWN))
          TriggerKeyDownEvent(keyboardHookStruct, ref handled);

        if (keyUpHandler != null && (wParam == Win.WM_KEYUP || wParam == Win.WM_SYSKEYUP))
          TriggerKeyUpEvent(keyboardHookStruct, ref handled);
      }

      if (handled)
        return -1;
      else
        return Win.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
    }

    private static void SubscribeToKeyboardEvents()
    {
      if (keyboardHookHandle != 0)    // Already subscribed.
        return;

      keyboardDelegate = KeyboardHookProc;
      keyboardHookHandle = Win.SetWindowsHookEx(Win.WH_KEYBOARD_LL, keyboardDelegate, IntPtr.Zero, 0);
      HandleAnyErrors(keyboardHookHandle);
    }

    private static void UnsubscribeFromKeyboardEvents()
    {
      if (keyboardHookHandle == 0)    // Already unsubscribed.
        return;

      if (keyDownHandler != null || keyUpHandler != null)   // We have listeners.
        return;

      int result = Win.UnhookWindowsHookEx(keyboardHookHandle);
      keyboardHookHandle = 0;
      keyboardDelegate = null;
      HandleAnyErrors(result);
    }

    public static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
      string activeWindowTitle = GetActiveWindowTitle();
      WindowActivatedEventArgs windowActivatedEventArgs = new WindowActivatedEventArgs(activeWindowTitle, hwnd);
      windowActivatedHandler.Invoke(null, windowActivatedEventArgs);
    }

    public static string GetActiveWindowTitle()
    {
      const int nChars = 256;
      StringBuilder stringBuilder = new StringBuilder(nChars);
      IntPtr foregroundWindowHandle = Win.GetForegroundWindow();

      if (Win.GetWindowText(foregroundWindowHandle, stringBuilder, Win.GetWindowTextLength(foregroundWindowHandle) + 1) > 0)
        return stringBuilder.ToString();
      return null;
    }

    static void HandleAnyErrors(int handle)
    {
      if (handle == 0)
        throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    static void SubscribeToWindowActivationEvents()
    {
      if (windowActivationHookHandle != 0)
        return;

      windowActivationDelegate = WinEventProc;
      windowActivationHookHandle = Win.SetWinEventHook(Win.EVENT_SYSTEM_FOREGROUND, Win.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, windowActivationDelegate, 0, 0, Win.WINEVENT_OUTOFCONTEXT);

      HandleAnyErrors(windowActivationHookHandle);
    }

    static void UnsubscribeFromWindowActivationEvents()
    {
      if (windowActivationHookHandle == 0)
        return;
      int result = Win.UnhookWinEvent(windowActivationHookHandle);
      windowActivationHookHandle = 0;
      windowActivationDelegate = null;
      HandleAnyErrors(result);
    }
    #endregion

    #region private fields...
    private static event KeyEventHandler keyDownHandler;
    private static event KeyEventHandler keyUpHandler;
    private static event MouseEventHandler mouseDownHandler;
    private static event MouseEventHandler mouseMoveHandler;
    private static event MouseEventHandler mouseUpHandler;
    private static event MouseEventHandler mouseWheelHandler;
    private static event WindowActivatedEventHandler windowActivatedHandler;
    #endregion

    // Events...
    #region KeyDown
    /// <summary>
    /// Occurs when a key is pressed. 
    /// </summary>
    public static event KeyEventHandler KeyDown
    {
      add
      {
        SubscribeToKeyboardEvents();
        keyDownHandler += value;
      }
      remove
      {
        keyDownHandler -= value;
        UnsubscribeFromKeyboardEvents();
      }
    }
    #endregion
    #region KeyUp
    /// <summary>
    /// Occurs when a key is released. 
    /// </summary>
    public static event KeyEventHandler KeyUp
    {
      add
      {
        SubscribeToKeyboardEvents();
        keyUpHandler += value;
      }
      remove
      {
        keyUpHandler -= value;
        UnsubscribeFromKeyboardEvents();
      }
    }
    #endregion
    #region MouseDown
    /// <summary>
    /// Occurs when the mouse a mouse button is pressed. 
    /// </summary>
    public static event MouseEventHandler MouseDown
    {
      add
      {
        SubscribeToMouseEvents();
        mouseDownHandler += value;
      }
      remove
      {
        mouseDownHandler -= value;
        TryUnsubscribeFromMouseEvents();
      }
    }
    #endregion
    #region MouseMove
    /// <summary>
    /// Occurs when the mouse pointer is moved. 
    /// </summary>
    public static event MouseEventHandler MouseMove
    {
      add
      {
        SubscribeToMouseEvents();
        mouseMoveHandler += value;
      }
      remove
      {
        mouseMoveHandler -= value;
        TryUnsubscribeFromMouseEvents();
      }
    }
    #endregion
    #region MouseUp
    /// <summary>
    /// Occurs when a mouse button is released. 
    /// </summary>
    public static event MouseEventHandler MouseUp
    {
      add
      {
        SubscribeToMouseEvents();
        mouseUpHandler += value;
      }
      remove
      {
        mouseUpHandler -= value;
        TryUnsubscribeFromMouseEvents();
      }
    }
    #endregion
    #region MouseWheel
    /// <summary>
    /// Occurs when the mouse wheel moves. 
    /// </summary>
    public static event MouseEventHandler MouseWheel
    {
      add
      {
        SubscribeToMouseEvents();
        mouseWheelHandler += value;
      }
      remove
      {
        mouseWheelHandler -= value;
        TryUnsubscribeFromMouseEvents();
      }
    }
    #endregion
    #region WindowActivated
    /// <summary>
    /// Occurs when a window is activated.
    /// </summary>
    public static event WindowActivatedEventHandler WindowActivated
    {
      add
      {
        SubscribeToWindowActivationEvents();
        windowActivatedHandler += value;
      }
      remove
      {
        windowActivatedHandler -= value;
        UnsubscribeFromWindowActivationEvents();
      }
    }
    #endregion
  }
}
