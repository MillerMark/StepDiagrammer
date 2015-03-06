using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace StepDiagrammer
{
  public class VisualTree
  {
    public static List<T> GetVisualChildCollection<T>(object parent) where T : Visual
    {
      List<T> visualCollection = new List<T>();
      GetVisualChildCollection(parent as DependencyObject, visualCollection);
      return visualCollection;
    }

    private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
    {
      int count = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < count; i++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(parent, i);
        if (child is T)
          visualCollection.Add(child as T);
        else if (child != null)
          GetVisualChildCollection(child, visualCollection);
      }
    }

    public static T GetParent<T>(FrameworkElement element) where T: FrameworkElement
    {
      if (element == null)
        return null;
      FrameworkElement testElement = element.Parent as FrameworkElement;
      while (testElement != null)
      {
        T result = testElement as T;
        if (result != null)
          return result;
        testElement = testElement.Parent as FrameworkElement;
      }
      return null;
    }

    public static T FindChild<T>(DependencyObject parent, string childName)
       where T : DependencyObject
    {
      if (parent == null || string.IsNullOrEmpty(childName))
        return null;

      T foundChild = null;

      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        T childType = child as T;
        if (childType == null)
        {
          foundChild = FindChild<T>(child, childName);    // recursively drill down

          if (foundChild != null)
            return foundChild;
        }
        else
        {
          var frameworkElement = child as FrameworkElement;
          if (frameworkElement != null && frameworkElement.Name == childName)
            return (T)child;
        }
      }

      return null;
    }
  }
}
