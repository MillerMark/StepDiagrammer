using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StepDiagrammer
{
	/// <summary>
	/// Interaction logic for StepDiagramViewer.xaml
	/// </summary>
	public partial class StepDiagramViewer : UserControl
	{
		int brushIndex = 0;
		List<Brush> taskBackgroundBrushes = new List<Brush>();
		Dictionary<IntPtr, int> taskBrushIdCache = new Dictionary<IntPtr, int>();
		SolidColorBrush taskForegroundBrush;
		public event EventHandler EventsDeleted;
		TimeSpan lastTimePopulatedWithTickmarks;
		int lastTimeTickmarkDrawn;
		private Canvas canvas;
		int nextTaskIndexToAdd;
		double spaceWidth = 0;
		double tickMarkFontSize;
		double activeWindowFontSize;
		double zoomLevel = 1.0;

		protected virtual void OnEventsDeleted()
		{
			EventHandler handler = EventsDeleted;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		void InitializeBrushes()
		{
			AddTaskBackground(Color.FromRgb(0xBA, 0xCF, 0xEB));
			AddTaskBackground(Color.FromRgb(0xE0, 0xFF, 0xE3));
			AddTaskBackground(Color.FromRgb(0xF0, 0xCC, 0xE6));
			AddTaskBackground(Color.FromRgb(0xE0, 0xE2, 0xFF));
			AddTaskBackground(Color.FromRgb(0xCF, 0xC4, 0xEB));
			AddTaskBackground(Color.FromRgb(0xFC, 0xE0, 0xFF));
			AddTaskBackground(Color.FromRgb(0xC7, 0xF1, 0xDE));
			AddTaskBackground(Color.FromRgb(0xFF, 0xF9, 0xE0));
			AddTaskBackground(Color.FromRgb(0xE9, 0xD8, 0xC2));

			taskForegroundBrush = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
		}

		private void AddTaskBackground(Color color)
		{
			taskBackgroundBrushes.Add(new SolidColorBrush(color));
		}

		public StepDiagramViewer()
		{
			InitializeComponent();
			lastTimeTickmarkDrawn = -1;
			nextTaskIndexToAdd = 0;
			brushIndex = 0;
			taskBrushIdCache.Clear();
			Event.LabelTypeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
			Event.LabelFontSize = FontSize;
			tickMarkFontSize = FontSize * 0.8;
			activeWindowFontSize = FontSize * 0.8;
			SetAvailableHeight();
			ConversionHelper.ZoomLevelChanged += ConversionHelper_ZoomLevelChanged;
			InitializeBrushes();
		}

		void ConversionHelper_ZoomLevelChanged(object sender, EventArgs e)
		{
			RefreshDataView();
		}

		public void Start()
		{
			lstSteps.ItemsSource = CollectedEvents;

			CollectedEvents.CollectionChanged += CollectedEvents_CollectionChanged;

			Session.SetStepDiagram(HookEngine.StepDiagram);
			INotifyPropertyChanged propertyNotifyChanged = HookEngine.StepDiagram as INotifyPropertyChanged;
			propertyNotifyChanged.PropertyChanged += HookEnginePropertyChanged;
			SetAvailableHeight();
		}

		void HookEnginePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "MaxForce")
				MaxForceChanged();
		}

		public void MaxForceChanged()
		{
			AddForceTicksToCanvas();
		}

		private void lstSteps_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SetAvailableHeight();
			RefreshDataView();
		}

		double GetHorizontalAxisHeight()
		{
			return 64;
		}

		public void SetAvailableHeight()
		{
			ConversionHelper.SetAvailableHeight(lstSteps.ActualHeight - SystemParameters.ScrollHeight - GetHorizontalAxisHeight());
			AddForceTicksToCanvas();
		}

		private Size MeasureString(string text)
		{
			return FontHelper.MeasureString(text, Event.LabelTypeface, tickMarkFontSize);
		}

		static Line GetForceTickLine(Canvas canvas, double x, double y)
		{
			Line newLine = new Line();
			newLine.X1 = x;
			newLine.X2 = canvas.ActualWidth;
			newLine.Y1 = y;
			newLine.Y2 = newLine.Y1;
			newLine.Stroke = Brushes.LightGray;
			return newLine;
		}

		TextBlock GetForceTickLabel(double force, double rightSide, double y)
		{
			TextBlock label = new TextBlock();
			string tickLabel = force.ToString();  // + "N"
			label.Text = tickLabel;
			label.Foreground = Brushes.DarkGray;
			label.FontSize = tickMarkFontSize;
			Size labelSize = MeasureString(tickLabel);
			Canvas.SetLeft(label, rightSide - labelSize.Width - SpaceWidth);
			Canvas.SetTop(label, y - labelSize.Height / 2);
			return label;
		}

		void AddTimeTickMarkLine(double x, double tickLength, double strokeThickness)
		{
			Line tickMark = new Line();
			tickMark.X1 = x;
			tickMark.Y1 = 0;
			tickMark.X2 = x;
			tickMark.Y2 = tickLength;
			tickMark.StrokeThickness = strokeThickness;
			tickMark.Stroke = Brushes.LightGray;
			cvsSecondsTicks.Children.Add(tickMark);
		}

		void AddTimeTickMarkLabel(double x, double longTickLength, double seconds, double tickFontSize)
		{
			TextBlock label = new TextBlock();
			double secondsTimeTen = (int)Math.Round(seconds * 10);
			double normalizedSeconds = secondsTimeTen / 10.0;  // To get around crazy math error in the system.
			string tickLabel = String.Format("{0}", normalizedSeconds);
			label.Text = tickLabel;
			label.Foreground = Brushes.DarkGray;
			label.FontSize = tickFontSize;
			Size labelSize = MeasureString(tickLabel);
			cvsSecondsTicks.Children.Add(label);
			Canvas.SetLeft(label, x - labelSize.Width / 2);
			Canvas.SetTop(label, longTickLength);
		}

		void AddTimeTickMark(double seconds)
		{
			if (seconds == 0)
				return;
			double horizontalAxisHeight = GetHorizontalAxisHeight();

			double longTickLength = horizontalAxisHeight / 5.0;
			double mediumTickLength = horizontalAxisHeight / 7.5;
			double shortTickLength = horizontalAxisHeight / 10.0;
			double tickLength = shortTickLength;

			double strokeThickness = 0.5;

			double tickFontSize = tickMarkFontSize;
			string timeAsStr = seconds.ToString("0.0");
			int indexOfDecimal = timeAsStr.IndexOf('.');
			if (indexOfDecimal > 0 && indexOfDecimal < timeAsStr.Length)
			{
				char charAfterDecimal = timeAsStr[indexOfDecimal + 1];
				if (charAfterDecimal == '0')
				{
					tickLength = longTickLength;
					tickFontSize = tickMarkFontSize * 1.2;
					strokeThickness = 1.5;
				}
				else if (charAfterDecimal == '5')
				{
					tickLength = mediumTickLength;
					tickFontSize = tickMarkFontSize * 1.1;
					strokeThickness = 1;
				}
			}

			double x = ConversionHelper.MillisecondsToPixels(seconds * 1000);
			AddTimeTickMarkLine(x, tickLength, strokeThickness);
			AddTimeTickMarkLabel(x, longTickLength, seconds, tickFontSize);
		}

		void AddForceTicksToCanvas()
		{
			Canvas canvas = cvsForceTicks;
			double height = canvas.ActualHeight;
			canvas.Children.Clear();
			double maxForce;
			if (Session.StepDiagram != null)
				maxForce = Session.StepDiagram.MaxForce;
			else
				maxForce = 2.5;

			int numSteps = 5;
			if (height < 80)
				numSteps = (int)Math.Floor(height / 20);

			double rawStep = maxForce / numSteps;
			double tenthPower = 0;
			while (rawStep > 0 && rawStep <= 1.0)
			{
				rawStep = rawStep * 10;
				tenthPower++;
			}
			double step = Math.Round(rawStep) / Math.Pow(10, tenthPower);
			double upperBound = maxForce + step;

			for (double force = 0; force < upperBound; force += step)
			{
				double y = ConversionHelper.AvailableHeight - ConversionHelper.ForceToPixels(force);
				if (y < 0)
					continue;

				double lineLeft = 3 * canvas.ActualWidth / 4;
				Line newLine = GetForceTickLine(canvas, lineLeft, y);
				canvas.Children.Add(newLine);

				TextBlock label = GetForceTickLabel(force, lineLeft, y);
				canvas.Children.Add(label);
			}

			Line rightEdgeLine = new Line();
			rightEdgeLine.X1 = canvas.ActualWidth;
			rightEdgeLine.X2 = canvas.ActualWidth;
			rightEdgeLine.Y1 = 0;
			rightEdgeLine.Y2 = ConversionHelper.AvailableHeight;
			rightEdgeLine.StrokeThickness = 1;
			rightEdgeLine.Stroke = Brushes.LightGray;
			canvas.Children.Add(rightEdgeLine);
		}

		private void cvsForceTicks_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			AddForceTicksToCanvas();
		}

		public double SpaceWidth
		{
			get
			{
				if (spaceWidth == 0)
					spaceWidth = MeasureString("M M").Width - MeasureString("MM").Width;
				return spaceWidth;
			}
		}

		void ClearWindowActivations()
		{
			if (cvsWindows != null)
				cvsWindows.Children.Clear();
			nextTaskIndexToAdd = 0;
			brushIndex = 0;
			taskBrushIdCache.Clear();
		}
		public void Clear()
		{
			if (cvsSecondsTicks != null)
				cvsSecondsTicks.Children.Clear();
			lastTimeTickmarkDrawn = -1;
			ClearWindowActivations();
			brushIndex = 0;
		}

		private void CalculateCanvasSize()
		{
			if (canvas == null)
				return;
			canvas.Width = ConversionHelper.GetSpanInPixels();
			cvsSecondsTicks.Width = canvas.Width;
			cvsWindows.Width = canvas.Width;
		}

		public void Stopping()
		{
			INotifyPropertyChanged propertyNotifyChanged = HookEngine.StepDiagram as INotifyPropertyChanged;
			propertyNotifyChanged.PropertyChanged -= HookEnginePropertyChanged;

			CollectedEvents.CollectionChanged -= CollectedEvents_CollectionChanged;
		}

		public void Stop()
		{
			StepDiagram stepDiagram = Session.StepDiagram;
			if (stepDiagram != null)
			{
				stepDiagram.TightFit();
				IList<TaskActive> tasks = stepDiagram.Tasks;
				if (tasks != null)
				{
					TaskActive lastTask = tasks.Last<TaskActive>();
					if (lastTask != null)
						lastTask.Stop = stepDiagram.StopTime;
				}
			}
			RefreshDataView();
		}

		void AddHorizontalAdornments()
		{
			AddTimeTickMarks();
			AddWindowActivations();
		}

		void CollectedEvents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			CalculateCanvasSize();
			AddHorizontalAdornments();
		}

		public void SetNewZoomLevel(double newZoomLevel)
		{
			zoomLevel = newZoomLevel;
			object selectedItem = lstSteps.SelectedItem;

			ConversionHelper.ZoomLevel = newZoomLevel;

			CalculateCanvasSize();
			Clear();
			AddHorizontalAdornments();

			lstSteps.SelectedItem = selectedItem;
			if (selectedItem != null)
				lstSteps.ScrollIntoView(selectedItem);
		}

		public void RefreshDataView()
		{
			lstSteps.ItemsSource = null;
			if (Session.StepDiagram != null)
				lstSteps.ItemsSource = Session.StepDiagram.Events;
			ClearWindowActivations();
			AddWindowActivations();
		}

		private ObservableCollection<Event> CollectedEvents
		{
			get
			{
				StepDiagram hookEngine = HookEngine.StepDiagram;
				if (hookEngine == null)
					return null;
				return hookEngine.Events;
			}
		}

		ObservableCollection<Event> GetEvents()
		{
			ObservableCollection<Event> events = CollectedEvents;
			if (events == null)
				if (Session.StepDiagram != null)
					events = Session.StepDiagram.Events;
			return events;
		}

		Event GetLastEvent()
		{
			ObservableCollection<Event> events = GetEvents();
			Event last = null;
			if (events != null && events.Count > 0)
				last = events.Last<Event>();
			return last;
		}

		Brush GetTaskFillBrush(TaskActive taskActive)
		{
			IntPtr handle = taskActive.Handle;
			if (taskBrushIdCache.ContainsKey(handle))
				return taskBackgroundBrushes[taskBrushIdCache[handle]];

			taskBrushIdCache.Add(handle, brushIndex);

			Brush brush = taskBackgroundBrushes[brushIndex];

			brushIndex++;
			if (brushIndex >= taskBackgroundBrushes.Count)
				brushIndex = 0;

			return brush;
		}

		FrameworkElement activeWindowTooltip;

		FrameworkElement GetWindowTooltip()
		{
			if (activeWindowTooltip == null)
				activeWindowTooltip = lstSteps.FindResource("ActiveWindowToolTip") as FrameworkElement;
			return activeWindowTooltip;
		}

		Rect AddActiveWindowBox(TaskActive task, bool isLast)
		{
			Rectangle taskRect = new Rectangle();
			StepDiagram stepDiagram = Session.StepDiagram;
			double left = ConversionHelper.MillisecondsToPixels(stepDiagram.GetOffset(task.Start).TotalMilliseconds);
			Canvas.SetLeft(taskRect, left);
			double width = ConversionHelper.MillisecondsToPixels(task.Duration.TotalMilliseconds);
			if (isLast && HookEngine.Listening)
				width = cvsWindows.ActualWidth;
			taskRect.Width = width;
			taskRect.Height = cvsWindows.ActualHeight;
			Canvas.SetTop(taskRect, 0);

			taskRect.Fill = GetTaskFillBrush(task);
			FrameworkElement windowTooltip = GetWindowTooltip();
			windowTooltip.DataContext = task;
			taskRect.ToolTip = windowTooltip;
			taskRect.ToolTipOpening += taskRect_ToolTipOpening;
			taskRect.Tag = task;
			cvsWindows.Children.Add(taskRect);
			return new Rect(left, 0, width, cvsWindows.ActualHeight);
		}

		void taskRect_ToolTipOpening(object sender, ToolTipEventArgs e)
		{
			Rectangle rectangle = sender as Rectangle;
			if (rectangle == null)
				return;
			FrameworkElement tooltip = rectangle.ToolTip as FrameworkElement;
			if (tooltip == null)
				return;
			tooltip.DataContext = rectangle.Tag;
		}

		void AddActiveWindowLabel(TaskActive task, Rect rect)
		{
			TextBlock label = new TextBlock() { Text = task.WindowName, Foreground = taskForegroundBrush, FontSize = activeWindowFontSize };
			label.IsHitTestVisible = false;
			label.Width = rect.Width;
			label.Height = rect.Height;
			label.TextAlignment = TextAlignment.Center;
			cvsWindows.Children.Add(label);
			Canvas.SetLeft(label, rect.Left);
			Canvas.SetTop(label, 0);
		}

		private void ActiveWindowToolTip_Loaded(object sender, RoutedEventArgs e)
		{
			//FrameworkElement frameworkElement = sender as FrameworkElement;
			//if (frameworkElement == null)
			//  return;
			//
			//frameworkElement.DataContext = Session.StepDiagram.Tasks[1];
		}


		void AddWindowActivations()
		{
			StepDiagram stepDiagram = Session.StepDiagram;
			if (stepDiagram == null)
				return;
			IList<TaskActive> tasks = stepDiagram.Tasks;
			for (int i = nextTaskIndexToAdd; i < tasks.Count; i++)
			{
				TaskActive task = tasks[i];
				bool isLast = i == tasks.Count - 1;
				Rect rect = AddActiveWindowBox(task, isLast);
				AddActiveWindowLabel(task, rect);
			}
			nextTaskIndexToAdd = tasks.Count;
		}

		void AddTimeTickMarks()
		{
			Event last = GetLastEvent();
			if (last == null)
				return;

			TimeSpan mostRecentTimeInCollection = Session.StepDiagram.GetOffset(last.Stop);
			int totalSecondsInView = (int)Math.Ceiling(mostRecentTimeInCollection.TotalSeconds);

			double tickAdvance = 1.0d;

			if (zoomLevel > 1.2)
				tickAdvance = 0.1d;
			else if (zoomLevel > 0.19)
				tickAdvance = 0.5d;

			if (lastTimeTickmarkDrawn == -1)
			{
				Line horizontalAxisLine = new Line();
				horizontalAxisLine.X1 = 0;
				horizontalAxisLine.X2 = 9000;
				horizontalAxisLine.Y1 = 0;
				horizontalAxisLine.Y2 = 0;
				horizontalAxisLine.StrokeThickness = 0.5;
				horizontalAxisLine.Stroke = Brushes.LightGray;
				cvsSecondsTicks.Children.Add(horizontalAxisLine);
			}

			for (double tickMarkInSeconds = lastTimeTickmarkDrawn + 1; tickMarkInSeconds <= totalSecondsInView; tickMarkInSeconds += tickAdvance)
				AddTimeTickMark(tickMarkInSeconds);

			lastTimeTickmarkDrawn = totalSecondsInView;
			lastTimePopulatedWithTickmarks = mostRecentTimeInCollection;
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			MouseMoveAnimator.TooltipLoaded(sender as Grid);
		}

		private void Canvas_Loaded(object sender, RoutedEventArgs e)
		{
			canvas = sender as Canvas;
		}

		ScrollBar GetStepDiagramHorizontalScrollBar()
		{
			List<ScrollBar> scrollBars = VisualTree.GetVisualChildCollection<ScrollBar>(lstSteps);

			ScrollBar horizontalScrollBar = null;
			foreach (ScrollBar scrollBar in scrollBars)
			{
				if (scrollBar.Orientation == Orientation.Horizontal)
				{
					horizontalScrollBar = scrollBar;
					break;
				}
			}
			return horizontalScrollBar;
		}


		private void lstSteps_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ScrollBar horizontalScrollBar = GetStepDiagramHorizontalScrollBar();

			if (horizontalScrollBar != null)
			{
				scrlTicks.ScrollToHorizontalOffset(horizontalScrollBar.Value);
				scrlWindows.ScrollToHorizontalOffset(horizontalScrollBar.Value);
			}
		}

		private void MakeEventFirst_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			int numEventsToDelete = IndexOf(GetSelectedEvent());

			if (numEventsToDelete < 0)
				return;

			for (int i = 0; i < numEventsToDelete; i++)
				RemoveAt(0);

			SelectIndexAfterDelete(0);
		}

		private int EventCount
		{
			get
			{
				StepDiagram stepDiagram = Session.StepDiagram;
				if (stepDiagram == null)
					return 0;

				ObservableCollection<Event> events = stepDiagram.Events;
				if (events == null)
					return 0;
				return events.Count;
			}
		}

		void RemoveAt(int index)
		{
			if (Session.StepDiagram != null && Session.StepDiagram.Events != null)
				Session.StepDiagram.Events.RemoveAt(index);
		}

		private void MakeEventLast_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			int numEventsToDelete = EventCount - IndexOf(GetSelectedEvent()) - 1;

			if (numEventsToDelete < 0)
				return;

			for (int i = 0; i < numEventsToDelete; i++)
				RemoveAt(EventCount - 1);

			SelectIndexAfterDelete(EventCount - 1);
		}

		Event GetSelectedEvent()
		{
			if (lstSteps == null)
				return null;
			object selectedItem = lstSteps.SelectedItem;

			if (selectedItem == null || Session.StepDiagram.Events == null)
				return null;

			return selectedItem as Event;
		}

		static int IndexOf(Event selectedEvent)
		{
			return Session.StepDiagram.Events.IndexOf(selectedEvent);
		}

		void SelectIndexAfterDelete(int index)
		{
			int indexToSelect;
			if (index >= EventCount)
				indexToSelect = EventCount - 1;
			else
				indexToSelect = index;

			Session.StepDiagram.TightFit();
			CalculateCanvasSize();
			RefreshDataView();

			if (indexToSelect >= 0)
				lstSteps.SelectedIndex = indexToSelect;

			OnEventsDeleted();
		}

		void DeleteEvent(Event eventToDelete)
		{
			int previousIndex = IndexOf(eventToDelete);
			Session.StepDiagram.Events.Remove(eventToDelete);
			SelectIndexAfterDelete(previousIndex);
		}

		private void DeleteEvent_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			Event selectedEvent = GetSelectedEvent();
			if (selectedEvent == null)
				return;

			DeleteEvent(selectedEvent);
		}

		private void lstSteps_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			ListBox listBox = sender as ListBox;
			if (listBox == null)
				return;
			object selectedItem = listBox.SelectedItem;
			itmDeleteEvent.IsEnabled = selectedItem != null;
			if (selectedItem == null)
			{
				itmDeleteEventsLeft.IsEnabled = false;
				itmDeleteEventsRight.IsEnabled = false;
			}
			else
			{
				itmDeleteEventsLeft.IsEnabled = listBox.SelectedIndex > 0;
				itmDeleteEventsRight.IsEnabled = listBox.SelectedIndex < listBox.Items.Count - 1;
			}
		}
	}
}
