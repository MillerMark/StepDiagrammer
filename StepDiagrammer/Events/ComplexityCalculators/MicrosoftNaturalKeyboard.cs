using System;
using System.Windows;
using System.Windows.Forms;
using System.Collections.Generic;

namespace StepDiagrammer
{
  /// <summary>
  /// This class calculates the Miller Complexity Index for key presses based on target size of 
  /// individual keys and distance from fingers. Finger position is based on a history of known 
  /// previous key presses for the left and right hands.
  /// 
  /// All Size and Point values are measured in cm. Point coordinates are measured from an origin 
  /// positioned at the top left of the keyboard, with positive values running to the right and down.
  /// 
  /// TODO: Instead of building the keyboard in code, create an engine that can load directly from 
  /// XML and a UI for creating keyboard models interactively.
  /// </summary>
  public class MicrosoftNaturalKeyboard : IInputDevices
  {
    public static class KeySize
    {
      const double StandardKeyHeight = 1.8;  //cm
      const double FunctionKeyHeight = 1.5;  //cm
      const double StandardKeyWidth = 1.8;  //cm

      // Key sizes (cm) for the Microsoft Natural Keyboard...
      public static readonly Size StandardKey = new Size(StandardKeyWidth, StandardKeyHeight);
      public static readonly Size T = new Size(2.9, StandardKeyHeight); 
      public static readonly Size H = new Size(2.7, StandardKeyHeight); 
      public static readonly Size N = new Size(3.5, StandardKeyHeight); 
      public static readonly Size G = new Size(2.0, StandardKeyHeight); 
      public static readonly Size D7 = new Size(3.0, StandardKeyHeight); 
      public static readonly Size Add = new Size(1.9, 3.5); 
      public static readonly Size NumPadEnter = new Size(1.8, 3.3);
      public static readonly Size NumPad0 = new Size(3.5, StandardKeyHeight); 
      public static readonly Size Space = new Size(14.7, 4.8); 
      public static readonly Size Escape = new Size(2.4, FunctionKeyHeight); 
      public static readonly Size Tilde = new Size(2.9, StandardKeyHeight); 
      public static readonly Size Tab = new Size(3.7, StandardKeyHeight); 
      public static readonly Size CapsLock = new Size(3.6, StandardKeyHeight); 
      public static readonly Size LeftShift = new Size(4.1, StandardKeyHeight);
      public static readonly Size RightShift = new Size(3.9, StandardKeyHeight); 
      public static readonly Size Ctrl = new Size(3.5, 2.5); 
      public static readonly Size Windows = new Size(3.5, 2.6);
      public static readonly Size LeftAlt = new Size(2.5, 2.8);
      public static readonly Size RightAlt = new Size(2.9, 2.8);
      public static readonly Size ContextMenu = new Size(2.8, 2.6);
      public static readonly Size Enter = new Size(3.0, StandardKeyHeight);
      public static readonly Size Backspace = new Size(3.5, StandardKeyHeight); 
      
    }
    const double DBL_MinimumDistanceBetweenKeys = 1.9; // cm
    KeyboardSection arrowKeys;
    ActionType lastMouseHandedAction;
    KeyboardSection numPadKeys;
    KeyboardSection numPadTopRowOperators;
    KeyboardSection homeKeysLeft;
    KeyboardSection homeKeysRight;
    KeyboardSection extendedKeys;
    KeyboardSection functionKeysRight;
    KeyboardSection punctuationKeysRight;
    Point mousePadCenter = new Point(double.MinValue, double.MinValue);
    
    /// <summary>
    /// Returns the size of the keyboard, in cm. 
    /// </summary>
    /// <returns></returns>
    public Size GetKeyboardSize()
    {
      return new Size(50, 25.4);
    }

    List<KeyboardSection> keyboardSections = new List<KeyboardSection>();
    PhysicalKey lastLeftKeyPressed;
    PhysicalKey lastRightKeyPressed;
    PhysicalKey lastAmbidextrousKeyPressed;
    DateTime timeOfLastAmbidextrousKeyPressed = DateTime.MinValue;
    DateTime timeOfLastLeftKeyPress = DateTime.MinValue;
    DateTime timeOfLastRightKeyPress = DateTime.MinValue;
    
