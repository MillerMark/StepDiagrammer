using System;
using System.Linq;

namespace StepDiagrammer
{
  public static class Session
  {
    private static StepDiagram stepDiagram;

    public static Event GetLastEvent()
    {
      if (stepDiagram == null || stepDiagram.Events == null || stepDiagram.Events.Count == 0)
        return null;
      return stepDiagram.Events.Last<Event>();
    }

    internal static void SetStepDiagram(StepDiagram stepDiagram)
    {
      Session.stepDiagram = stepDiagram;
    }

    public static void Clean()
    {
      if (stepDiagram != null)
        stepDiagram.Clean();
    }

    public static bool IsEmpty
    {
      get
      {
        return StepDiagram == null || StepDiagram.Events == null || StepDiagram.Events.Count == 0;
      }
    }

    public static StepDiagram StepDiagram
    {
      get
      {
        return stepDiagram;
      }
    }
  }
}
