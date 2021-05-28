using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class FlyoutHelper
	{
		public static FrameworkElement GetOpenFlyoutPresenter()
		{
#if NETFX_CORE
			var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
#else
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot.Current);
#endif
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("Expected exactly one open Popup.");
			}

			return popups[0] ?? throw new InvalidOperationException("Popup child should not be null.");
		}

		public static void HideFlyout<T>(T flyoutControl)
			where T : FlyoutBase
		{
			flyoutControl.Close();
		}

		internal static void OpenFlyout<T>(T flyoutControl, FrameworkElement target, FlyoutOpenMethod openMethod)
			where T: FlyoutBase
		{
			flyoutControl.Open();
		}

		public static void ValidateOpenFlyoutOverlayBrush(string name)
		{
			throw new NotImplementedException();
		}
	}

	internal enum FlyoutOpenMethod
	{
		Mouse,
		Touch,
		Pen,
		Keyboard,
		Gamepad,
		Programmatic_ShowAt,
		Programmatic_ShowAttachedFlyout
	}
}
