using System;

namespace StepDiagrammer
{
  public static class EventFactory
  {
    public static Event Create(EventType eventType)
    {
      if (eventType == EventType.MouseDown)
        return new MouseDownEvent();
      if (eventType == EventType.MouseMove)
        return new MouseMoveEvent();
      if (eventType == EventType.MouseWheel)
        return new MouseWheelEvent();
      if (eventType == EventType.KeyDown)
        return new KeyDownEvent();
      
      // TODO: Add support for registered event factories.
      throw new Exception("Event type not supported."); 
    }
  }
}
