using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Toolkit
{
#if !NET6_0_OR_GREATER // Moved to the linker definition file
#if __IOS__
	[global::Foundation.PreserveAttribute(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.PreserveAttribute(AllMembers = true)]
#endif
#endif
	public static class SplitViewExtensions
	{
		#region IsPaneEnabled

		public static DependencyProperty IsPaneEnabledProperty { get; } =
			DependencyProperty.RegisterAttached(
				"IsPaneEnabled",
				typeof(bool),
				typeof(SplitViewExtensions),
				new PropertyMetadata(true)
			);

		public static void SetIsPaneEnabled(this SplitView splitView, bool isPaneEnabled)
		{
			splitView.SetValue(IsPaneEnabledProperty, isPaneEnabled);
		}

		public static bool GetIsPaneEnabled(this SplitView splitView)
		{
			return (bool)splitView.GetValue(IsPaneEnabledProperty);
		}

		#endregion
	}
}