    public void AttachMousePad(MousePad mousePad, Handedness position)
    {
      MousePadPosition = position;
      MousePad = mousePad;
    }

    public double GetMouseToKeyboardTransitionCost(KeyDownEvent keyDownEvent)
    {
      PhysicalKey key = FindKey(keyDownEvent.Data);
      if (key != null)
      {
        KeyboardSection parentSection = key.ParentSection;
        if (parentSection.Handedness == Handedness.Right)
        {
          lastMouseHandedAction = ActionType.Keyboard;
          if (MousePad == null)
            throw new Exception("MousePad not specified.");
          if (mousePadCenter.X == double.MinValue)
            CalculateMousePadCenterPoint();

          double travelDistance = MillerComplexity.GetDistance(parentSection.Center, mousePadCenter);
          double targetApproachWidth = MillerComplexity.GetTargetApproachWidth(key.Size.Width, key.Size.Height);
          return MillerComplexity.GetTargetPressScore(travelDistance, targetApproachWidth);
        }
      }
      return 0;
    }

    public void PrepareForAnalysis()
    {
      lastLeftKeyPressed = null;
      lastRightKeyPressed = null;
      lastAmbidextrousKeyPressed = null;
      lastMouseHandedAction = ActionType.None;
    }

    KeyboardSection GetArrowKeys()
    {
      KeyboardSection arrowKeys = new KeyboardSection(new Point(37.1, 14.7), new Size(6.2, 5.2), Handedness.Right);
      arrowKeys.Add(Keys.Left, Sides.Left | Sides.Top | Sides.Bottom);
      arrowKeys.Add(Keys.Up, Sides.Left | Sides.Top | Sides.Right);
      arrowKeys.Add(Keys.Right, Sides.Bottom | Sides.Top | Sides.Right);
      arrowKeys.Add(Keys.Down, Sides.Bottom);
      return arrowKeys;
    }

    KeyboardSection GetFunctionKeysLeft()
    {
      KeyboardSection functionKeysLeft = new KeyboardSection(new Point(10.25, 5.5), new Size(9.4, 3.7), Handedness.Left);
      functionKeysLeft.Add(Keys.F1, Sides.Left | Sides.Top | Sides.Bottom);
      functionKeysLeft.Add(Keys.F2, Sides.Top | Sides.Bottom);
      functionKeysLeft.Add(Keys.F3, Sides.Top | Sides.Bottom);
      functionKeysLeft.Add(Keys.F4, Sides.Top | Sides.Bottom);
      functionKeysLeft.Add(Keys.F5, Sides.Top | Sides.Bottom | Sides.Right);
      return functionKeysLeft;
    }

    KeyboardSection GetFunctionKeysRight()
    {
      KeyboardSection functionKeysRight = new KeyboardSection(new Point(25.4, 6), new Size(13.4, 2.0), Handedness.Right);
      functionKeysRight.Add(Keys.F6, Sides.Left | Sides.Top | Sides.Bottom);
      functionKeysRight.Add(Keys.F7, Sides.Top | Sides.Bottom);
      functionKeysRight.Add(Keys.F8, Sides.Top | Sides.Bottom);
      functionKeysRight.Add(Keys.F9, Sides.Top | Sides.Bottom);
      functionKeysRight.Add(Keys.F10, Sides.Top | Sides.Bottom);
      functionKeysRight.Add(Keys.F11, Sides.Top | Sides.Bottom);
      functionKeysRight.Add(Keys.F12, Sides.Top | Sides.Bottom);
      return functionKeysRight;
    }

    KeyboardSection GetHomeKeysLeft()
    {
      KeyboardSection homeKeysLeft = new KeyboardSection(new Point(10, 11.85), new Size(9.6, 5.4), Handedness.Left);
      homeKeysLeft.Add(Keys.Q);
      homeKeysLeft.Add(Keys.W);
      homeKeysLeft.Add(Keys.E);
      homeKeysLeft.Add(Keys.R);
      homeKeysLeft.Add(Keys.T, Sides.Right, KeySize.T);

      homeKeysLeft.Add(Keys.A);
      homeKeysLeft.Add(Keys.S);
      homeKeysLeft.Add(Keys.D);
      homeKeysLeft.Add(Keys.F);
      homeKeysLeft.Add(Keys.G, Sides.Right, KeySize.G);

      homeKeysLeft.Add(Keys.Z);
      homeKeysLeft.Add(Keys.X);
      homeKeysLeft.Add(Keys.C);
      homeKeysLeft.Add(Keys.V);
      homeKeysLeft.Add(Keys.B, Sides.Right);

      return homeKeysLeft;
    }

