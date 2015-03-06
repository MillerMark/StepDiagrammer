using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace StepDiagrammer
{
  public static class Commands
  {
    public static readonly RoutedUICommand DeleteEvent = new RoutedUICommand("Delete this event", "DeleteEvent", typeof(StepDiagramViewer));
    public static readonly RoutedUICommand MakeEventFirst = new RoutedUICommand("Make this event first (delete left)", "MakeEventFirst", typeof(StepDiagramViewer));
    public static readonly RoutedUICommand MakeEventLast = new RoutedUICommand("Make this event last (delete right)", "MakeEventLast", typeof(StepDiagramViewer));
  }
}
