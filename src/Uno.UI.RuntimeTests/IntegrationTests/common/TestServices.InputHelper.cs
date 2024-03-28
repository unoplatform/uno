using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;
using System.Threading;
using Windows.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Private.Infrastructure
{
	partial class TestServices
	{
		public static class InputHelper
		{
			public static void DynamicPressCenter(UIElement element, double x, double y, PointerFinger finger)
			{
				throw new System.NotImplementedException();
			}
			public static void DynamicRelease(PointerFinger finger)
			{
				throw new System.NotImplementedException();
			}

			public static void MoveMouse(Point position)
			{
				throw new System.NotImplementedException();
			}

			public static void MoveMouse(UIElement element)
			{
				throw new System.NotImplementedException();
			}

			public static void Hold(UIElement element)
			{
				throw new System.NotImplementedException();
			}

			public static void Tap(UIElement element, uint waitBetweenPressRelease = 0)
			{
#if WINAPPSDK || __SKIA__
				Finger finger = null;
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					finger = InputInjector.TryCreate()?.GetFinger() ?? throw new InvalidOperationException("Failed to create finger");
					var topLeft = element.TransformToVisual(WindowHelper.XamlRoot.Content).TransformPoint(new Point(0, 0));
					var center = new Point(topLeft.X + element.RenderSize.Width / 2, topLeft.Y + element.RenderSize.Height / 2);
					finger.Press(center);
				});

				// For some tests when running on Windows, we need to explicitly wait between press and release, e.g. in CalendarDatePickerIntegrationTests
				// UNO TODO: Why do we need this?
				Thread.Sleep((int)waitBetweenPressRelease);

				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					finger.Release();
				});

#else
				// fall back to a tap event on platforms where InputInjector isn't implemented. Ideally tap should be triggered
				// by GestureRecognizer when a pointer is pressed and released, but here we do a hacky workaround
				var args = new TappedEventArgs(1, PointerDeviceType.Touch, default, 1);
				element.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs(element, args));
#endif
			}
			public static void Tap(Point point)
			{
				throw new System.NotImplementedException();
			}

			public static void ScrollMouseWheel(CalendarView cv, int i)
			{
				throw new System.NotImplementedException();
			}

			public static void LeftMouseClick(UIElement element) => Tap(element);

			public static void PenBarrelTap(FrameworkElement pElement)
			{
				throw new System.NotImplementedException();
			}

			public static void ClickMouseButton(MouseButton button, Point position)
			{
				throw new System.NotImplementedException();
			}
		}
	}
}
