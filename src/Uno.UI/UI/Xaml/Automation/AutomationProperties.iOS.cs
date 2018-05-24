using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.UI.Xaml.Automation
{
    partial class AutomationProperties
    {
		static partial void OnNamePropertyChangedPartial(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = dependencyObject as UIView;
			if (view != null)
			{
				view.AccessibilityLabel = (string)args.NewValue;
				view.IsAccessibilityElement = true;
			}
		}
	}
}
