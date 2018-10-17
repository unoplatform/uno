using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Automation
{
	public sealed partial class AutomationProperties
	{
		#region Name

		public static void SetName(DependencyObject element, string value)
		{
			element.SetValue(NameProperty, value);
		}

		public static string GetName(DependencyObject element)
		{
			return (string)element.GetValue(NameProperty);
		}

		public static DependencyProperty NameProperty =
			DependencyProperty.RegisterAttached(
				"Name",
				typeof(string),
				typeof(AutomationProperties),
				new PropertyMetadata(string.Empty, OnNamePropertyChanged)
			);


		private static void OnNamePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
		}

		#endregion

		#region AccessibilityView

		public static AccessibilityView GetAccessibilityView(DependencyObject element)
		{
			return (AccessibilityView)element.GetValue(AccessibilityViewProperty);
		}

		public static void SetAccessibilityeView(DependencyObject element, AccessibilityView value)
		{
			element.SetValue(AccessibilityViewProperty, value);
		}

		public static DependencyProperty AccessibilityViewProperty { get; } =
			DependencyProperty.RegisterAttached(
				"AccessibilityView",
				typeof(AccessibilityView),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(AccessibilityView.Content)
			);

		#endregion

		#region LabeledBy
		
		public static IUIElement GetLabeledBy(DependencyObject element)
		{
			return (IUIElement)element.GetValue(LabeledByProperty);
		}

		public static void SetLabeledBy(DependencyObject element, IUIElement value)
		{
			element.SetValue(LabeledByProperty, value);
		}

		public static DependencyProperty LabeledByProperty { get; } =
			DependencyProperty.RegisterAttached(
				"LabeledBy", typeof(IUIElement),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(IUIElement))
			);

		#endregion

		#region LocalizedControlType
		
		public static string GetLocalizedControlType(DependencyObject element)
		{
			return (string)element.GetValue(LocalizedControlTypeProperty);
		}

		public static void SetLocalizedControlType(DependencyObject element, string value)
		{
			element.SetValue(LocalizedControlTypeProperty, value);
		}

		public static DependencyProperty LocalizedControlTypeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"LocalizedControlType", typeof(string),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(string))
			);

		#endregion

		#region DescribedBy
		
		public static IList<DependencyObject> GetDescribedBy(DependencyObject element)
		{
			return (IList<DependencyObject>)element.GetValue(DescribedByProperty);
		}

		public static DependencyProperty DescribedByProperty { get; } =
			DependencyProperty.RegisterAttached(
				"DescribedBy", typeof(IList<DependencyObject>),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(IList<DependencyObject>)) // TODO: Empty list?
			);

		#endregion
	}
}
