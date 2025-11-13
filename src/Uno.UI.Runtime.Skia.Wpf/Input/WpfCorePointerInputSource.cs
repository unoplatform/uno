#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Constants;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Skia.Wpf;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.UI.Controls;
using Point = System.Windows.Point;
using Rect = Windows.Foundation.Rect;
using WpfControl = System.Windows.Controls.Control;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Uno.UI.XamlHost.Skia.Wpf;

internal sealed class WpfCorePointerInputSource : IUnoCorePointerInputSource
{
#pragma warning disable CS0067 // Some event are not raised on WPF ... yet!
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	private readonly FrameworkElement _renderLayer = default!;
	private readonly WpfControl? _host;
	private PointerEventArgs? _previous;

	private bool _queueExited;

	public WpfCorePointerInputSource(IXamlRootHost host)
	{
		if (host is null) return;

		if (host is not WpfControl hostControl)
		{
			throw new ArgumentException($"{nameof(host)} must be a WPF Control instance", nameof(host));
		}

		_host = hostControl;
		_renderLayer = host is UnoWpfWindowHost compositeWindowHost ? compositeWindowHost.RenderLayer : hostControl;

		// we only use the top layer for events since subscribing on the hostControl will receive
		// extra and/or out-of-order events.
		_renderLayer.MouseEnter += HostOnMouseEnter;
		_renderLayer.MouseLeave += HostOnMouseLeave;

		_renderLayer.StylusMove += HostControlOnStylusMove;
		_renderLayer.StylusDown += HostControlOnStylusDown;
		_renderLayer.StylusUp += HostControlOnStylusUp;

		_renderLayer.MouseMove += HostOnMouseMove;
		_renderLayer.MouseDown += HostOnMouseDown;
		_renderLayer.MouseUp += HostOnMouseUp;

		_renderLayer.LostMouseCapture += HostOnMouseCaptureLost;

		// Hook for native events
		if (_renderLayer.IsLoaded)
		{
			HookNative(null, null);
		}
		else
		{
			_renderLayer.Loaded += HookNative;
		}

		void HookNative(object? sender, RoutedEventArgs? e)
		{
			_renderLayer.Loaded -= HookNative;

			var win = Window.GetWindow(_renderLayer);

			var fromDependencyObject = PresentationSource.FromDependencyObject(win);
			var hwndSource = fromDependencyObject as HwndSource;
			hwndSource?.AddHook(OnWmMessage);
		}
	}

	public bool HasCapture => _renderLayer.IsMouseCaptured;

	public CoreCursor PointerCursor
	{
		get => Mouse.OverrideCursor.ToCoreCursor();
		set => Mouse.OverrideCursor = value.ToCursor();
	}

	[NotImplemented]
	public Windows.Foundation.Point PointerPosition { get; }

	public void SetPointerCapture(PointerIdentifier pointer)
		=> _renderLayer.CaptureMouse();

	public void SetPointerCapture()
		=> _renderLayer.CaptureMouse();

	public void ReleasePointerCapture(PointerIdentifier pointer) => ReleasePointerCapture();

	public void ReleasePointerCapture()
	{
		try
		{
			// When releasing the capture natively, we will synchronously get a Leave event.
			// This capture release can happen during the handling of another pointer event
			// (e.g. releasing during PointerUp), so we make sure to dispatch the next Exited
			// that will fire during ReleaseMouseCapture.
			_queueExited = true;
			_renderLayer.ReleaseMouseCapture();
		}
		finally
		{
			_queueExited = false;
		}
	}

	#region Native events

	private void HostOnMouseEvent(InputEventArgs args, TypedEventHandler<object, PointerEventArgs>? ev,
		bool? isReleaseOrCancel = null, [CallerArgumentExpression(nameof(ev))] string eventName = "")
	{
		var current = SynchronizationContext.Current;
		try
		{
			// Make sure WPF doesn't override our own SynchronizationContext.
			SynchronizationContext.SetSynchronizationContext(NativeDispatcher.Main.SynchronizationContext);

			var eventArgs = BuildPointerArgs(args, isReleaseOrCancel);
			ev?.Invoke(this, eventArgs);
			_previous = eventArgs;
		}
		catch (Exception e)
		{
			this.Log().Error($"Failed to raise {eventName}", e);
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(current);
		}
	}

	private void HostOnMouseEnter(object sender, WpfMouseEventArgs args)
	{
		HostOnMouseEvent(args, PointerEntered);
	}

