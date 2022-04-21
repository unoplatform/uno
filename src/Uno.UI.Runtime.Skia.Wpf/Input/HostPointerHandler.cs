// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Windows.Devices.Input;
using Windows.UI.Core;
using Uno.Extensions;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.UI.Input;
using MouseDevice = System.Windows.Input.MouseDevice;
using System.Reflection;
using Windows.System;
using Uno.UI.Skia.Platform.Extensions;
using Uno.Foundation.Logging;
using UnoApplication = Windows.UI.Xaml.Application;
using WinUI = Windows.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using Uno.UI.Runtime.Skia.Wpf.Input;
using Uno.UI.Runtime.Skia.Wpf.Constants;
using Uno.UI.Runtime.Skia.Wpf;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	internal class HostPointerHandler
	{
		private WpfControl _hostControl;
		private HwndSource _hwndSource;
		private PointerEventArgs _previous;
		private readonly IWpfHost _host;

		public HostPointerHandler(IWpfHost host)
		{
			if (host is not WpfControl hostControl)
			{
				throw new ArgumentException($"{nameof(host)} must be a WPF Control instance", nameof(host));
			}
			
			_hostControl = hostControl;

			_hostControl.MouseEnter += HostOnMouseEnter;
			_hostControl.MouseLeave += HostOnMouseLeave;
			_hostControl.MouseMove += HostOnMouseMove;
			_hostControl.MouseDown += HostOnMouseDown;
			_hostControl.MouseUp += HostOnMouseUp;

			// Hook for native events
			_hostControl.Loaded += HookNative;

			void HookNative(object sender, RoutedEventArgs e)
			{
				_hostControl.Loaded -= HookNative;

				var win = Window.GetWindow(_hostControl);

				var fromDependencyObject = PresentationSource.FromDependencyObject(win);
				_hwndSource = fromDependencyObject as HwndSource;
				_hwndSource?.AddHook(OnWmMessage);
			}
			_host = host;
		}

		public void SetPointerCapture(PointerIdentifier pointer)
			=> _hostControl.CaptureMouse();

		public void ReleasePointerCapture(PointerIdentifier pointer)
			=> _hostControl.ReleaseMouseCapture();

		#region Native events
		private void HostOnMouseEnter(object sender, MouseEventArgs args)
		{
			try
			{
				_host.XamlRoot?.VisualTree.ContentRoot.InputManager.RaisePointerEntered(_previous = BuildPointerArgs(args));
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerEntered", e);
			}
		}

		private void HostOnMouseLeave(object sender, MouseEventArgs args)
		{
			try
			{
				_host.XamlRoot?.VisualTree.ContentRoot.InputManager.RaisePointerExited(_previous = BuildPointerArgs(args));
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerExited", e);
			}
		}

		private void HostOnMouseMove(object sender, MouseEventArgs args)
		{
			try
			{
				_host.XamlRoot?.VisualTree.ContentRoot.InputManager.RaisePointerMoved(_previous = BuildPointerArgs(args));
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerMoved", e);
			}
		}

		private void HostOnMouseDown(object sender, MouseButtonEventArgs args)
		{
			try
			{
				// IsInContact: true
				_host.XamlRoot?.VisualTree.ContentRoot.InputManager.RaisePointerPressed(_previous = BuildPointerArgs(args));
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerPressed", e);
			}
		}

		private void HostOnMouseUp(object sender, MouseButtonEventArgs args)
		{
			try
			{
				_host.XamlRoot?.VisualTree.ContentRoot.InputManager.RaisePointerReleased(_previous = BuildPointerArgs(args));
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerReleased", e);
			}
		}

		private IntPtr OnWmMessage(IntPtr hwnd, int msg, IntPtr wparamOriginal, IntPtr lparamOriginal, ref bool handled)
		{
			var wparam = (int)(((long)wparamOriginal) & 0xFFFFFFFF);
			var lparam = (int)(((long)lparamOriginal) & 0xFFFFFFFF);

			static short GetLoWord(int i) => (short)(i & 0xFFFF);
			static short GetHiWord(int i) => (short)(i >> 16);

			switch (msg)
			{
				case Win32Messages.WM_DPICHANGED:
					break;
				case Win32Messages.WM_MOUSEHWHEEL:
				case Win32Messages.WM_MOUSEWHEEL:
					{
						var keys = (MouseModifierKeys)GetLoWord(wparam);

						// Vertical: https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousewheel
						// Horizontal: https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousehwheel

						var l = (int)lparam;
						var screenPosition = new Point(GetLoWord(l), GetHiWord(l));
						var wpfPosition = _hostControl.PointFromScreen(screenPosition);
						var position = new Windows.Foundation.Point(wpfPosition.X, wpfPosition.Y);

						var properties = new PointerPointProperties
						{
							IsLeftButtonPressed = keys.HasFlag(MouseModifierKeys.MK_LBUTTON),
							IsMiddleButtonPressed = keys.HasFlag(MouseModifierKeys.MK_MBUTTON),
							IsRightButtonPressed = keys.HasFlag(MouseModifierKeys.MK_RBUTTON),
							IsXButton1Pressed = keys.HasFlag(MouseModifierKeys.MK_XBUTTON1),
							IsXButton2Pressed = keys.HasFlag(MouseModifierKeys.MK_XBUTTON2),
							IsHorizontalMouseWheel = msg == Win32Messages.WM_MOUSEHWHEEL,
							IsPrimary = true,
							IsInRange = true,
							MouseWheelDelta = -((int)wparam >> 16) / 40
						}.SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);

						var modifiers = VirtualKeyModifiers.None;
						if (keys.HasFlag(MouseModifierKeys.MK_SHIFT))
						{
							modifiers |= VirtualKeyModifiers.Shift;
						}
						if (keys.HasFlag(MouseModifierKeys.MK_CONTROL))
						{
							modifiers |= VirtualKeyModifiers.Control;
						}

						var point = new Windows.UI.Input.PointerPoint(
							frameId: FrameIdProvider.GetNextFrameId(),
							timestamp: (ulong)Environment.TickCount,
							device: PointerDevice.For(PointerDeviceType.Mouse),
							pointerId: 1,
							rawPosition: position,
							position: position,
							isInContact: properties.HasPressedButton,
							properties: properties
						);
						var ptArgs = new PointerEventArgs(point, modifiers);

						_host.XamlRoot?.VisualTree.ContentRoot.InputManager.RaisePointerWheelChanged(_previous = ptArgs);

						handled = true;
						break;
					}
			}

			return IntPtr.Zero;
		}
		#endregion

		private PointerEventArgs BuildPointerArgs(MouseEventArgs args)
		{
			var position = args.GetPosition(_hostControl);
			var properties = BuildPointerProperties(args).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
			var modifiers = GetKeyModifiers();
			var point = new Windows.UI.Input.PointerPoint(
				frameId: FrameIdProvider.GetNextFrameId(),
				timestamp: (ulong)(args.Timestamp * TimeSpan.TicksPerMillisecond),
				device: GetPointerDevice(args),
				pointerId: 1,
				rawPosition: new Windows.Foundation.Point(position.X, position.Y),
				position: new Windows.Foundation.Point(position.X, position.Y),
				isInContact: properties.HasPressedButton,
				properties: properties
			);

			return new PointerEventArgs(point, modifiers);
		}

		private static PointerPointProperties BuildPointerProperties(System.Windows.Input.MouseEventArgs args)
			=> new()
			{
				IsLeftButtonPressed = args.LeftButton == MouseButtonState.Pressed,
				IsMiddleButtonPressed = args.MiddleButton == MouseButtonState.Pressed,
				IsRightButtonPressed = args.RightButton == MouseButtonState.Pressed,
				IsXButton1Pressed = args.XButton1 == MouseButtonState.Pressed,
				IsXButton2Pressed = args.XButton2 == MouseButtonState.Pressed,
				IsPrimary = true,
				IsInRange = true
			};

		private static PointerDevice GetPointerDevice(System.Windows.Input.MouseEventArgs args)
			=> args.Device switch
			{
				System.Windows.Input.MouseDevice _ => PointerDevice.For(PointerDeviceType.Mouse),
				StylusDevice _ => PointerDevice.For(PointerDeviceType.Pen),
				TouchDevice _ => PointerDevice.For(PointerDeviceType.Touch),
				_ => PointerDevice.For(PointerDeviceType.Mouse),
			};

		private VirtualKeyModifiers GetKeyModifiers()
			=> VirtualKeyModifiers.None;
	}
}
