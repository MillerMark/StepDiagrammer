using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StepDiagrammer
{
  public class StepDiagram : INotifyPropertyChanged
  {
    const int INT_MaxTimeSpanBetweenMouseMoves = 200;   // The maximum number of milliseconds of the pauses between mouse moves for the moves to be considered a single move.
    bool isModified;
    private string name;

    TaskActive lastTaskActive;
    Event lastMouseWheel;
    Event lastMouseMove;

    double maxForce = double.MinValue;
    DateTime startTime;
    DateTime stopTime;
    readonly IList<TaskActive> tasks = new List<TaskActive>();
    readonly PartiallyObservableCollection<Event> events = new PartiallyObservableCollection<Event>();
    readonly Dictionary<int, Event> openKeys = new Dictionary<int, Event>();
    readonly Dictionary<MouseButtons, Event> openMouseButtons = new Dictionary<MouseButtons, Event>();

    // statistics
    int totalKeyPresses = int.MinValue;
    double totalMillerComplexityScore = double.MinValue;
    int totalMouseClicks = int.MinValue;
    int totalMouseWheels = int.MinValue;
    double totalMouseMoves = double.MinValue;
    TimeSpan totalTimeSpentMovingMouse = TimeSpan.Zero;
    double totalForceTime = double.MinValue;
    TimeSpan totalTimeSpentMoving = TimeSpan.MinValue;

    public void Save(string fileName)
    {
      using (StreamWriter writer = new StreamWriter(fileName))
      {
        XmlSerializer serializer = new XmlSerializer(GetType());
        serializer.Serialize(writer, this);
        writer.Flush();
      }
    }

    public static StepDiagram Load(string fileName)
    {
      using (FileStream stream = File.OpenRead(fileName))
      {
        XmlSerializer serializer = new XmlSerializer(typeof(StepDiagram));
        return serializer.Deserialize(stream) as StepDiagram;
      }
    }

    public void ClearTotals()
    {
      totalKeyPresses = int.MinValue;
      totalMouseWheels = int.MinValue;
      totalMouseClicks = int.MinValue;
      totalMouseMoves = double.MinValue;
      totalMillerComplexityScore = double.MinValue;
      totalTimeSpentMovingMouse = TimeSpan.Zero;
      totalTimeSpentMoving = TimeSpan.MinValue;
      totalForceTime = double.MinValue;
    }

    public void Clean()
    {
      foreach (Event evt in events)
        evt.Clear();
      foreach (Event evt in openMouseButtons.Values)
        evt.Clear();
      foreach (Event evt in openKeys.Values)
        evt.Clear();
      openMouseButtons.Clear();
      openKeys.Clear();
      events.Clear();
      tasks.Clear();
      ClearTotals();
    }

    //void ConsolidateAdjacentMouseMoves()
    //{
    //  for (int i = events.Count - 1; i > 0; i--)   // Counting backwards because we're deleting elements...
    //  {
    //    MouseMoveEvent thisEvent = events[i] as MouseMoveEvent;
    //    MouseMoveEvent previousEvent = events[i - 1] as MouseMoveEvent;

    //    if (thisEvent == null || previousEvent == null)
    //      continue;

    //    previousEvent.Stop = thisEvent.Stop;
    //    previousEvent.SetEndPosition(thisEvent.EndPosition);
    //    foreach (TimePoint midPoint in thisEvent.MidPoints)
    //      previousEvent.MidPoints.Add(midPoint);

    //    thisEvent.Clear();

    //    events.RemoveAt(i);
    //  }
    //}

    /// <summary>
    /// Called after all the data is collected.
    /// </summary>
    public void PostProcess()
    {
      //ConsolidateAdjacentMouseMoves();
      foreach (Event evt in events)
        evt.PostProcess();
    }


    public Event AddMouseDown(MouseButtons button, int x, int y)
    {
      Event newMouseDownEvent = Add(EventType.MouseDown);

      if (openMouseButtons.ContainsKey(button))
        openMouseButtons[button] = newMouseDownEvent;
      else
        openMouseButtons.Add(button, newMouseDownEvent);

      MouseDownEvent mouseDownEvent = newMouseDownEvent as MouseDownEvent;
      mouseDownEvent.SetPosition(x, y);

      return newMouseDownEvent;
    }

    Event MouseIsReleased(MouseButtons button)
    {
      Event thisMouseAction = null;

      if (openMouseButtons.ContainsKey(button))
      {
        thisMouseAction = openMouseButtons[button];
        thisMouseAction.EndsNow();
        UpdateMaxForce(thisMouseAction);
        Modified();
        openMouseButtons.Remove(button);
      }
      else
        throw new Exception("Mouse up without a corresponding DOWN???!!!");
      return thisMouseAction;
    }

    public Event AddMouseClick(MouseButtons button, int x, int y)
    {
      MouseDownEvent mouseDownEvent = MouseIsReleased(button) as MouseDownEvent;

      if (mouseDownEvent != null)
      {
        mouseDownEvent.IsClick = true;
        mouseDownEvent.SetPosition(x, y);
        Modified();
        return mouseDownEvent;
      }
      else
        return AddMouseDown(button, x, y);
    }

    public Event AddMouseUp(MouseButtons button)
    {
      return MouseIsReleased(button);
    }

    void CreateMouseMoveEvent(int x, int y)
    {
      lastMouseMove = Add(EventType.MouseMove);

      MouseMoveEvent mouseMoveEvent = lastMouseMove as MouseMoveEvent;
      if (mouseMoveEvent != null)
        mouseMoveEvent.SetStartXY(x, y, openMouseButtons.Count > 0);
    }

    void ModifyLastMouseMoveEvent(int x, int y)
    {
      lastMouseMove.EndsNow();

      MouseMoveEvent mouseMoveEvent = lastMouseMove as MouseMoveEvent;
      if (mouseMoveEvent != null)
        mouseMoveEvent.SetEndXY(x, y, openMouseButtons.Count > 0);

      Modified();
    }

    public Event AddMouseMove(int x, int y)
    {
      if (lastMouseMove != null)
        if (lastMouseMove.Age.TotalMilliseconds < INT_MaxTimeSpanBetweenMouseMoves)
        {
          ModifyLastMouseMoveEvent(x, y);
          return lastMouseMove;
          // No need to add a second mouse move. This existing mouse move can be continued.
        }

      CreateMouseMoveEvent(x, y);

      return lastMouseMove;
    }

    public void AddTaskActive(string windowName, IntPtr handle)
    {
      if (lastTaskActive != null)
        lastTaskActive.Stop = DateTime.Now;

      TaskActive newTaskActive = new TaskActive(windowName, handle);
      //newTaskActive.CaptureActiveWindow(handle);
      lastTaskActive = newTaskActive;
      tasks.Add(newTaskActive);
    }

    public Event AddMouseWheel()
    {
      if (lastMouseWheel != null && lastMouseWheel.Age.TotalMilliseconds < INT_MaxTimeSpanBetweenMouseMoves)
      {
        lastMouseWheel.EndsNow();
        Modified();
        return lastMouseWheel;     // No need to add a second mouse move. This existing mouse move can be continued.
      }

      lastMouseWheel = Add(EventType.MouseWheel);
      return lastMouseWheel;
    }

    KeyDownEvent AddNewKeyDownEvent(int keyValue, Keys keyData, IntPtr handle)
    {
      KeyDownEvent newKeyDownEvent = Add(EventType.KeyDown) as KeyDownEvent;

      KeysConverter kc = new KeysConverter();
      
      
      newKeyDownEvent.Data = kc.ConvertToString(keyData);

      newKeyDownEvent.RepeatCount = 1;
      openKeys.Add(keyValue, newKeyDownEvent);
      newKeyDownEvent.CaptureActiveControl(handle);
      return newKeyDownEvent;
    }

    public Event AddKeyDown(int keyValue, Keys keyData, bool shiftKeyDown, IntPtr handle)
    {
      KeyDownEvent newKeyDownEvent;
      if (openKeys.ContainsKey(keyValue))
      {
        newKeyDownEvent = openKeys[keyValue] as KeyDownEvent;
        newKeyDownEvent.RepeatCount++;
        Modified();
        newKeyDownEvent.EndsNow();
        UpdateMaxForce(newKeyDownEvent);
        newKeyDownEvent.ShiftKeyDown = shiftKeyDown;
        return newKeyDownEvent;
      }
      
      
      
      newKeyDownEvent = AddNewKeyDownEvent(keyValue, keyData, handle);
      newKeyDownEvent.ShiftKeyDown = shiftKeyDown;

      return newKeyDownEvent;
    }

    public Event AddKeyUp(int keyValue)
    {
      Event thisKey = null;

      if (openKeys.ContainsKey(keyValue))
      {
        thisKey = openKeys[keyValue];
        Modified();
        thisKey.EndsNow();
        UpdateMaxForce(thisKey);
        openKeys.Remove(keyValue);
      }
      return thisKey;
    }

    void CropTasksToSpan()
    {
      for (int i = tasks.Count - 1; i >= 0; i--)
      {
        TaskActive task = tasks[i];
        if (task.Start >= stopTime)
          tasks.RemoveAt(i);
        else if (task.Stop >= stopTime)
          task.Stop = stopTime;
        else if (task.Stop < startTime)
          tasks.RemoveAt(i);
        else if (task.Start < startTime)
          task.Start = startTime;
      }
    }

    public void TightFit()
    {
      if (events == null || events.Count == 0)
        return;
      Event first = events.First<Event>();
      if (first != null)
        startTime = first.Start;
      Event last = events.Last<Event>();
      if (last != null)
        stopTime = last.Stop;

      CropTasksToSpan();
    }

    void UpdateMaxForce(Event newEvent)
    {
      if (newEvent.Force > MaxForce)
        MaxForce = newEvent.Force;
    }

    public Event Add(EventType eventType)
    {
      if (events.Count == 0)
        startTime = DateTime.Now;
      Event newEvent = EventFactory.Create(eventType);
      events.Add(newEvent);
      UpdateMaxForce(newEvent);
      if (lastTaskActive != null && lastTaskActive.NeedsScreenGrab())
        lastTaskActive.CaptureActiveWindow();
      Modified();

      return newEvent;
    }

    public TimeSpan GetOffset(DateTime dateTime)
    {
      return dateTime - startTime;
    }

    /// <summary>
    /// Sets the end time for this collection. Call this method after modifying any elements inside the collection.
    /// This method is also called when new events are added (through the call the Add()).
    /// </summary>
    void Modified()
    {
      if (lastTaskActive != null)
      {
        lastTaskActive.Stop = DateTime.Now;
        if (lastTaskActive.NeedsScreenGrab())
          lastTaskActive.CaptureActiveWindow();
      }

      isModified = true;
      stopTime = DateTime.Now;
    }

    /// <summary>
    /// In pixels per second.
    /// </summary>
    public double AverageMouseSpeed
    {
      get
      {
        return TotalMouseDistanceTravelled / TotalTimeSpentMovingMouse.TotalSeconds;
      }
    }

    public string Name
    {
      get
      {
        return name;
      }
      set
      {
        if (name == value)
          return;
        name = value;
        isModified = true;
      }
    }

    public void Save()
    {
      // TODO: Implement this!

      isModified = false;
    }

    public DateTime StopTime
    {
      get
      {
        return stopTime;
      }
    }

    public IList<TaskActive> Tasks
    {
      get
      {
        return tasks;
      }
    }

    public TimeSpan TotalDuration
    {
      get
      {
        return stopTime - startTime;
      }
    }

    public ObservableCollection<Event> Events
    {
      get
      {
        return events;
      }
    }

    /// <summary>
    /// In Newtons
    /// </summary>
    public double MaxForce
    {
      get
      {
        return maxForce;
      }
      set
      {
        if (maxForce == value)
          return;
        maxForce = value;
        PropertyChanged(this, new PropertyChangedEventArgs("MaxForce"));
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Returns the Miller Complexity Score for all events.
    /// </summary>
    public double TotalMillerComplexityScore
    {
      get
      {
        if (totalMillerComplexityScore == double.MinValue)
        {
          Event.ComplexityCalculator.PrepareForAnalysis();
          totalMillerComplexityScore = 0;
          Event previousEvent = null;
          foreach (Event evt in events)
          {
            totalMillerComplexityScore += evt.GetMillerComplexityScore(previousEvent);
            previousEvent = evt;
          }
        }
        return totalMillerComplexityScore;
      }
    }

    /// <summary>
    /// Returns the total number of key presses.
    /// </summary>
    public int TotalKeyPresses
    {
      get
      {
        if (totalKeyPresses == int.MinValue)
        {
          totalKeyPresses = 0;
          foreach (Event evt in events)
          {
            if (evt is KeyDownEvent)
              totalKeyPresses++;
          }
        }
        return totalKeyPresses;
      }
    }

    /// <summary>
    /// Returns the total number of clicks on the mouse.
    /// </summary>
    public int TotalMouseClicks
    {
      get
      {
        if (totalMouseClicks == int.MinValue)
        {
          totalMouseClicks = 0;
          foreach (Event evt in events)
            if (evt is MouseDownEvent)
              totalMouseClicks++;
        }
        return totalMouseClicks;
      }
    }

    /// <summary>
    /// Returns the total distance (in pixels) travelled by the mouseIsReleased.
    /// </summary>
    public double TotalMouseDistanceTravelled
    {
      get
      {
        if (totalMouseMoves == double.MinValue)
        {
          totalMouseMoves = 0;
          foreach (Event evt in events)
          {
            MouseMoveEvent mouseMoveEvent = evt as MouseMoveEvent;
            if (mouseMoveEvent != null)
              totalMouseMoves += mouseMoveEvent.TotalDistanceTravelled;
          }
        }
        return totalMouseMoves;
      }
    }

    /// <summary>
    /// Returns the total amount of time spent moving the mouse.
    /// </summary>
    public TimeSpan TotalTimeSpentMovingMouse
    {
      get
      {
        if (totalTimeSpentMovingMouse == TimeSpan.Zero)
        {
          totalTimeSpentMovingMouse = TimeSpan.Zero;
          foreach (Event evt in events)
          {
            MouseMoveEvent mouseMoveEvent = evt as MouseMoveEvent;
            if (mouseMoveEvent != null)
              totalTimeSpentMovingMouse += mouseMoveEvent.Duration;
          }
        }
        return totalTimeSpentMovingMouse;
      }
    }

    /// <summary>
    /// Returns the total amount of time the user was in motion interacting with input devices.
    /// </summary>
    public TimeSpan TotalTimeSpentMoving
    {
      get
      {
        if (totalTimeSpentMoving == TimeSpan.MinValue)
        {
          totalTimeSpentMoving = TimeSpan.Zero;
          foreach (Event evt in events)
            totalTimeSpentMoving += evt.Duration;
        }
        return totalTimeSpentMoving;
      }
    }

    /// <summary>
    /// Returns the total amount of time the user spent not doing anything at all.
    /// </summary>
    public double TimeSpentMotionless
    {
      get
      {
        return TotalDuration.TotalSeconds - TotalTimeSpentMoving.TotalSeconds;
      }
    }

    /// <summary>
    /// Returns the total force-time in Newton Seconds.
    /// </summary>
    public double TotalForceTime
    {
      get
      {
        if (totalForceTime == double.MinValue)
        {
          totalForceTime = 0.0d;
          foreach (Event evt in events)
            totalForceTime += evt.Duration.TotalSeconds * evt.Force;
        }
        return totalForceTime;
      }
    }

    /// <summary>
    /// Returns the total number of times the mouse wheel was moved.
    /// </summary>
    public int TotalMouseWheels
    {
      get
      {
        if (totalMouseWheels == int.MinValue)
        {
          totalMouseWheels = 0;
          foreach (Event evt in events)
          {
            if (evt is MouseWheelEvent)
              totalMouseWheels++;
          }
        }
        return totalMouseWheels;
      }
    }

  }
}
