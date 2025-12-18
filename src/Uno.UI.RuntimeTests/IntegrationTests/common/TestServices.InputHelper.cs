using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;
using System.Threading;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;

#if HAS_UNO
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Core;

#endif

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
				EnsureInputInjectorSupported();
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");
					mouse.MoveTo(position);
				});
			}

			public static void MoveMouse(UIElement element)
			{
				EnsureInputInjectorSupported();
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var topLeft = element.TransformToVisual(WindowHelper.XamlRoot.Content).TransformPoint(new Point(0, 0));
					var center = new Point(topLeft.X + element.RenderSize.Width / 2, topLeft.Y + element.RenderSize.Height / 2);
					var mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");
					mouse.MoveTo(center);
				});
			}

			public static void Hold(UIElement element)
			{
				throw new System.NotImplementedException();
			}

			public static void Tap(UIElement element, uint waitBetweenPressRelease = 0)
			{
#if WINAPPSDK || HAS_INPUT_INJECTOR
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
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var inputManager = VisualTree.GetContentRootForElement(element).InputManager;
					if (inputManager is not null)
					{
						inputManager.LastInputDeviceType = InputDeviceType.Touch;
					}
					// fall back to a tap event on platforms where InputInjector isn't implemented. Ideally tap should be triggered
					// by GestureRecognizer when a pointer is pressed and released, but here we do a hacky workaround
					var args = new TappedEventArgs(1, PointerDeviceType.Touch, default, 1);
					element.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs(element, args, element));
				});
#endif
			}
			public static void Tap(Point point)
			{
				EnsureInputInjectorSupported();
				Finger finger = null;
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					finger = InputInjector.TryCreate()?.GetFinger() ?? throw new InvalidOperationException("Failed to create finger");
					finger.Press(point);
				});

				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					finger.Release();
				});
			}

			public static void ScrollMouseWheel(UIElement cv, int i)
			{
				throw new System.NotImplementedException();
			}

			public static void LeftMouseClick(UIElement element)
			{
				EnsureInputInjectorSupported();
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");
					var topLeft = element.TransformToVisual(WindowHelper.XamlRoot.Content).TransformPoint(new Point(0, 0));
					var center = new Point(topLeft.X + element.RenderSize.Width / 2, topLeft.Y + element.RenderSize.Height / 2);
					mouse.Press(center);
					mouse.Release();
				});
			}

			public static void LeftMouseClick(Point point)
			{
				EnsureInputInjectorSupported();
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");
					mouse.Press(point);
					mouse.Release();
				});
			}

			public static void PenBarrelTap(FrameworkElement pElement)
			{
				throw new System.NotImplementedException();
			}

			public static void ClickMouseButton(MouseButton button, Point position)
			{
				EnsureInputInjectorSupported();
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");

					switch (button)
					{
						case MouseButton.Left:
							mouse.Press(position);
							mouse.Release();
							break;
						case MouseButton.Right:
							mouse.PressRight(position);
							mouse.ReleaseRight();
							break;
						case MouseButton.Middle:
							mouse.MoveTo(position);
							mouse.PressMiddle();
							mouse.ReleaseMiddle();
							break;
						default:
							throw new ArgumentException($"Unsupported mouse button: {button}", nameof(button));
					}
				});
			}

			internal static void MouseButtonDown(FrameworkElement element, int dx, int dy, MouseButton button)
			{
				EnsureInputInjectorSupported();
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");
					var topLeft = element.TransformToVisual(WindowHelper.XamlRoot.Content).TransformPoint(new Point(0, 0));
					var center = new Point(topLeft.X + element.RenderSize.Width / 2, topLeft.Y + element.RenderSize.Height / 2);
					var target = new Point(center.X + dx, center.Y + dy);

					mouse.MoveTo(target);

					switch (button)
					{
						case MouseButton.Left:
							mouse.Press();
							break;
						case MouseButton.Right:
							mouse.PressRight();
							break;
						case MouseButton.Middle:
							mouse.PressMiddle();
							break;
						default:
							throw new ArgumentException($"Unsupported mouse button: {button}", nameof(button));
					}
				});
			}

			internal static void MouseButtonUp(FrameworkElement element, int dx, int dy, MouseButton button)
			{
				EnsureInputInjectorSupported();
				MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
				{
					var mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");

					switch (button)
					{
						case MouseButton.Left:
							mouse.Release();
							break;
						case MouseButton.Right:
							mouse.ReleaseRight();
							break;
						case MouseButton.Middle:
							mouse.ReleaseMiddle();
							break;
						default:
							throw new ArgumentException($"Unsupported mouse button: {button}", nameof(button));
					}
				});
			}

			private static void EnsureInputInjectorSupported()
			{
#if !WINAPPSDK && !HAS_INPUT_INJECTOR
				Assert.Inconclusive("InputInjector is not supported on this platform.");		
#endif
			}
		}
	}
}
