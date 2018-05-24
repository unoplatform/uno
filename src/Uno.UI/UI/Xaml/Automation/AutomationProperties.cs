using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Automation
{
    public sealed partial class AutomationProperties
    {
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
			if (AutomationConfiguration.IsAccessibilityEnabled)
			{
				OnNamePropertyChangedPartial(dependencyObject, args);
			}
		}

	    static partial void OnNamePropertyChangedPartial(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args);
    }
}
