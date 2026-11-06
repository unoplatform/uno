using System;
using Uno.UI.DevTools.Input;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Enterprise;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Microsoft.UI;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class FlyoutHelper
	{
		public static FrameworkElement GetOpenFlyoutPresenter()
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("Expected exactly one open Popup.");
			}

			var child = popups[0].Child ?? throw new InvalidOperationException("Popup child should not be null.");
			return child as FrameworkElement;
		}

		public static async Task HideFlyout<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] T
		>(T flyoutControl)
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

		internal static async Task OpenFlyout<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] T
		>(T flyoutControl, FrameworkElement target, FlyoutOpenMethod openMethod)
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

		public static async Task<Flyout> CreateDefaultFlyout(FlyoutPlacementMode placement)
		{
			Flyout flyout = null;

			await RunOnUIThread(() =>
			{
				var content = new StackPanel();
				content.Width = 300;

				var button = new Button();
				button.Margin = new Thickness(12);
				button.FontSize = 25;
				button.FontWeight = Microsoft.UI.Text.FontWeights.Thin;
				button.HorizontalAlignment = HorizontalAlignment.Stretch;
				button.Content = "button";

				var textbox = new TextBox();
				textbox.Margin = new Thickness(12);
				textbox.Height = 225;
				textbox.HorizontalAlignment = HorizontalAlignment.Stretch;
				button.FontSize = 18;
				button.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
				textbox.Text = "textbox";

				var checkbox = new CheckBox();
				checkbox.Margin = new Thickness(12);
				checkbox.FontSize = 25;
				checkbox.FontWeight = Microsoft.UI.Text.FontWeights.Thin;
				checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
				checkbox.Content = "checkbox";

				content.Children.Add(button);
				content.Children.Add(textbox);
				content.Children.Add(checkbox);

				var style = new Style(typeof(FlyoutPresenter));
				style.Setters.Add(new Setter(FlyoutPresenter.TagProperty, "presenter_style"));

				flyout = new Flyout();
				flyout.Placement = placement;
				flyout.Content = content;
				flyout.FlyoutPresenterStyle = style;
			});

			return flyout;
		}

		public static FlyoutPresenter GetFlyoutPresenter(Flyout flyout)
		{
			TestServices.VERIFY_IS_NOT_NULL(flyout.Content);
			return TreeHelper.FindAncestor<FlyoutPresenter>(flyout.Content);
		}

		internal static async Task ShowFlyoutWithOptions<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] T
		>(T flyoutControl, DependencyObject targetElement, FlyoutShowOptions showOptions)
			where T : FlyoutBase
		{
			Event openingEvent = new();
			var openingRegistration = CreateSafeEventRegistration<T, EventHandler<object>>("Opening");
			openingRegistration.Attach(flyoutControl, (s, e) => openingEvent.Set());

			Event openedEvent = new();
			var openedRegistration = CreateSafeEventRegistration<T, EventHandler<object>>("Opened");
			openedRegistration.Attach(flyoutControl, (s, e) => openedEvent.Set());

			await RunOnUIThread(() =>
			{
				flyoutControl.ShowAt(targetElement, showOptions);
			});

			await openingEvent.WaitForDefault();
			await openedEvent.WaitForDefault();

			await TestServices.WindowHelper.WaitForIdle();
		}

		// FlyoutHelper.OpenFlyout does not implement FlyoutOpenMethod.Pen: this is its Pen branch, with a pen hold.
		internal static async Task OpenFlyoutWithPenHold<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] T
		>(T flyoutControl, FrameworkElement target)
			where T : FlyoutBase
		{
			Event openingEvent = new();
			var openingRegistration = CreateSafeEventRegistration<T, EventHandler<object>>("Opening");
			openingRegistration.Attach(flyoutControl, (s, e) => openingEvent.Set());

			Event openedEvent = new();
			var openedRegistration = CreateSafeEventRegistration<T, EventHandler<object>>("Opened");
			openedRegistration.Attach(flyoutControl, (s, e) => openedEvent.Set());

			Windows.Foundation.Point center = default;
			await RunOnUIThread(() =>
			{
				center = target.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(target.ActualWidth / 2, target.ActualHeight / 2));
			});

			await RunOnUIThread(async () =>
			{
				var injector = Windows.UI.Input.Preview.Injection.InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
#if !WINAPPSDK
				using var pen = injector.GetPen();
				await Uno.UI.RuntimeTests.Helpers.InjectedPointerHoldExtensions.Hold(pen, center);
#else
				throw new NotSupportedException("Needs Uno's injected pen helpers.");
#endif
			});

			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.SynchronouslyTickUIThread(60);

			await openingEvent.WaitForDefault();
			await openedEvent.WaitForDefault();

			await TestServices.WindowHelper.WaitForIdle();
		}

		public static FrameworkElement GetOpenFlyoutOverlayElement()
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("Expected exactly one open Popup.");
			}

#if !WINAPPSDK
			return TestServices.Utilities.GetPopupOverlayElement(popups[0]);
#else
			throw new NotSupportedException("Needs access to the popup's overlay element.");
#endif
		}

		public static async Task ValidateOverlayBrush<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] T
		>(string expectedBrushKey)
			where T : FlyoutBase, new()
		{
			T flyout = null;
			Button target = null;

			await RunOnUIThread(() =>
			{
				flyout = new T();
				flyout.LightDismissOverlayMode = LightDismissOverlayMode.On;

				target = new Button();
				TestServices.WindowHelper.WindowContent = target;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

			await ValidateOpenFlyoutOverlayBrushAsync(expectedBrushKey);

			await HideFlyout(flyout);
		}

		public static async Task ValidateOpenFlyoutOverlayBrushAsync(string expectedBrushKey)
		{
			await RunOnUIThread(() =>
			{
				var expectedBrush = (SolidColorBrush)Application.Current.Resources[expectedBrushKey];

				var overlayElement = GetOpenFlyoutOverlayElement();
				if (overlayElement is null)
				{
					throw new InvalidOperationException("An overlay element should exist for the flyout.");
				}

				var overlayRect = overlayElement as Microsoft.UI.Xaml.Shapes.Rectangle;
				if (overlayRect is null)
				{
					throw new InvalidOperationException("The overlay element should be a rectangle.");
				}

				var overlayBrush = (SolidColorBrush)overlayRect.Fill;
				VERIFY_IS_NOT_NULL(overlayBrush);
				VERIFY_IS_TRUE(overlayBrush.Equals(expectedBrush));
			});
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
