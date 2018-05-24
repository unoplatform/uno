using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;

namespace Windows.UI.Xaml.Automation
{
    partial class AutomationProperties
    {
		static partial void OnNamePropertyChangedPartial(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = dependencyObject as View;
			if (view != null)
			{
				view.ContentDescription = (string)args.NewValue;
			}
		}
	}
}

