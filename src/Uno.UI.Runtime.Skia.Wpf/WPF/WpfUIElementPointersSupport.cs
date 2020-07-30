using System;
using Windows.Devices.Input;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.Logging;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;
using System.Threading;
using System.Windows.Input;
using Windows.UI.Input;
using MouseDevice = System.Windows.Input.MouseDevice;

namespace Uno.UI.Skia.Platform
{
	public class WpfUIElementPointersSupport : ICoreWindowExtension
	{
		private CoreWindow _owner;
		private ICoreWindowEvents _ownerEvents;
		private WpfWindow _mainWpfWindow;
		private WpfHost _host;

		private static int _currentFrameId;

		public WpfUIElementPointersSupport(object owner)
		{
			_owner = (CoreWindow)owner;
			_ownerEvents = (ICoreWindowEvents)owner;

			_mainWpfWindow = WpfApplication.Current.MainWindow;
			_host = WpfHost.Current;

			_host.MouseEnter += HostOnMouseEnter;
			_host.MouseLeave += HostOnMouseLeave;
			_host.MouseMove += HostOnMouseMove;
			_host.MouseDown += HostOnMouseDown;
			_host.MouseUp += HostOnMouseUp;
			_host.MouseWheel += HostOnMouseWheel;

		}

		private static uint GetNextFrameId() => (uint)Interlocked.Increment(ref _currentFrameId);

		private static PointerPointProperties BuildPointerProperties(MouseEventArgs args, int wheelDelta = 0)
		{
			var properties = new PointerPointProperties
			{
				IsLeftButtonPressed = args.LeftButton == MouseButtonState.Pressed,
				IsRightButtonPressed = args.RightButton == MouseButtonState.Pressed,
				IsPrimary = true
			};

			if (wheelDelta != 0)
			{
				properties.MouseWheelDelta = -wheelDelta / 10;
			}

			return properties;
		}

		private static PointerDevice GetPointerDevice(MouseEventArgs args)
		{
			return args.Device switch
			{
				MouseDevice _ => PointerDevice.For(PointerDeviceType.Mouse),
				StylusDevice _ => PointerDevice.For(PointerDeviceType.Pen),
				TouchDevice _ => PointerDevice.For(PointerDeviceType.Touch),
				_ => PointerDevice.For(PointerDeviceType.Mouse),
			};
		}

		private void HostOnMouseEnter(object sender, MouseEventArgs args)
		{
			try
			{
				var position = args.GetPosition(_host);

				_ownerEvents.RaisePointerEntered(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: false,
							properties: BuildPointerProperties(args)
						)
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

				_ownerEvents.RaisePointerExited(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: false,
							properties: BuildPointerProperties(args)
						)
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

				_ownerEvents.RaisePointerMoved(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: false,
							properties: BuildPointerProperties(args)
						)
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

				_ownerEvents.RaisePointerPressed(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: false,
							properties: BuildPointerProperties(args)
						)
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

				_ownerEvents.RaisePointerReleased(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: false,
							properties: BuildPointerProperties(args)
						)
					)
				);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerReleased", e);
			}
		}

		private void HostOnMouseWheel(object sender, MouseWheelEventArgs args)
		{
			try
			{
				var position = args.GetPosition(_host);

				_ownerEvents.RaisePointerWheelChanged(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: (uint)args.Timestamp,
							device: GetPointerDevice(args),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(position.X, position.Y),
							position: new Windows.Foundation.Point(position.X, position.Y),
							isInContact: false,
							properties: BuildPointerProperties(args, args.Delta)
						)
					)
				);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerWheelChanged", e);
			}
		}
	}
}
