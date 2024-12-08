using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#if XAMARIN
using NativeDependencyObject = System.Object;
#else
using NativeDependencyObject = Windows.UI.Xaml.DependencyObject;
#endif

namespace XamlGenerationTests.Shared
{
	public static class AttachedProperty_CollectionProperty_Data
	{
		public static UIElementCollection GetMyProperty(NativeDependencyObject obj)
		{
			return (UIElementCollection)obj.GetValue(MyPropertyProperty);
		}

		public static void SetMyProperty(NativeDependencyObject obj, Windows.UI.Xaml.Controls.UIElementCollection value)
		{
			obj.SetValue(MyPropertyProperty, value);
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.RegisterAttached("MyProperty", typeof(UIElementCollection), typeof(AttachedProperty_CollectionProperty_Data), new FrameworkPropertyMetadata(null));
	}
}