	private void HostOnMouseLeave(object sender, WpfMouseEventArgs args)
	{
		if (_queueExited)
		{
			var xamlRoot = XamlRootMap.GetRootForHost((IWpfXamlRootHost)_host!);
			_ = xamlRoot!.VisualTree.RootElement.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => HostOnMouseEvent(args, PointerExited));
		}
		else
		{
			HostOnMouseEvent(args, PointerExited);
		}
	}


	private void HostControlOnStylusMove(object sender, StylusEventArgs args) =>
		HostOnMouseEvent(args, PointerMoved, isReleaseOrCancel: false);
	private void HostControlOnStylusDown(object sender, StylusEventArgs args) => HostOnMouseEvent(args, PointerPressed, isReleaseOrCancel: false);
	private void HostControlOnStylusUp(object sender, StylusEventArgs args) => HostOnMouseEvent(args, PointerReleased, isReleaseOrCancel: true);


	private void HostOnMouseMove(object sender, WpfMouseEventArgs args)
	{
		if (args.StylusDevice != null)
		{
			return;
		}

		HostOnMouseEvent(args, PointerMoved);
	}

	private void HostOnMouseDown(object sender, MouseButtonEventArgs args)
	{
		if (args.StylusDevice != null)
		{
			return;
		}

		HostOnMouseEvent(args, PointerPressed);
	}

	private void HostOnMouseUp(object sender, MouseButtonEventArgs args)
	{
		if (args.StylusDevice != null)
		{
			return;
		}

		HostOnMouseEvent(args, PointerReleased);
	}

	private void HostOnMouseCaptureLost(object sender, WpfMouseEventArgs args)
	{
		HostOnMouseEvent(args, PointerCaptureLost);
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
					var screenPosition = new System.Windows.Point(GetLoWord(l), GetHiWord(l));
					var wpfPosition = _renderLayer.PointFromScreen(screenPosition);
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
						MouseWheelDelta = (int)wparam >> 16
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
						timestamp: (ulong)(Environment.TickCount64 * 1000),
						device: PointerDevice.For(PointerDeviceType.Mouse),
						pointerId: 1,
						rawPosition: position,
						position: position,
						isInContact: properties.HasPressedButton,
						properties: properties
					);
					var ptArgs = new PointerEventArgs(point, modifiers);

					PointerWheelChanged?.Invoke(this, ptArgs);
					_previous = ptArgs;

					handled = ptArgs.Handled;
					break;
				}
		}

		return IntPtr.Zero;
	}
	#endregion

	#region Convert helpers

	private PointerEventArgs BuildPointerArgs(InputEventArgs args, bool? isReleaseOrCancel = null)
	{
		if (args is null)
		{
			throw new ArgumentNullException(nameof(args));
		}

		Point position;
		PointerPointProperties properties;

		uint pointerId;
		if (args is WpfMouseEventArgs mouseEventArgs)
		{
			pointerId = 1;
			position = mouseEventArgs.GetPosition(_renderLayer);
			properties = new()
			{
				IsLeftButtonPressed = mouseEventArgs.LeftButton == MouseButtonState.Pressed,
				IsMiddleButtonPressed = mouseEventArgs.MiddleButton == MouseButtonState.Pressed,
				IsRightButtonPressed = mouseEventArgs.RightButton == MouseButtonState.Pressed,
				IsXButton1Pressed = mouseEventArgs.XButton1 == MouseButtonState.Pressed,
				IsXButton2Pressed = mouseEventArgs.XButton2 == MouseButtonState.Pressed,
				IsPrimary = true,
				IsInRange = true
			};
		}
		else if (args is StylusEventArgs stylusEventArgs)
		{
			pointerId = (uint)stylusEventArgs.StylusDevice.Id;
			position = stylusEventArgs.GetPosition(_renderLayer);

			var isTouch = stylusEventArgs.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch;
			bool isLeftButtonPressed;
			if (isTouch)
			{
				// For touch, IsLeftButtonPressed has to be false for release and cancel.
				isLeftButtonPressed = isReleaseOrCancel != true;
			}
			else
			{
				// For pen, it has to be true only when !stylusEventArgs.InAir.
				isLeftButtonPressed = !stylusEventArgs.InAir;
			}

			properties = new()
			{
				IsLeftButtonPressed = isLeftButtonPressed,
				IsPrimary = true,
				IsInRange = !stylusEventArgs.InAir,
			};

			var stylusPointCollection = stylusEventArgs.GetStylusPoints(_renderLayer);
			if (stylusPointCollection.Count > 0)
			{
				var stylusPoint = stylusPointCollection[0];

				properties.Pressure = stylusPoint.PressureFactor;

				if (stylusPoint.HasProperty(StylusPointProperties.Width) && stylusPoint.HasProperty(StylusPointProperties.Height))
				{
					var width = stylusPoint.GetPropertyValue(StylusPointProperties.Width);
					var height = stylusPoint.GetPropertyValue(StylusPointProperties.Height);

					// Consider enable the ContactRectRaw property.
					//properties.ContactRectRaw = new Rect(position.X, position.Y, width, height);
					properties.ContactRect = new Rect(position.X, position.Y, width, height);
				}

				if (stylusPoint.HasProperty(StylusPointProperties.XTiltOrientation))
				{
					var xTilt = stylusPoint.GetPropertyValue(StylusPointProperties.XTiltOrientation);
					properties.XTilt = xTilt;
				}

				if (stylusPoint.HasProperty(StylusPointProperties.YTiltOrientation))
				{
					var yTilt = stylusPoint.GetPropertyValue(StylusPointProperties.YTiltOrientation);
					properties.YTilt = yTilt;
				}
			}
		}
		else
		{
			throw new ArgumentException();
		}

		var timestampInMicroseconds = (ulong)(args.Timestamp * 1000);
		properties = properties.SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
		var modifiers = GetKeyModifiers();
		var point = new PointerPoint(
			frameId: FrameIdProvider.GetNextFrameId(),
			timestamp: timestampInMicroseconds,
			device: GetPointerDevice(args),
			pointerId: pointerId,
			rawPosition: new Windows.Foundation.Point(position.X, position.Y),
			position: new Windows.Foundation.Point(position.X, position.Y),
			isInContact: properties.HasPressedButton,
			properties: properties
		);

		return new PointerEventArgs(point, modifiers);
	}

	private static PointerDevice GetPointerDevice(InputEventArgs args)
	{
		if (args is null)
		{
			throw new ArgumentNullException(nameof(args));
		}

		if (args is StylusEventArgs stylusEventArgs)
		{
			if (stylusEventArgs.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch)
			{
				return PointerDevice.For(PointerDeviceType.Touch);
			}
			else
			{
				return PointerDevice.For(PointerDeviceType.Pen);
			}
		}
		else
		{
			return PointerDevice.For(PointerDeviceType.Mouse);
		}
	}

	private static VirtualKeyModifiers GetKeyModifiers()
	{
		VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
		if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}

		if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}

		if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}

		if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}

		return modifiers;
	}
	#endregion
}
