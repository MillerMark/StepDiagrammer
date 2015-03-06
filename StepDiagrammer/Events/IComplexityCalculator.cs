using System;
using System.Windows;

namespace StepDiagrammer
{
  public interface IInputDevices
  {
    /// <summary>
    /// Returns the size of the keyboard, in cm. Width is the width of the keyboard. Height is used 
    /// to calculate the position of an embedded pointing device, and should be the distance from the 
    /// top of the keyboard down to the top of any embedded a mouse/track pads built in to the keyboard. 
    /// If the keyboard does not include any built-in mouse device, then Height should be the true 
    /// height of the keyboard.
    /// </summary>
    Size GetKeyboardSize();
    void PrepareForAnalysis();
    void AttachMousePad(MousePad mousePad, Handedness position);
    ActionType LastMouseHandedAction { get; set; }
    double GetMouseToKeyboardTransitionCost(KeyDownEvent keyDownEvent);
    double GetReachingForMouseTransitionCost();
    double GetKeyCost(string key);
  }
}
