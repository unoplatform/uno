#nullable enable

using System;
using Windows.Devices.Input;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.Logging;
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
using Microsoft.Extensions.Logging;

namespace Uno.UI.Skia.Platform
{
	internal partial class WpfCoreWindowExtension : ICoreWindowExtension
	{
		private readonly ICoreWindowEvents _ownerEvents;
		private readonly WpfHost _host;

		private static int _currentFrameId;
		private HwndSource _hwndSource;

		// Win32 constants
		private const int WM_MOUSEWHEEL = 0x020A;
		private const int WM_MOUSEHWHEEL = 0x020E;
		private const int WM_DPICHANGED = 0x02E0;

		public CoreCursor PointerCursor
		{
			get => Mouse.OverrideCursor.ToCoreCursor();
			set => Mouse.OverrideCursor = value.ToCursor();
		}

		[Flags]
		private enum MouseModifierKeys : int
		{
			MK_LBUTTON = 0x0001,
			MK_RBUTTON = 0x0002,
			MK_SHIFT = 0x0004,
			MK_CONTROL = 0x0008,
			MK_MBUTTON = 0x0010,
			MK_XBUTTON1 = 0x0020,
			MK_XBUTTON2 = 0x0040,
		};


		public WpfCoreWindowExtension(object owner)
		{
			_ownerEvents = (ICoreWindowEvents)owner;

			_host = WpfHost.Current;

			_host.MouseEnter += HostOnMouseEnter;
			_host.MouseLeave += HostOnMouseLeave;
			_host.MouseMove += HostOnMouseMove;
			_host.MouseDown += HostOnMouseDown;
			_host.MouseUp += HostOnMouseUp;

			// Hook for native events
			_host.Loaded += HookNative;

			void HookNative(object sender, RoutedEventArgs e)
			{
				_host.Loaded -= HookNative;

				var win = Window.GetWindow(_host);

				win.AddHandler(UIElement.KeyUpEvent, (KeyEventHandler)HostOnKeyUp, true);
				win.AddHandler(UIElement.KeyDownEvent, (KeyEventHandler)HostOnKeyDown, true);

				var fromDependencyObject = PresentationSource.FromDependencyObject(win);
				_hwndSource = fromDependencyObject as HwndSource;
				_hwndSource?.AddHook(OnWmMessage);
			}
		}

		public void ReleasePointerCapture()
			=> WpfHost.Current.ReleaseMouseCapture();

		public void SetPointerCapture()
			=> WpfHost.Current.CaptureMouse();

		private static uint GetNextFrameId()
			=> (uint)Interlocked.Increment(ref _currentFrameId);

		private static PointerPointProperties BuildPointerProperties(MouseEventArgs args)
			=> new PointerPointProperties
			{
				IsLeftButtonPressed = args.LeftButton == MouseButtonState.Pressed,
				IsMiddleButtonPressed = args.MiddleButton == MouseButtonState.Pressed,
				IsRightButtonPressed = args.RightButton == MouseButtonState.Pressed,
				IsXButton1Pressed = args.XButton1 == MouseButtonState.Pressed,
				IsXButton2Pressed = args.XButton2 == MouseButtonState.Pressed,
				IsPrimary = true,
				IsInRange = true,
			};

		private static PointerDevice GetPointerDevice(MouseEventArgs args)
			=> args.Device switch
			{
				MouseDevice _ => PointerDevice.For(PointerDeviceType.Mouse),
				StylusDevice _ => PointerDevice.For(PointerDeviceType.Pen),
				TouchDevice _ => PointerDevice.For(PointerDeviceType.Touch),
				_ => PointerDevice.For(PointerDeviceType.Mouse),
			};

		private VirtualKeyModifiers GetKeyModifiers()
			=> VirtualKeyModifiers.None;

		private void HostOnMouseEnter(object sender, MouseEventArgs args)
		{
			try
			{
				var position = args.GetPosition(_host);
				var properties = BuildPointerProperties(args);
				var modifiers = GetKeyModifiers();

				_ownerEvents.RaisePointerEntered(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 1,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: properties.HasPressedButton,
							properties: properties
						),
						modifiers
					)
				);
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
				var position = args.GetPosition(_host);
				var properties = BuildPointerProperties(args);
				var modifiers = GetKeyModifiers();

				_ownerEvents.RaisePointerExited(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 1,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: properties.HasPressedButton,
							properties: properties
						),
						modifiers
					)
				);
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
				var position = args.GetPosition(_host);
				var properties = BuildPointerProperties(args);
				var modifiers = GetKeyModifiers();

				_ownerEvents.RaisePointerMoved(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 1,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: properties.HasPressedButton,
							properties: properties
						),
						modifiers
					)
				);
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
				var position = args.GetPosition(_host);
				var properties = BuildPointerProperties(args);
				var modifiers = GetKeyModifiers();

				_ownerEvents.RaisePointerPressed(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 1,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: true,
							properties: properties
						),
						modifiers
					)
				);
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
				var position = args.GetPosition(_host);
				var properties = BuildPointerProperties(args);
				var keys = GetKeyModifiers();

				_ownerEvents.RaisePointerReleased(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 1,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: properties.HasPressedButton,
							properties: properties
						),
						keys
					)
				);
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
				case WM_DPICHANGED:
					break;
				case WM_MOUSEHWHEEL:
				case WM_MOUSEWHEEL:
				{
					var keys = (MouseModifierKeys)GetLoWord(wparam);

					// Vertical: https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousewheel
					// Horizontal: https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousehwheel

					var l = (int)lparam;
					var screenPosition = new Point(GetLoWord(l), GetHiWord(l));
					var wpfPosition = _host.PointFromScreen(screenPosition);
					var position = new Windows.Foundation.Point(wpfPosition.X, wpfPosition.Y);

					var properties = new PointerPointProperties
					{
						IsLeftButtonPressed = keys.HasFlag(MouseModifierKeys.MK_LBUTTON),
						IsMiddleButtonPressed = keys.HasFlag(MouseModifierKeys.MK_MBUTTON),
						IsRightButtonPressed = keys.HasFlag(MouseModifierKeys.MK_RBUTTON),
						IsXButton1Pressed = keys.HasFlag(MouseModifierKeys.MK_XBUTTON1),
						IsXButton2Pressed = keys.HasFlag(MouseModifierKeys.MK_XBUTTON2),
						IsHorizontalMouseWheel = msg == WM_MOUSEHWHEEL,
						IsPrimary = true,
						IsInRange = true,
						MouseWheelDelta = -((int)wparam >> 16) / 40
					};
					var modifiers = VirtualKeyModifiers.None;
					if (keys.HasFlag(MouseModifierKeys.MK_SHIFT))
					{
						modifiers |= VirtualKeyModifiers.Shift;
					}
					if (keys.HasFlag(MouseModifierKeys.MK_CONTROL))
					{
						modifiers |= VirtualKeyModifiers.Control;
					}

					_ownerEvents.RaisePointerWheelChanged(
						new PointerEventArgs(
							new Windows.UI.Input.PointerPoint(
								frameId: GetNextFrameId(),
								timestamp: (uint)Environment.TickCount,
								device: PointerDevice.For(PointerDeviceType.Mouse),
								pointerId: 1,
								rawPosition: position,
								position: position,
								isInContact: properties.HasPressedButton,
								properties: properties
							),
							modifiers
						)
					);

					handled = true;
					break;
				}
			}

			return IntPtr.Zero;
		}
	}
}
