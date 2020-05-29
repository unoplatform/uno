using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Toolkit
{
#if __IOS__
	[global::Foundation.PreserveAttribute(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.PreserveAttribute(AllMembers = true)]
#endif
	public class MenuFlyoutExtensions
	{
		#region Property: OverrideIosCancelText
		public static DependencyProperty OverrideIosCancelTextProperty { get; } = DependencyProperty.RegisterAttached(
			"OverrideIosCancelText",
			typeof(string),
			typeof(MenuFlyoutExtensions),
			new PropertyMetadata(default(string)));

		public static string GetOverrideIosCancelText(MenuFlyout obj) => (string)obj.GetValue(OverrideIosCancelTextProperty);

		public static void SetOverrideIosCancelText(MenuFlyout obj, string value) => obj.SetValue(OverrideIosCancelTextProperty, value);
		#endregion
	}
}
