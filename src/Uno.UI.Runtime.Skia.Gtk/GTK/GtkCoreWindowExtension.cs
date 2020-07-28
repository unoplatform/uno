using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gdk;
using Gtk;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Logging;
using Uno.UI.Runtime.Skia;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Input;

namespace Uno.UI.Runtime.Skia
{
	public class GtkUIElementPointersSupport : ICoreWindowExtension
	{
		private readonly CoreWindow _owner;
		private ICoreWindowEvents _ownerEvents;
		private static int _currentFrameId;

		public GtkUIElementPointersSupport(object owner)
		{
			_owner = (CoreWindow)owner;
			_ownerEvents = (ICoreWindowEvents)owner;

			GtkHost.Window.AddEvents((int)(
				Gdk.EventMask.PointerMotionMask
				| EventMask.ButtonPressMask
				| EventMask.SmoothScrollMask
			));
			GtkHost.Window.MotionNotifyEvent += OnMotionEvent;
			GtkHost.Window.ButtonPressEvent += Window_ButtonPressEvent;
			GtkHost.Window.ButtonReleaseEvent += Window_ButtonReleaseEvent;
			GtkHost.Window.EnterNotifyEvent += Window_EnterEvent;
			GtkHost.Window.LeaveNotifyEvent += Window_LeaveEvent;
			GtkHost.Window.ScrollEvent += Window_ScrollEvent;
		}

		private static uint GetNextFrameId() => (uint)Interlocked.Increment(ref _currentFrameId);

		private void Window_ScrollEvent(object o, ScrollEventArgs args)
		{
			try
			{
				if (args.Event.Direction == ScrollDirection.Smooth)
				{
					_ownerEvents.RaisePointerWheelChanged(
						new PointerEventArgs(
							new Windows.UI.Input.PointerPoint(
								frameId: GetNextFrameId(),
								timestamp: args.Event.Time,
								device: PointerDevice.For(PointerDeviceType.Mouse),
								pointerId: 0,
								rawPosition: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
								position: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
								isInContact: false,
								properties: BuildProperties(args.Event)
							)
						)
					);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerExited", e);
			}
		}

		private PointerPointProperties BuildProperties(EventScroll scrollEvent)
			=> new PointerPointProperties
			   {
				   MouseWheelDelta = scrollEvent.DeltaX != 0 ? (int)scrollEvent.DeltaX : (int)scrollEvent.DeltaY,
				   IsHorizontalMouseWheel = scrollEvent.DeltaX != 0,
				};

		private void Window_LeaveEvent(object o, LeaveNotifyEventArgs args)
		{
			try
			{
				_ownerEvents.RaisePointerExited(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: args.Event.Time,
							device: PointerDevice.For(PointerDeviceType.Mouse),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							position: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							isInContact: false,
							properties: BuildProperties(args.Event)
						)
					)
				);
			}
			catch(Exception e)
			{
				this.Log().Error("Failed to raise PointerExited", e);
			}
		}

		private void Window_EnterEvent(object o, EnterNotifyEventArgs args)
		{
			try
			{
				_ownerEvents.RaisePointerEntered(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: args.Event.Time,
							device: PointerDevice.For(PointerDeviceType.Mouse),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							position: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							isInContact: false,
							properties: BuildProperties(args.Event)
						)
					)
				);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerEntered", e);
			}
		}

		private void Window_ButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			try
			{
				_ownerEvents.RaisePointerReleased(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: args.Event.Time,
							device: PointerDevice.For(PointerDeviceType.Mouse),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							position: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							isInContact: false,
							properties: BuildProperties(args.Event)
						)
					)
				);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerReleased", e);
			}
		}

		private void Window_ButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			try
			{
				_ownerEvents.RaisePointerPressed(
					new PointerEventArgs(
						new Windows.UI.Input.PointerPoint(
							frameId: GetNextFrameId(),
							timestamp: args.Event.Time,
							device: PointerDevice.For(PointerDeviceType.Mouse),
							pointerId: 0,
							rawPosition: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							position: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
							isInContact: false,
							properties: BuildProperties(args.Event)
						)
					)
				);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerPressed", e);
			}
		}

		private PointerPointProperties BuildProperties(EventButton eventButton)
			=> new PointerPointProperties
			{
				IsLeftButtonPressed = eventButton.Button == 1,
				IsRightButtonPressed = eventButton.Button == 3,
			};

		private PointerPointProperties BuildProperties(EventCrossing eventCrossing)
			=> new PointerPointProperties
			{
				IsLeftButtonPressed = (eventCrossing.State & ModifierType.Button1Mask) != 0,
				IsRightButtonPressed = (eventCrossing.State & ModifierType.Button4Mask) != 0,
			};

		private void OnMotionEvent(object o, MotionNotifyEventArgs args)
		{
			try
			{
				switch (args.Event.Type)
				{
					case Gdk.EventType.MotionNotify:
						_ownerEvents.RaisePointerMoved(
							new PointerEventArgs(
								new Windows.UI.Input.PointerPoint(
									frameId: GetNextFrameId(),
									timestamp: args.Event.Time,
									device: PointerDevice.For(PointerDeviceType.Mouse),
									pointerId: 0,
									rawPosition: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
									position: new Windows.Foundation.Point(args.Event.X, args.Event.Y),
									isInContact: false,
									properties: BuildProperties(args.Event)
								)
							)
						);
						break;

					default:
						Console.WriteLine($"Unknown event: {args.Event.State}");
						break;
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerMoved", e);
			}
		}

		private PointerPointProperties BuildProperties(EventMotion eventMotion)
			=> new Windows.UI.Input.PointerPointProperties()
			{
				IsLeftButtonPressed = (eventMotion.State & Gdk.ModifierType.Button1Mask) != 0,
				IsRightButtonPressed = (eventMotion.State & Gdk.ModifierType.Button2Mask) != 0
			};
	}
}