    KeyboardSection GetHomeKeysRight()
    {
      KeyboardSection homeKeysRight = new KeyboardSection(new Point(23.3, 12.7), new Size(9.7, 5.5), Handedness.Right);
      homeKeysRight.Add(Keys.Y, Sides.Left);
      homeKeysRight.Add(Keys.U);
      homeKeysRight.Add(Keys.I);
      homeKeysRight.Add(Keys.O);
      homeKeysRight.Add(Keys.P);

      homeKeysRight.Add(Keys.H, Sides.Left, KeySize.H);
      homeKeysRight.Add(Keys.J);
      homeKeysRight.Add(Keys.K);
      homeKeysRight.Add(Keys.L);

      homeKeysRight.Add(Keys.N, Sides.Left, KeySize.N);
      homeKeysRight.Add(Keys.M);

      return homeKeysRight;
    }

    KeyboardSection GetNumKeysLeft()
    {
      KeyboardSection numKeysLeft = new KeyboardSection(new Point(23.3, 12.7), new Size(9.7, 5.5), Handedness.Left);
      numKeysLeft.Add(Keys.D1, Sides.Top);
      numKeysLeft.Add(Keys.D2, Sides.Top);
      numKeysLeft.Add(Keys.D3, Sides.Top);
      numKeysLeft.Add(Keys.D4, Sides.Top);
      numKeysLeft.Add(Keys.D5, Sides.Top);
      numKeysLeft.Add(Keys.D6, Sides.Top | Sides.Right);
      return numKeysLeft;
    }

    KeyboardSection GetNumKeysRight()
    {
      KeyboardSection numKeysRight = new KeyboardSection(new Point(37.2, 9.2), new Size(5.8, 5.5), Handedness.Right);
      numKeysRight.Add(Keys.D7, Sides.Top | Sides.Left, KeySize.D7);
      numKeysRight.Add(Keys.D8, Sides.Top);
      numKeysRight.Add(Keys.D9, Sides.Top);
      numKeysRight.Add(Keys.D0, Sides.Top);
      return numKeysRight;
    }

    KeyboardSection GetExtendedNavKeys()
    {
      KeyboardSection extendedNavKeys = new KeyboardSection(new Point(23.3, 12.7), new Size(9.7, 4.1), Handedness.Right);
      extendedNavKeys.Add(Keys.Insert, Sides.Top | Sides.Left);
      extendedNavKeys.Add(Keys.Home, Sides.Top);
      extendedNavKeys.Add(Keys.PageUp, Sides.Top | Sides.Right);
      extendedNavKeys.Add(Keys.Delete, Sides.Left | Sides.Bottom);
      extendedNavKeys.Add(Keys.End, Sides.Bottom);
      extendedNavKeys.Add(Keys.PageDown, Sides.Bottom | Sides.Right);
      return extendedNavKeys;
    }

    KeyboardSection GetNumPadKeys()
    {
      KeyboardSection numPadKeys = new KeyboardSection(new Point(44.4, 12.2), new Size(7.75, 9.6), Handedness.Right);
      numPadKeys.Add(Keys.NumLock, Sides.Left | Sides.Top);
      numPadKeys.Add(Keys.Divide, Sides.Top);
      numPadKeys.Add(Keys.Multiply, Sides.Top);
      numPadKeys.Add(Keys.Subtract, Sides.Top | Sides.Right);
      numPadKeys.Add(Keys.NumPad7, Sides.Left);
      numPadKeys.Add(Keys.NumPad8);
      numPadKeys.Add(Keys.NumPad9);
      numPadKeys.Add(Keys.Add, Sides.Right, KeySize.Add);
      numPadKeys.Add(Keys.NumPad4, Sides.Left);
      numPadKeys.Add(Keys.NumPad5);
      numPadKeys.Add(Keys.NumPad6);
      numPadKeys.Add(Keys.NumPad1, Sides.Left);
      numPadKeys.Add(Keys.NumPad2);
      numPadKeys.Add(Keys.NumPad3);
      numPadKeys.Add(Keys.Enter, Sides.Right | Sides.Bottom, KeySize.NumPadEnter);
      numPadKeys.Add(Keys.NumPad0, Sides.Left | Sides.Bottom, KeySize.NumPad0);
      numPadKeys.Add(Keys.Delete);
      
      return numPadKeys;
    }

