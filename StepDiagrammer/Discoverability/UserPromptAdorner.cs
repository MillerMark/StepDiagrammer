using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StepDiagrammer
{
  internal class UserPromptAdorner : Adorner
  {
    #region private fields...
    readonly ContentPresenter userPromptPresenter;
    #endregion

    // constructors...
    #region UserPromptAdorner
    public UserPromptAdorner(UIElement adornedElement, object userPrompt)
      : base(adornedElement)
    {
      IsHitTestVisible = false;

      Thickness padding = AdornedControl.Padding;
      Thickness newMargin = new Thickness(padding.Left + 2, padding.Top + 1, padding.Right, padding.Bottom);
      
      userPromptPresenter = new ContentPresenter() { Content = userPrompt, Opacity = 0.5, Margin = newMargin };

      if (AdornedControl is ItemsControl && !(AdornedControl is ComboBox))
      {
        userPromptPresenter.VerticalAlignment = VerticalAlignment.Center;
        userPromptPresenter.HorizontalAlignment = HorizontalAlignment.Center;
      }

      Binding binding = new Binding("IsVisible") { Source = adornedElement, Converter = new BooleanToVisibilityConverter() };
      SetBinding(VisibilityProperty, binding);
    }
    #endregion

    // private properties...
    #region AdornedControl
    private Control AdornedControl
    {
      get
      {
        return (Control)AdornedElement;
      }
    }
    #endregion

    // protected property overrides...
    #region VisualChildrenCount
    protected override int VisualChildrenCount
    {
      get
      {
        return 1;   // Always one child.
      }
    }
    #endregion

    // protected method overrides...
    #region ArrangeOverride
    protected override Size ArrangeOverride(Size finalSize)
    {
      userPromptPresenter.Arrange(new Rect(finalSize));
      return finalSize;
    }
    #endregion
    #region GetVisualChild
    protected override Visual GetVisualChild(int index)
    {
      return userPromptPresenter;    // Always return the userPromptPresenter.
    }
    #endregion
    #region MeasureOverride
    protected override Size MeasureOverride(Size constraint)
    {
      // Cover the entire adorned control:
      userPromptPresenter.Measure(AdornedControl.RenderSize);
      return AdornedControl.RenderSize;
    }
    #endregion
  }
}
