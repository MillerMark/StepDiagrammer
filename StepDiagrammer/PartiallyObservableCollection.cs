using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace StepDiagrammer
{

  public class PartiallyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
  {
    bool suspendNotification;
    public PartiallyObservableCollection(): base()
    {
      CollectionChanged += TrulyObservableCollection_CollectionChanged;
      suspendNotification = false;
    }

    void TrulyObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.NewItems != null)
        foreach (INotifyPropertyChanged item in e.NewItems)
          item.PropertyChanged += item_PropertyChanged;

      if (e.OldItems != null)
        foreach (INotifyPropertyChanged item in e.OldItems)
          item.PropertyChanged -= item_PropertyChanged;
    }

    void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (suspendNotification)
        return;
      NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      OnCollectionChanged(a);
      stopwatch.Stop();
      if (stopwatch.ElapsedMilliseconds > 15)   // If performance drops, suspend notification.
        suspendNotification = true;
    }
  }
}