    KeyboardSection GetSpaceBar()
    {
      KeyboardSection spaceBarSection = new KeyboardSection(new Point(16.7, 17.2), new Size(14.7, 4.8), Handedness.Both);
      spaceBarSection.Add(Keys.Space, Sides.Bottom, KeySize.Space);
      return spaceBarSection;
    }

    KeyboardSection GetEscape()
    {
      KeyboardSection escapeSection = new KeyboardSection(new Point(3.1, 4.3), new Size(2.4, 1.9), Handedness.Left);
      escapeSection.Add(Keys.Escape, Sides.Bottom | Sides.Left | Sides.Right | Sides.Top, KeySize.Escape);
      return escapeSection;
    }

    KeyboardSection GetLeftKeyColumn()
    {
      KeyboardSection leftKeyColumn = new KeyboardSection(new Point(2.6, 8.2), new Size(3.9, 5.9), Handedness.Left);
      leftKeyColumn.Add(Keys.Oemtilde, Sides.Left | Sides.Top, KeySize.Tilde);
      leftKeyColumn.Add(Keys.Tab, Sides.Left, KeySize.Tab);
      leftKeyColumn.Add(Keys.Capital, Sides.Left, KeySize.CapsLock);
      leftKeyColumn.Add(Keys.CapsLock, Sides.Left, KeySize.CapsLock);

      return leftKeyColumn;
    }

    KeyboardSection GetLeftModifiers()
    {
      KeyboardSection leftModifiers = new KeyboardSection(new Point(5.3, 14.8), new Size(8.6, 6.8), Handedness.Left);
      leftModifiers.Add(Keys.LShiftKey, Sides.Left, KeySize.LeftShift);
      leftModifiers.Add(Keys.LControlKey, Sides.Left | Sides.Bottom, KeySize.Ctrl);
      leftModifiers.Add(Keys.LWin, Sides.Bottom, KeySize.Windows);
      leftModifiers.Add(Keys.LMenu, Sides.Bottom, KeySize.LeftAlt);
      return leftModifiers;
    }

    KeyboardSection GetRightModifiers()
    {
      KeyboardSection rightModifiers = new KeyboardSection(new Point(28.9, 15.5), new Size(10.0, 5.3), Handedness.Right);
      rightModifiers.Add(Keys.RShiftKey, Sides.Right, KeySize.RightShift);
      rightModifiers.Add(Keys.RControlKey, Sides.Right | Sides.Bottom, KeySize.Ctrl);
      rightModifiers.Add(Keys.Apps, Sides.Bottom, KeySize.ContextMenu);
      rightModifiers.Add(Keys.RMenu, Sides.Bottom, KeySize.RightAlt);
      return rightModifiers;
    }


    KeyboardSection GetPunctuationKeys()
    {
      KeyboardSection punctuationKeys = new KeyboardSection(new Point(29.2, 11.0), new Size(9.2, 7.9), Handedness.Right);
      punctuationKeys.Add(Keys.OemPeriod);
      punctuationKeys.Add(Keys.OemQuestion);
      punctuationKeys.Add(Keys.OemMinus, Sides.Top);
      punctuationKeys.Add(Keys.Oemplus, Sides.Top);
      punctuationKeys.Add(Keys.Oemcomma);
      punctuationKeys.Add(Keys.OemOpenBrackets);
      punctuationKeys.Add(Keys.Oem7);
      punctuationKeys.Add(Keys.Oem6);
      punctuationKeys.Add(Keys.Oem5, Sides.Right);  // backslash key
      punctuationKeys.Add(Keys.Oem1);
      punctuationKeys.Add(Keys.Back, Sides.Top | Sides.Right, KeySize.Backspace);
      punctuationKeys.Add(Keys.Enter, Sides.Right, KeySize.Enter);
      return punctuationKeys;
    }

