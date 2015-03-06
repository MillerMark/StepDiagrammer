using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StepDiagrammer
{
  public static class Discoverability
  {
    // attached property...
    #region UserPrompt
    public static readonly DependencyProperty UserPromptProperty = DependencyProperty.RegisterAttached(
       "UserPrompt",
       typeof(object),
       typeof(Discoverability),
       new FrameworkPropertyMetadata((object)null, OnUserPromptChanged));
    #endregion

    // attached property support methods...
    #region GetUserPrompt
    public static object GetUserPrompt(DependencyObject dependencyObject)
    {
      return (object)dependencyObject.GetValue(UserPromptProperty);
    }
    #endregion
    #region SetUserPrompt
    public static void SetUserPrompt(DependencyObject dependencyObject, object value)
    {
      dependencyObject.SetValue(UserPromptProperty, value);
    }
    #endregion
    #region OnUserPromptChanged
    private static void OnUserPromptChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
      Control control = (Control)dependencyObject;
      control.Loaded += Control_Loaded;

      if (dependencyObject is TextBox)
      {
        control.GotKeyboardFocus += Control_GotKeyboardFocus;
        control.LostKeyboardFocus += Control_Loaded;
      }
    }
    #endregion

    // event handlers...
    #region Control_GotKeyboardFocus
    private static void Control_GotKeyboardFocus(object sender, RoutedEventArgs e)
    {
      Control control = (Control)sender;
      if (ShouldShowUserPrompt(control))
        RemoveUserPrompt(control);
    }
    #endregion
    #region Control_Loaded
    private static void Control_Loaded(object sender, RoutedEventArgs e)
    {
      Control control = (Control)sender;
      if (ShouldShowUserPrompt(control))
        ShowUserPrompt(control);
    }
    #endregion

    // helper methods...
    #region RemoveUserPrompt
    private static void RemoveUserPrompt(UIElement control)
    {
      AdornerLayer layer = AdornerLayer.GetAdornerLayer(control);
      if (layer == null)
        return;

      Adorner[] adorners = layer.GetAdorners(control);
      if (adorners == null)
        return;

      foreach (Adorner adorner in adorners)
        if (adorner is UserPromptAdorner)
        {
          adorner.Visibility = Visibility.Hidden;
          layer.Remove(adorner);
        }
    }
    #endregion
    #region ShowUserPrompt
    private static void ShowUserPrompt(Control control)
    {
      AdornerLayer layer = AdornerLayer.GetAdornerLayer(control);
      if (layer == null)
        return;

      layer.Add(new UserPromptAdorner(control, GetUserPrompt(control)));
    }
    #endregion
    #region ShouldShowUserPrompt
    private static bool ShouldShowUserPrompt(Control control)
    {
      ComboBox comboBox = control as ComboBox;
      if (comboBox != null)
        return comboBox.Text == string.Empty;

      TextBox textBox = control as TextBox;
      if (textBox != null)
        return textBox.Text == string.Empty;

      ItemsControl itemsControl = control as ItemsControl;
      if (itemsControl != null)
        return itemsControl.Items.Count == 0;

      return false;
    }
    #endregion
  }
}
