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
	public static class MenuFlyoutItemExtensions
	{
		#region IsDestructive

		public static DependencyProperty IsDestructiveProperty { get; } =
			DependencyProperty.RegisterAttached(
				"IsDestructive",
				typeof(bool),
				typeof(MenuFlyoutItemExtensions),
				new PropertyMetadata(false)
			);

		public static void SetIsDestructive(this MenuFlyoutItem menuFlyoutItem, bool isDestructive)
		{
			menuFlyoutItem.SetValue(IsDestructiveProperty, isDestructive);
		}

		public static bool GetIsDestructive(this MenuFlyoutItem menuFlyoutItem)
		{
			return (bool)menuFlyoutItem.GetValue(IsDestructiveProperty);
		}

		#endregion
	}
}