    KeyboardSection GetNumPadTopRowOperators()
    {
      KeyboardSection punctuationKeys = new KeyboardSection(new Point(37.4, 5.9), new Size(5.8, 1.9), Handedness.Right);
      punctuationKeys.Add(Keys.PrintScreen, Sides.Left | Sides.Top | Sides.Bottom);
      punctuationKeys.Add(Keys.Scroll, Sides.Top | Sides.Bottom);
      punctuationKeys.Add(Keys.Pause, Sides.Right | Sides.Top | Sides.Bottom);
      return punctuationKeys;
    }

    KeyboardSection GetWebSearchMailKeys()
    {
      KeyboardSection webSearchMailKeys = new KeyboardSection(new Point(6.1, 2.2), new Size(6.5, 1.0), Handedness.Left);
      webSearchMailKeys.Add(Keys.BrowserHome, Sides.Left | Sides.Top | Sides.Bottom);
      webSearchMailKeys.Add(Keys.BrowserSearch, Sides.Top | Sides.Bottom);
      webSearchMailKeys.Add(Keys.LaunchMail, Sides.Right | Sides.Top | Sides.Bottom);
      return webSearchMailKeys;
    }
    /// <summary>
    /// Initializes a new instance of the MicrosoftNaturalKeyboard class.
    /// </summary>
    public MicrosoftNaturalKeyboard()
    {
      KeyboardSection.StandardKeySize = KeySize.StandardKey;

      homeKeysLeft = GetHomeKeysLeft();
      homeKeysRight = GetHomeKeysRight();
      punctuationKeysRight = GetPunctuationKeys();
      extendedKeys = GetExtendedNavKeys();
      functionKeysRight = GetFunctionKeysRight();
      numPadKeys = GetNumPadKeys();
      numPadTopRowOperators = GetNumPadTopRowOperators();
      arrowKeys = GetArrowKeys();
      
      keyboardSections.Add(arrowKeys);
      keyboardSections.Add(GetFunctionKeysLeft());
      keyboardSections.Add(functionKeysRight);
      keyboardSections.Add(homeKeysLeft);
      keyboardSections.Add(homeKeysRight);
      keyboardSections.Add(GetNumKeysLeft());
      keyboardSections.Add(GetNumKeysRight());
      keyboardSections.Add(extendedKeys);
      keyboardSections.Add(numPadKeys);
      keyboardSections.Add(numPadTopRowOperators);
      keyboardSections.Add(GetSpaceBar());
      keyboardSections.Add(GetEscape());
      keyboardSections.Add(GetLeftKeyColumn());
      keyboardSections.Add(GetLeftModifiers());
      keyboardSections.Add(GetRightModifiers());
      keyboardSections.Add(GetWebSearchMailKeys());
      keyboardSections.Add(punctuationKeysRight);
    }

    void CalculateMousePadCenterPoint()
    {
      Size keyboardSize = GetKeyboardSize();
      mousePadCenter = new Point(MousePad.Width / 2.0, MousePad.Height / 2.0);
      switch (MousePadPosition)
      {
        case Handedness.Left:
          mousePadCenter.Offset(-MousePad.Width, 0);
          break;
        case Handedness.Right:
          mousePadCenter.Offset(keyboardSize.Width, 0);
          break;
        case Handedness.Both:
          double betweenHomeKeys = (homeKeysLeft.Center.X + homeKeysRight.Center.X) / 2.0;
          mousePadCenter.Offset(betweenHomeKeys, keyboardSize.Height);
          break;
      }
    }
    KeyboardSection GetLastTouchedSectionClosestToMouse()
    {
      KeyboardSection nearestSection = null;
      switch (MousePadPosition)
      {
        case Handedness.Left:
          if (lastLeftKeyPressed != null)
            nearestSection = lastLeftKeyPressed.ParentSection;
          else
            nearestSection = homeKeysLeft;
          break;
        case Handedness.Right:
          if (lastRightKeyPressed != null)
            nearestSection = lastRightKeyPressed.ParentSection;
          else
            nearestSection = homeKeysRight;
          break;
        case Handedness.Both:
          if (lastAmbidextrousKeyPressed != null)
            nearestSection = lastAmbidextrousKeyPressed.ParentSection;
          else
            nearestSection = homeKeysRight;
          break;
      }
      return nearestSection;
    }

