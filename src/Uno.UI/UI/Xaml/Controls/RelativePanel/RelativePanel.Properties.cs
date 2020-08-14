using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class RelativePanel
	{
		#region Panel Alignment relationships

		public static bool GetAlignBottomWithPanel(DependencyObject view)
		{
			return (bool)view.GetValue(AlignBottomWithPanelProperty);
		}

		public static void SetAlignBottomWithPanel(DependencyObject view, bool value)
		{
			view.SetValue(AlignBottomWithPanelProperty, value);
		}

		public static DependencyProperty AlignBottomWithPanelProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignBottomWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static bool GetAlignLeftWithPanel(DependencyObject view)
		{
			return (bool)view.GetValue(AlignLeftWithPanelProperty);
		}

		public static void SetAlignLeftWithPanel(DependencyObject view, bool value)
		{
			view.SetValue(AlignLeftWithPanelProperty, value);
		}

		public static DependencyProperty AlignLeftWithPanelProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignLeftWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static bool GetAlignRightWithPanel(DependencyObject view)
		{
			return (bool)view.GetValue(AlignRightWithPanelProperty);
		}

		public static void SetAlignRightWithPanel(DependencyObject view, bool value)
		{
			view.SetValue(AlignRightWithPanelProperty, value);
		}

		public static DependencyProperty AlignRightWithPanelProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignRightWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static bool GetAlignTopWithPanel(DependencyObject view)
		{
			return (bool)view.GetValue(AlignTopWithPanelProperty);
		}

		public static void SetAlignTopWithPanel(DependencyObject view, bool value)
		{
			view.SetValue(AlignTopWithPanelProperty, value);
		}

		public static DependencyProperty AlignTopWithPanelProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignTopWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static bool GetAlignHorizontalCenterWithPanel(DependencyObject view)
		{
			return (bool)view.GetValue(AlignHorizontalCenterWithPanelProperty);
		}

		public static void SetAlignHorizontalCenterWithPanel(DependencyObject view, bool value)
		{
			view.SetValue(AlignHorizontalCenterWithPanelProperty, value);
		}

		public static DependencyProperty AlignHorizontalCenterWithPanelProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignHorizontalCenterWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static bool GetAlignVerticalCenterWithPanel(DependencyObject view)
		{
			return (bool)view.GetValue(AlignVerticalCenterWithPanelProperty);
		}

		public static void SetAlignVerticalCenterWithPanel(DependencyObject view, bool value)
		{
			view.SetValue(AlignVerticalCenterWithPanelProperty, value);
		}

		public static DependencyProperty AlignVerticalCenterWithPanelProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignVerticalCenterWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));
		#endregion

		#region Sibling Alignment relationships

		public static object GetAlignBottomWith(DependencyObject view)
		{
			return (object)view.GetValue(AlignBottomWithProperty);
		}

		public static void SetAlignBottomWith(DependencyObject view, object value)
		{
			view.SetValue(AlignBottomWithProperty, value);
		}

		public static DependencyProperty AlignBottomWithProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignBottomWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetAlignLeftWith(DependencyObject view)
		{
			return (object)view.GetValue(AlignLeftWithProperty);
		}

		public static void SetAlignLeftWith(DependencyObject view, object value)
		{
			view.SetValue(AlignLeftWithProperty, value);
		}

		public static DependencyProperty AlignLeftWithProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignLeftWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetAlignRightWith(DependencyObject view)
		{
			return (object)view.GetValue(AlignRightWithProperty);
		}

		public static void SetAlignRightWith(DependencyObject view, object value)
		{
			view.SetValue(AlignRightWithProperty, value);
		}

		public static DependencyProperty AlignRightWithProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignRightWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetAlignTopWith(DependencyObject view)
		{
			return (object)view.GetValue(AlignTopWithProperty);
		}

		public static void SetAlignTopWith(DependencyObject view, object value)
		{
			view.SetValue(AlignTopWithProperty, value);
		}

		public static DependencyProperty AlignTopWithProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignTopWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetAlignHorizontalCenterWith(DependencyObject view)
		{
			return (object)view.GetValue(AlignHorizontalCenterWithProperty);
		}

		public static void SetAlignHorizontalCenterWith(DependencyObject view, object value)
		{
			view.SetValue(AlignHorizontalCenterWithProperty, value);
		}

		public static DependencyProperty AlignHorizontalCenterWithProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignHorizontalCenterWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetAlignVerticalCenterWith(DependencyObject view)
		{
			return (object)view.GetValue(AlignVerticalCenterWithProperty);
		}

		public static void SetAlignVerticalCenterWith(DependencyObject view, object value)
		{
			view.SetValue(AlignVerticalCenterWithProperty, value);
		}

		public static DependencyProperty AlignVerticalCenterWithProperty { get ; } =
			DependencyProperty.RegisterAttached("AlignVerticalCenterWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		#endregion

		#region Sibling Positional relationships

		public static object GetAbove(DependencyObject view)
		{
			return (object)view.GetValue(AboveProperty);
		}

		public static void SetAbove(DependencyObject view, object value)
		{
			view.SetValue(AboveProperty, value);
		}

		public static DependencyProperty AboveProperty { get ; } =
			DependencyProperty.RegisterAttached("Above", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetBelow(DependencyObject view)
		{
			return (object)view.GetValue(BelowProperty);
		}

		public static void SetBelow(DependencyObject view, object value)
		{
			view.SetValue(BelowProperty, value);
		}

		public static DependencyProperty BelowProperty { get ; } =
			DependencyProperty.RegisterAttached("Below", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetLeftOf(DependencyObject view)
		{
			return (object)view.GetValue(LeftOfProperty);
		}

		public static void SetLeftOf(DependencyObject view, object value)
		{
			view.SetValue(LeftOfProperty, value);
		}

		public static DependencyProperty LeftOfProperty { get ; } =
			DependencyProperty.RegisterAttached("LeftOf", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		public static object GetRightOf(DependencyObject view)
		{
			return (object)view.GetValue(RightOfProperty);
		}

		public static void SetRightOf(DependencyObject view, object value)
		{
			view.SetValue(RightOfProperty, value);
		}

		public static DependencyProperty RightOfProperty { get ; } =
			DependencyProperty.RegisterAttached("RightOf", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

		#endregion

		private static void OnPositioningChanged(object s)
		{
			var element = s as FrameworkElement;

			if (element == null)
			{
				return;
			}

			element.InvalidateArrange();
		}
	}
}
