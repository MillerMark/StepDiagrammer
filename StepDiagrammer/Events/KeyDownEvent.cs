using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StepDiagrammer
{
  public class KeyDownEvent : Event
  {
    static Dictionary<string, string> keyNames = new Dictionary<string, string>();
    // Test these: "Tab", "CAPSLOCK", "Back", "Return", Escape

    public override double GetMillerComplexityScore(Event previousEvent)
    {
      double baseScore = 0;
      if (ComplexityCalculator.LastMouseHandedAction == ActionType.Mouse)
        baseScore = ComplexityCalculator.GetMouseToKeyboardTransitionCost(this);  // Transitions between mouse and keyboard are expensive!
      else
      {
        KeyDownEvent keyDownEvent = previousEvent as KeyDownEvent;
        if (keyDownEvent != null && keyDownEvent.ShiftKeyDown != ShiftKeyDown && Start - keyDownEvent.Start < TimeSpan.FromMilliseconds(750))
          baseScore = 3;  // Transitions between rapidly-typed shifted & normal keys are moderately expensive.
      }

      return baseScore + ComplexityCalculator.GetKeyCost(Data);
    }

    static void BuildKeyNamesDictionary()
    {
      keyNames.Add("RShiftKey", "Shift (right)");
      keyNames.Add("LShiftKey", "Shift (left)");
      keyNames.Add("OemPeriod", ".");
      keyNames.Add("Oemtilde", "~");
      keyNames.Add("LMenu", "Alt (left)");
      keyNames.Add("RMenu", "Alt (right)");
      keyNames.Add("OemQuestion", "?");
      keyNames.Add("OemMinus", "-");
      keyNames.Add("Oemplus", "+");
      keyNames.Add("Oemcomma", ",");
      keyNames.Add("OemOpenBrackets", "[");
      keyNames.Add("Oem7", "'");
      keyNames.Add("Oem6", "]");
      keyNames.Add("Oem5", "\\");
      keyNames.Add("Oem1", ";");
      keyNames.Add("RControlKey", "Ctrl (right)");
      keyNames.Add("LControlKey", "Ctrl (left)");
      keyNames.Add("LWin", "Win (left)");
      keyNames.Add("RWin", "Win (right)");
    }

    static KeyDownEvent()
    {
      BuildKeyNamesDictionary();
    }
    public VerticalAlignment KeyNameVerticalAlignment
    {
      get
      {
        if (ShortDisplayText.Length > 1)
          return VerticalAlignment.Bottom;
        else
          return VerticalAlignment.Top;
      }
    }
    
    public double KeyFontSize
    {
      get
      {
        if (ShortDisplayText.Length > 1)
          return 12;
        else
          return 24;
      }
    }
    
    public System.Drawing.Point GetCaretPosition()
    {
      Win.GUITHREADINFO guiInfo = new Win.GUITHREADINFO();
      guiInfo.cbSize = (uint)Marshal.SizeOf(guiInfo);
      if (!Win.GetGUIThreadInfo(0, out guiInfo))
        return System.Drawing.Point.Empty;

      System.Drawing.Point caretPosition = new System.Drawing.Point();
      caretPosition.X = (int)guiInfo.rcCaret.Left + 10;   // +10 to better center the caret.
      caretPosition.Y = (int)guiInfo.rcCaret.Bottom - 10;   // -10 to better center the caret.

      Win.ClientToScreen(guiInfo.hwndCaret, ref caretPosition);

      return caretPosition;
    }

    private static bool PointIsOutsideRect(System.Drawing.Point pt, Win.RECT rect)
    {
      return pt.Y < rect.Top || pt.X < rect.Left || pt.X > rect.Right || pt.Y > rect.Bottom;
    }

    public void CaptureActiveControl(IntPtr handle)
    {
      IntPtr activeControl = Win.GetActiveControl(handle);
      if (activeControl != IntPtr.Zero)
      {
        Win.RECT activeWindowRect;
        Win.GetWindowRect(activeControl, out activeWindowRect);
        Rect myRect;
        System.Drawing.Point caretPosition = GetCaretPosition();
        if (caretPosition == System.Drawing.Point.Empty)
        {
          double width = activeWindowRect.Right - activeWindowRect.Left;
          double height = activeWindowRect.Bottom - activeWindowRect.Top;
          ConversionHelper.ResizeToArea(ref width, ref height, 900);
          myRect = new Rect(activeWindowRect.Left, activeWindowRect.Top, width, height);
        }
        else
        {
          if (PointIsOutsideRect(caretPosition, activeWindowRect))
            myRect = new Rect(activeWindowRect.Left, activeWindowRect.Top, Math.Max(50, Math.Min(activeWindowRect.Right - activeWindowRect.Left, 200)), Math.Max(25, Math.Min(activeWindowRect.Bottom - activeWindowRect.Top, 100)));
          else
          {
            myRect = new Rect(caretPosition.X, caretPosition.Y, 1, 1);
            myRect.Inflate(100, 50);
          }
        }

        ScreenCapture.SetAreaOfFocus(myRect);
        CaptureScreen();
      }
    }

    public void CaptureActiveControl()
    {
      Rect rect = new Rect();
      ScreenCapture.SetAreaOfFocus(rect);
      CaptureScreen();
    }

    protected virtual double GetSaturation()
    {
      if (Duration.TotalMilliseconds > 300)
        return 0.7;   // Tends to take more force to hold the key down then to get a single click.
      else
        return 0.5;
    }

    /// <summary>
    /// Gets the hue and saturation for this event.
    /// </summary>
    /// <param name="hue">A 0-255 value for the hue.</param>
    /// <param name="saturation">A 0-1.0 value for the saturation.</param>
    protected override void GetHueAndSaturation(out double hue, out double saturation)
    {
      hue = 0;
      saturation = GetSaturation();
    }

    protected override string GetDisplayText()
    {
      string displayText = GetShortDisplayText();
      if (displayText.StartsWith("(") && displayText.Length >= 2)
        return displayText;
      string repeatCountStr = string.Empty;
      if (RepeatCount > 1)
        repeatCountStr = String.Format(" (x{0})", RepeatCount);
      return String.Format("\"{0}\"{1}", displayText, repeatCountStr);
    }

    protected override string GetShortDisplayText()
    {
      if (Data == null)
        return String.Empty;

      if (keyNames.ContainsKey(Data))
        return keyNames[Data];
      
      if (Data.Length == 1 && char.IsLetter(Data[0]) && !ShiftKeyDown)
        return char.ToLower(Data[0]).ToString();
      return Data;
    }

    protected override double GetForce()
    {
      if (Duration.TotalMilliseconds > 300)
        return 0.54;
      else
        return 0.45;
    }

    protected override void PrepareToGetPolygonPoints(out double leftIndent, out double baseStretch)
    {
      base.PrepareToGetPolygonPoints(out leftIndent, out baseStretch);
      baseStretch = 2;
    }

    protected override PointCollection GetPolygonPoints(double width, double height, double leftIndent, double rightIndent, double baseStretch)
    {
      return VectorAssist.GetSpikePoints(width, height, leftIndent, rightIndent, baseStretch);
    }

    public int RepeatCount { get; set; }

    public bool ShiftKeyDown { get; set; }
  }
}
