using System;

namespace StepDiagrammer
{
  public class WindowActivatedEventArgs : EventArgs
  {
    public WindowActivatedEventArgs(string windowName, IntPtr hwnd)
    {
      Handle = hwnd;
      WindowName = windowName;
    }

    public string WindowName { get; private set; }
    public IntPtr Handle { get; private set; }
  }
}
