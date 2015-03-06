using System;
using System.Timers;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;

namespace StepDiagrammer
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    Stopwatch stopwatch;
    Timer timer = new Timer();
      
    public MainWindow()
    {
      InitializeComponent();
      zoomLevel.Value = 10.0;
      stepDiagramViewer.EventsDeleted += stepDiagramViewer_EventsDeleted;
    }

    void stepDiagramViewer_EventsDeleted(object sender, EventArgs e)
    {
      StepDiagram events = Session.StepDiagram;
      if (events != null)
        events.ClearTotals();
      UpdateTotals();
    }

    private void btnStart_Click(object sender, RoutedEventArgs e)
    {
      stepDiagramViewer.Clear();
      
      btnStart.IsEnabled = false;
      Session.Clean();
      Session.SetStepDiagram(null);
      UpdateTotals();
      HookEngine.Start();
      HookEngine.UsageChanged += HookEngine_UsageChanged;

      stepDiagramViewer.Start();
      HookEngine.StepDiagram.Events.CollectionChanged += Events_CollectionChanged;

      btnStop.IsEnabled = true;

      TextBox txtDiagramName = VisualTree.FindChild<TextBox>(this, "txtDiagramName");
      if (txtDiagramName != null)
        txtDiagramName.Visibility = System.Windows.Visibility.Hidden;
      lblClock.Text = "0:00.00";
      timer.Interval = 5;
      timer.Elapsed += timer_Elapsed;
      timer.Start();
      stopwatch = new Stopwatch();
    }

    void Events_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      HookEngine.StepDiagram.Events.CollectionChanged -= Events_CollectionChanged;
      stopwatch.Start();
    }

    void timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      TimeSpan elapsed = stopwatch.Elapsed;
      string newClockTime = String.Format("{0}:{1:00.}.{2:00.}", (int)elapsed.TotalMinutes, elapsed.Seconds, (int)Math.Round(elapsed.Milliseconds / 10.0));
      App.Current.Dispatcher.Invoke((Action)(() => lblClock.Text = newClockTime));
    }

    void ResetDiagramNameBox()
    {
      TextBox txtDiagramName = VisualTree.FindChild<TextBox>(this, "txtDiagramName");
      if (txtDiagramName != null)
      {
        txtDiagramName.Text = string.Empty;
        txtDiagramName.Visibility = System.Windows.Visibility.Visible;
      }
    }
    private void btnStop_Click(object sender, RoutedEventArgs e)
    {
      HookEngine.StepDiagram.Events.CollectionChanged -= Events_CollectionChanged;
      timer.Stop();
      timer.Elapsed -= timer_Elapsed;
      stopwatch = null;
      btnStop.IsEnabled = false;
      
      stepDiagramViewer.Stopping();
      HookEngine.Stop();
      HookEngine.UsageChanged -= HookEngine_UsageChanged;
      stepDiagramViewer.Stop();

      btnStart.IsEnabled = true;
      UpdateTotals();

      ResetDiagramNameBox();
    }

    private void txtDiagramName_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox txtDiagramName = sender as TextBox;
      if (txtDiagramName == null)
        return;

      if (Session.StepDiagram == null)
        return;

      Session.StepDiagram.Name = txtDiagramName.Text;
    }

    void HookEngine_UsageChanged(object sender, UsageChangedEventArgs e)
    {
      Title = String.Format("Step Diagrammer (usage = {0}%)", e.NewUsage);
    }

    void UpdateTotals()
    {
      StepDiagram stepDiagram = Session.StepDiagram;
      if (stepDiagram == null)
      {
        lblKeyPresses.Text = "0";
        lblMouseWheelSpins.Text = "0";
        lblMouseClicks.Text = "0 clicks";
        lblMouseMoveDistance.Text = "0px";
        //lblMouseSpeed.Text = "0px/s";
        lblTimeSpentMoving.Text = "0s";
        lblForceTime.Text = "0N-s";
        lblMillerComplexityScore.Text = "0";
      }
      else
      {
        lblKeyPresses.Text = String.Format("{0:#,0.} keys", stepDiagram.TotalKeyPresses);
        lblMouseWheelSpins.Text = String.Format("{0:#,0.} detents", stepDiagram.TotalMouseWheels);
        lblMouseClicks.Text = String.Format("{0:#,0.} clicks", stepDiagram.TotalMouseClicks);
        lblMouseMoveDistance.Text = String.Format("{0:#,0.#}px", stepDiagram.TotalMouseDistanceTravelled);
        lblTimeSpentMoving.Text = String.Format("{0:s\\.ff}s", stepDiagram.TotalTimeSpentMoving);
        lblForceTime.Text = String.Format("{0:#,0.##}NS", stepDiagram.TotalForceTime);
        lblMillerComplexityScore.Text = String.Format("{0:#,0.##} mci", stepDiagram.TotalMillerComplexityScore);
        /*lblMouseSpeed*/
        this.Title = String.Format("Step Diagrammer - Average Mouse Speed: {0:#,0.##}px/s", stepDiagram.AverageMouseSpeed);
      }
    }

    private void zoomLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (stepDiagramViewer == null)
        return;

      double newValue = e.NewValue;
      if (newValue > 10)
        newValue -= 9;
      else
        newValue = 1 / (11 - newValue);

      if (ConversionHelper.ZoomLevel == newValue)
        return;

      stepDiagramViewer.SetNewZoomLevel(newValue);
    }

    private void btnStretch_Click(object sender, RoutedEventArgs e)
    {
      Point buttonPos = btnStretch.PointToScreen(new Point(0, 0));
      Rect screenBounds = ScreenCapture.GetScreenBoundsAt((int)buttonPos.X, (int)buttonPos.Y);
      Left = screenBounds.Left;
      Width = screenBounds.Width;
    }
  }
}