    public double GetReachingForMouseTransitionCost()
    {
      if (MousePad == null)
        throw new Exception("MousePad not specified.");
      if (mousePadCenter.X == double.MinValue)
        CalculateMousePadCenterPoint();
      if (MousePad.Mouse == null)
        throw new Exception("Mouse not specified.");

      KeyboardSection nearestSection = GetLastTouchedSectionClosestToMouse();
      if (nearestSection == null)
        throw new Exception("A keyboard section near the mouse was not found.");

      double travelDistance = MillerComplexity.GetDistance(nearestSection.Center, mousePadCenter);
      double targetApproachWidth = MousePad.Mouse.Width; // Mouse is nearly always approached from the left or right;
      lastMouseHandedAction = ActionType.Mouse;
      return MillerComplexity.GetTargetPressScore(travelDistance, targetApproachWidth);
    }

    PhysicalKey FindKey(string keyName)
    {
      PhysicalKey key;

      if (keyName.Length == 1 && char.IsDigit(keyName, 0))
        keyName = "D" + keyName;

      const string Key_Enter = "Enter";

      if (keyName == Key_Enter && lastRightKeyPressed != null)
      {
        // The Enter key appears twice on the MS Natural keyboard. Find it in the right section based on the closest section of the last key hit.
        KeyboardSection parentSection = lastRightKeyPressed.ParentSection;

        if (parentSection == numPadKeys || parentSection == numPadTopRowOperators)
          key = numPadKeys.FindKey(Key_Enter);
        else
          key = punctuationKeysRight.FindKey(Key_Enter);
        if (key != null)
          return key;
      }

      foreach (KeyboardSection keyboardSection in keyboardSections)
      {
        key = keyboardSection.FindKey(keyName);
        if (key != null)
          return key;
      }

      return null;
    }

    void SaveLastKeyPressed(PhysicalKey key)
    {
      Handedness handedness = key.ParentSection.Handedness;
      switch (handedness)
      {
        case Handedness.Left:
          lastLeftKeyPressed = key;
          timeOfLastLeftKeyPress = DateTime.Now;
          if (timeOfLastRightKeyPress > timeOfLastAmbidextrousKeyPressed)   // Both right-handed and left-handed keys have been pressed more recently than the last ambidextrous key was hit.
            lastAmbidextrousKeyPressed = null;
          break;
        case Handedness.Right:
          lastRightKeyPressed = key;
          timeOfLastRightKeyPress = DateTime.Now;
          if (timeOfLastLeftKeyPress > timeOfLastAmbidextrousKeyPressed)   // Both right-handed and left-handed keys have been pressed more recently than the last ambidextrous key was hit.
            lastAmbidextrousKeyPressed = null;
          break;
        case Handedness.Both:
          lastAmbidextrousKeyPressed = key;
          timeOfLastAmbidextrousKeyPressed = DateTime.Now;
          break;
      }
    }

    double GetDistanceBetweenPoints(Point pt1, Point pt2)
    {
      double deltaX = pt2.X - pt1.X;
      double deltaY = pt2.Y - pt1.Y;
      return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }


    /// <summary>
    /// Returns the distance between the center of the specified section to the center of the section
    /// holding the specified key.
    /// </summary>
    double GetApproximateDistanceToKey(KeyboardSection section, PhysicalKey key1)
    {
      double distanceBetweenPoints = GetDistanceBetweenPoints(key1.ParentSection.Center, section.Center);
      return Math.Max(distanceBetweenPoints, DBL_MinimumDistanceBetweenKeys);
    }

    /// <summary>
    /// Determines the approximate travel distance between keys by measuring the travel distance 
    /// between the parenting sections of the two keys.
    /// </summary>
    double GetApproximateDistanceBetweenKeys(PhysicalKey key1, PhysicalKey key2)
    {
      KeyboardSection section = key2.ParentSection;
      return GetApproximateDistanceToKey(section, key1);
    }

    bool KeyIsARepeat(PhysicalKey key)
    {
      return key == lastLeftKeyPressed || key == lastRightKeyPressed || key == lastAmbidextrousKeyPressed;
    }

    
    double GetDistanceToLastLeftHandedKey(PhysicalKey key)
    {
      if (lastLeftKeyPressed == null)
        return GetApproximateDistanceToKey(homeKeysLeft, key);
      else
        return GetApproximateDistanceBetweenKeys(key, lastLeftKeyPressed);
    }
    
