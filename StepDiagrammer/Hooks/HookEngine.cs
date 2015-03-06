using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Input;

namespace StepDiagrammer
{
  public static class HookEngine
  {
    private static IntPtr activeWindowHandle;
    static object currentProcessTimerLocker = new Object();
    public static event UsageChangeEventHandler UsageChanged;
    static UsageChangedEventArgs usageChangedEventArgs = new UsageChangedEventArgs();
    static short lastUsage;
    static bool listening;
    static CpuUsageCalculator cpuUsageCalculator;
    public static StepDiagram StepDiagram { get; set; }

    static void HookEvents()
    {
      if (listening)
        return;
      WindowsEvents.KeyDown += KeyDownListener;
      WindowsEvents.KeyUp += HookManager_KeyUp;
      WindowsEvents.MouseMove += HookManager_MouseMove;
      WindowsEvents.MouseUp += HookManager_MouseUp;
      WindowsEvents.MouseDown += HookManager_MouseDown;
      WindowsEvents.MouseWheel += HookManager_MouseWheel;
      WindowsEvents.WindowActivated += HookManager_WindowActivated;
      listening = true;
    }

    static void UnhookEvents()
    {
      if (!listening)
        return;
      WindowsEvents.KeyDown -= KeyDownListener;
      WindowsEvents.KeyUp -= HookManager_KeyUp;
      WindowsEvents.MouseMove -= HookManager_MouseMove;
      WindowsEvents.MouseUp -= HookManager_MouseUp;
      WindowsEvents.MouseDown -= HookManager_MouseDown;
      WindowsEvents.MouseWheel -= HookManager_MouseWheel;
      WindowsEvents.WindowActivated -= HookManager_WindowActivated;
      listening = false;
    }

    public static void OnUsageChanged(object sender, UsageChangedEventArgs e)
    {
      UsageChangeEventHandler handler = UsageChanged;
      if (handler != null)
        handler(sender, e);
    }

    static void CheckCpuUsage(object obj)
    {
      Process currentProcess = Process.GetCurrentProcess();
      short usage = cpuUsageCalculator.GetUsage(currentProcess);
      if (usage >= 0 && usage != lastUsage)
      {
        usageChangedEventArgs.SetValues(lastUsage, usage);
        App.Current.Dispatcher.Invoke((Action)(() => OnUsageChanged(null, usageChangedEventArgs)));
      }
      lastUsage = usage;
    }

    static Timer currentProcessTimer;

    public static void Start()
    {
      if (StepDiagram != null)
        throw new Exception("Unable to start twice. Already listening. Call HookEngine.Stop after calling HookEngine.Start");
      activeWindowHandle = Win.GetActiveWindow();
      StepDiagram = new StepDiagram();
      cpuUsageCalculator = new CpuUsageCalculator();
      currentProcessTimer = new Timer(CheckCpuUsage, null, 100, 100);
      HookEvents();
      StepDiagram.AddTaskActive(WindowsEvents.GetActiveWindowTitle(), Win.GetForegroundWindow());
    }

    public static StepDiagram Stop()
    {
      if (StepDiagram == null)
        throw new Exception("Call to Stop() failed. Must call HookEngine.Start before calling HookEngine.Stop.");

      lock (currentProcessTimerLocker)
      {
        currentProcessTimer.Change(Timeout.Infinite, Timeout.Infinite);
        currentProcessTimer.Dispose();
        currentProcessTimer = null;
      }

      cpuUsageCalculator = null;
      UnhookEvents();
      StepDiagram result = StepDiagram;
      result.PostProcess();
      StepDiagram = null;
      return result;
    }

    static void HookManager_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      StepDiagram.AddMouseWheel();
    }

    static void HookManager_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      StepDiagram.AddMouseDown(e.Button, e.X, e.Y);
    }

    static void HookManager_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      StepDiagram.AddMouseUp(e.Button);
    }

    static void HookManager_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      StepDiagram.AddMouseMove(e.X, e.Y);
    }

    static void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
    {
      StepDiagram.AddKeyUp(e.KeyValue);
    }

    static void KeyDownListener(object sender, System.Windows.Forms.KeyEventArgs e)
    {
      bool shiftKeyDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
      StepDiagram.AddKeyDown(e.KeyValue, e.KeyData, shiftKeyDown, activeWindowHandle);
    }

    static void HookManager_WindowActivated(object sender, WindowActivatedEventArgs e)
    {
      StepDiagram.AddTaskActive(e.WindowName, e.Handle);
    }

    public static bool Listening
    {
      get
      {
        return listening;
      }
    }
  }
}
