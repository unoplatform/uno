#nullable enable

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf;
using Uno.UI.Runtime.Skia.Wpf.Constants;
using Uno.UI.Runtime.Skia.Wpf.Input;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using WinUIInputManager = Uno.UI.Xaml.Core.InputManager;
using WpfControl = System.Windows.Controls.Control;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	internal class HostPointerHandler
	{
		private readonly IWpfHost _host;
		private readonly WpfControl _hostControl;
		private readonly ICoreWindowEvents _coreWindow;
		private HwndSource? _hwndSource;
		private PointerEventArgs? _previous;
		private WinUIInputManager? _inputManager;

		public HostPointerHandler(IWpfHost host)
		{
			if (host is not WpfControl hostControl)
			{
				throw new ArgumentException($"{nameof(host)} must be a WPF Control instance", nameof(host));
			}

			if (!host.IsIsland)
			{
				_coreWindow = CoreWindow.GetForCurrentThread()!;
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

		// TODO:MZ: InputManager is invoked directly only for islands,
		// in other cases input goes through CoreWindow
		private WinUIInputManager? InputManager => _host.IsIsland ?
			(_inputManager ??= _host.XamlRoot?.VisualTree.ContentRoot.InputManager) :
			null;

		public void SetPointerCapture(PointerIdentifier pointer)
			=> _hostControl.CaptureMouse();

		public void ReleasePointerCapture(PointerIdentifier pointer)
			=> _hostControl.ReleaseMouseCapture();

		#region Native events
		private void HostOnMouseEnter(object sender, WpfMouseEventArgs args)
		{
			try
			{
				var eventArgs = BuildPointerArgs(args);
				_coreWindow?.RaisePointerEntered(eventArgs);
				InputManager?.RaisePointerEntered(eventArgs);
				_previous = eventArgs;
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerEntered", e);
			}
		}

		private void HostOnMouseLeave(object sender, WpfMouseEventArgs args)
		{
			try
			{
				var eventArgs = BuildPointerArgs(args);
				_coreWindow?.RaisePointerExited(eventArgs);
				InputManager?.RaisePointerExited(eventArgs);
				_previous = eventArgs;
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerExited", e);
			}
		}

		private void HostOnMouseMove(object sender, WpfMouseEventArgs args)
		{
			try
			{
				var eventArgs = BuildPointerArgs(args);
				_coreWindow?.RaisePointerMoved(eventArgs);
				InputManager?.RaisePointerMoved(eventArgs);
				_previous = eventArgs;
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
				var eventArgs = BuildPointerArgs(args);
				_coreWindow?.RaisePointerPressed(eventArgs);
				InputManager?.RaisePointerPressed(eventArgs);
				_previous = eventArgs;
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
				var eventArgs = BuildPointerArgs(args);
				_coreWindow?.RaisePointerReleased(eventArgs);
				InputManager?.RaisePointerReleased(eventArgs);
				_previous = eventArgs;
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

						_coreWindow?.RaisePointerWheelChanged(ptArgs);
						InputManager?.RaisePointerWheelChanged(ptArgs);
						_previous = ptArgs;

						handled = ptArgs.Handled;
						break;
					}
			}

			return IntPtr.Zero;
		}
		#endregion

		private PointerEventArgs BuildPointerArgs(WpfMouseEventArgs args)
		{
			if (args is null)
			{
				throw new ArgumentNullException(nameof(args));
			}

			var position = args.GetPosition(_hostControl);
			var properties = BuildPointerProperties(args).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
			var modifiers = GetKeyModifiers();
			var point = new PointerPoint(
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

		private static PointerPointProperties BuildPointerProperties(WpfMouseEventArgs args)
		{
			if (args is null)
			{
				throw new ArgumentNullException(nameof(args));
			}

			return new()
			{
				IsLeftButtonPressed = args.LeftButton == MouseButtonState.Pressed,
				IsMiddleButtonPressed = args.MiddleButton == MouseButtonState.Pressed,
				IsRightButtonPressed = args.RightButton == MouseButtonState.Pressed,
				IsXButton1Pressed = args.XButton1 == MouseButtonState.Pressed,
				IsXButton2Pressed = args.XButton2 == MouseButtonState.Pressed,
				IsPrimary = true,
				IsInRange = true
			};
		}

		private static PointerDevice GetPointerDevice(WpfMouseEventArgs args)
		{
			if (args is null)
			{
				throw new ArgumentNullException(nameof(args));
			}

			return args.Device switch
			{
				System.Windows.Input.MouseDevice _ => PointerDevice.For(PointerDeviceType.Mouse),
				StylusDevice _ => PointerDevice.For(PointerDeviceType.Pen),
				TouchDevice _ => PointerDevice.For(PointerDeviceType.Touch),
				_ => PointerDevice.For(PointerDeviceType.Mouse),
			};
		}

		private VirtualKeyModifiers GetKeyModifiers()
			=> VirtualKeyModifiers.None;
	}
}