    double GetDistanceToLastRightHandedKey(PhysicalKey key)
    {
      if (lastRightKeyPressed == null)
        return GetApproximateDistanceToKey(homeKeysRight, key);
      else
        return GetApproximateDistanceBetweenKeys(key, lastRightKeyPressed);
    }
    
    double GetDistanceFromLastAmbidextrousKey(PhysicalKey key)
    {
      if (lastAmbidextrousKeyPressed == null)
        return double.MaxValue;
      else
        return GetApproximateDistanceBetweenKeys(key, lastAmbidextrousKeyPressed);
    }

    double GetShortestDistanceToKey(PhysicalKey key)
    {
      double distanceToLastSameHandedKey = double.MaxValue;
      double distanceToAmbidextrousKey = GetDistanceFromLastAmbidextrousKey(key);

      // Find the closest section (holding a previously-hit key) to the specified key...
      if (key.ParentSection.Handedness == Handedness.Left)
        distanceToLastSameHandedKey = GetDistanceToLastLeftHandedKey(key);
      else if (key.ParentSection.Handedness == Handedness.Right)
        distanceToLastSameHandedKey = GetDistanceToLastRightHandedKey(key);
      else if (key.ParentSection.Handedness == Handedness.Both)
      {
        double distanceFromLastLeftHandedKey = GetDistanceToLastLeftHandedKey(key);
        double distanceFromLastRightHandedKey = GetDistanceToLastRightHandedKey(key);
        
        double distanceFromNearestHand = Math.Min(distanceToAmbidextrousKey, Math.Min(distanceFromLastLeftHandedKey, distanceFromLastRightHandedKey));

        if (distanceFromNearestHand < double.MaxValue)
          return distanceFromNearestHand;
      }

      double smallestDistance;
      if (distanceToLastSameHandedKey == double.MaxValue && distanceToAmbidextrousKey == double.MaxValue)
        smallestDistance = DBL_MinimumDistanceBetweenKeys;
      else
        smallestDistance = Math.Min(distanceToLastSameHandedKey, distanceToAmbidextrousKey);
      return smallestDistance;
    }

    bool SpaceBarHitWhileHandsAtHomeRow(PhysicalKey key)
    {
      return key.Name == "Space" && (lastLeftKeyPressed == null || lastRightKeyPressed == null || lastLeftKeyPressed.ParentSection == homeKeysLeft || lastRightKeyPressed.ParentSection == homeKeysRight);
    }

    double CalculateCost(PhysicalKey key)
    {
      if (KeyIsARepeat(key))
        return MillerComplexity.GetCostOfRepeatHit(key.EffectiveTargetApproachWidth);

      const double DBL_AverageDistanceToSpaceBarWhileHandsNearHomeKeys = 1.4;   /* cm */
      if (SpaceBarHitWhileHandsAtHomeRow(key))
        return MillerComplexity.GetTargetPressScore(DBL_AverageDistanceToSpaceBarWhileHandsNearHomeKeys, key.EffectiveTargetApproachWidth);    // Space bar hit and at least one hand is near the home key row position.

      return MillerComplexity.GetTargetPressScore(GetShortestDistanceToKey(key), key.EffectiveTargetApproachWidth);
    }

    bool KeyHasSameHandednessAsMouse(PhysicalKey key)
    {
      return key.ParentSection.Handedness == MousePadPosition;
    }

    public virtual double GetKeyCost(string keyName)
    {
      PhysicalKey key = FindKey(keyName);
      double cost = 0;
      if (key != null)
      {
        if (KeyHasSameHandednessAsMouse(key))
          lastMouseHandedAction = ActionType.Keyboard;
        cost = CalculateCost(key);
        SaveLastKeyPressed(key);
      }
      else
      {
        //throw new Exception("key not found: " + keyName);
      }
      return cost;
    }

    public ActionType LastMouseHandedAction
    {
      get
      {
        return lastMouseHandedAction;
      }
      set
      {
      	lastMouseHandedAction = value;
      }
    }

    public MousePad MousePad { get; set; }
    public Handedness MousePadPosition { get; set; }
  }
}
