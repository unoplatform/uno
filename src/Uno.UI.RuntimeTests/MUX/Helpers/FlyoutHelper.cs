using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Enterprise;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class FlyoutHelper
	{
		public static FrameworkElement GetOpenFlyoutPresenter(XamlRoot xamlRoot)
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("Expected exactly one open Popup.");
			}

			var child = popups[0].Child ?? throw new InvalidOperationException("Popup child should not be null.");
			return child as FrameworkElement;
		}

		public static async Task HideFlyout<T>(T flyoutControl)
			where T : FlyoutBase
		{
#if WINAPPSDK
			flyoutControl.Hide();
#else
			Event closedEvent = new();
			var closedRegistration = CreateSafeEventRegistration<T, EventHandler<object>>("Closed");
			closedRegistration.Attach(flyoutControl, (s, e) => { closedEvent.Set(); });

			await RunOnUIThread(() =>
			{
				flyoutControl.Hide();
			});

			await closedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();
#endif
		}

		internal static async Task OpenFlyout<T>(T flyoutControl, FrameworkElement target, FlyoutOpenMethod openMethod)
			where T : FlyoutBase
		{
#if WINAPPSDK
			flyoutControl.ShowAt(target);
#else
			Event openingEvent = new Event();
			var openingRegistration = CreateSafeEventRegistration<T, EventHandler<object>>("Opening");
			openingRegistration.Attach(flyoutControl, (s, e) => openingEvent.Set());

			Event openedEvent = new Event();
			var openedRegistration = CreateSafeEventRegistration<T, EventHandler<object>>("Opened");
			openedRegistration.Attach(flyoutControl, (s, e) => openedEvent.Set());

			if (openMethod == FlyoutOpenMethod.Mouse)
			{
				TestServices.InputHelper.LeftMouseClick(target);
				await TestServices.WindowHelper.WaitForIdle();
				// Wait for the sub menu to open. It opens after a delay - clicking and waiting for idle doesn't open it.
				await TestServices.WindowHelper.SynchronouslyTickUIThread(60);
			}
			else if (openMethod == FlyoutOpenMethod.Touch)
			{
				TestServices.InputHelper.Tap(target);
				await TestServices.WindowHelper.WaitForIdle();
			}
			else if (openMethod == FlyoutOpenMethod.Pen)
			{
				throw new NotImplementedException("Pen not implemented yet");
				//TestServices.InputHelper.PenHold(target);
				//await TestServices.WindowHelper.WaitForIdle();
				//await TestServices.WindowHelper.SynchronouslyTickUIThread(60);
			}
			else if (openMethod == FlyoutOpenMethod.Keyboard)
			{
				await RunOnUIThread(() =>
				{
					target.Focus(FocusState.Keyboard);
				});
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.KeyboardHelper.PressKeySequence(" ");
				await TestServices.WindowHelper.WaitForIdle();
			}
			else if (openMethod == FlyoutOpenMethod.Gamepad)
			{
				await RunOnUIThread(() =>
				{
					target.Focus(FocusState.Keyboard);
				});
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.KeyboardHelper.GamepadA();
				await TestServices.WindowHelper.WaitForIdle();
			}
			else if (openMethod == FlyoutOpenMethod.Programmatic_ShowAt)
			{
				await RunOnUIThread(() =>
				{
					flyoutControl.ShowAt(target);
				});
				await TestServices.WindowHelper.WaitForIdle();
			}
			else if (openMethod == FlyoutOpenMethod.Programmatic_ShowAttachedFlyout)
			{
				await RunOnUIThread(() =>
				{
					FlyoutBase.ShowAttachedFlyout(target);
				});
				await TestServices.WindowHelper.WaitForIdle();
			}
			await openingEvent.WaitForDefault();
			await openedEvent.WaitForDefault();

			await TestServices.WindowHelper.WaitForIdle();
#endif
		}

		public static void ValidateOpenFlyoutOverlayBrush(string name)
		{
			throw new NotImplementedException();
		}

		public static async Task<Border> CreateTarget(
			double width,
			double height,
			Thickness margin,
			HorizontalAlignment halign,
			VerticalAlignment valign)
		{
			Border target = null;

			await RunOnUIThread(() =>
			{
				target = new Border();
				target.Background = new SolidColorBrush(Colors.RoyalBlue);
				target.Height = height;
				target.Width = width;
				target.Margin = margin;
				target.HorizontalAlignment = halign;
				target.VerticalAlignment = valign;
			});

			return target;
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
