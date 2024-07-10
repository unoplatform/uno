using System;
using Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using UIKit;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;

namespace Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class AppleUIKitWindow : UIWindow
{
	public override void TouchesBegan(NSSet touches, UIEvent? evt)
	{
		try
		{
			foreach (UITouch touch in touches)
			{

			}
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	public override void TouchesMoved(NSSet touches, UIEvent evt)
	{
		if (_sequenceReRouteTarget is { } target && target != this)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug($"Re-routing pointer sequence (implicit capture) from {this.GetDebugName()} to {target.GetDebugName()}");

			target.TouchesMoved(touches, evt, canBubbleNatively: false, forcedOriginalSource: target);
		}

		TouchesMoved(touches, evt, canBubbleNatively: true);
	}

	internal void TouchesMoved(NSSet touches, UIEvent evt, bool canBubbleNatively, UIElement forcedOriginalSource = null)
	{
		f TRACE_NATIVE_POINTER_EVENTS
		Console.WriteLine($"{this.GetDebugIdentifier()} [TOUCHES_MOVED]");
		ndif


	try
		{
			var isHandledOrBubblingInManaged = default(bool);
			foreach (UITouch touch in touches)
			{
				var pt = TransientNativePointer.Get(this, touch);
				var src = forcedOriginalSource ?? touch.FindOriginalSource() ?? this;
				var args = new PointerRoutedEventArgs(pt.Id, touch, evt, src) { CanBubbleNatively = canBubbleNatively };
				var isPointerOver = touch.IsTouchInView(this);

				// This is acceptable to keep that flag in a kind-of static way, since iOS do "implicit captures",
				// a potential move will be dispatched to all elements "registered" on this "TransientNativePointer".
				pt.HadMove = true;

				// As we don't have enter/exit equivalents on iOS, we have to update the IsOver on each move
				// Note: Entered / Exited are raised *before* the Move (Checked using the args timestamp)
				isHandledOrBubblingInManaged |= OnNativePointerMoveWithOverCheck(args, isPointerOver);
			}

			if (canBubbleNatively && !isHandledOrBubblingInManaged)
			{
				// Continue native bubbling up of the event
				base.TouchesMoved(touches, evt);
			}
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	public override void TouchesEnded(NSSet touches, UIEvent evt)
	{
		if (_sequenceReRouteTarget is { } target && target != this)
		{
			_sequenceReRouteTarget = null;

			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug($"Re-routing pointer sequence (implicit capture) from {this.GetDebugName()} to {target.GetDebugName()}");

			target.TouchesEnded(touches, evt, canBubbleNatively: false, forcedOriginalSource: target);
		}

		TouchesEnded(touches, evt, canBubbleNatively: true);
	}

	internal void TouchesEnded(NSSet touches, UIEvent evt, bool canBubbleNatively, UIElement forcedOriginalSource = null)
	{
		f TRACE_NATIVE_POINTER_EVENTS
		Console.WriteLine($"{this.GetDebugIdentifier()} [TOUCHES_ENDED]");
		ndif

	/* Note: Here we have a mismatching behavior with UWP, if the events bubble natively we're going to get
			 (with Ctrl_02 is a child of Ctrl_01):
					Ctrl_02: Released
							 Exited
					Ctrl_01: Released
							 Exited

			While on UWP we will get:
					Ctrl_02: Released
					Ctrl_01: Released
					Ctrl_02: Exited
					Ctrl_01: Exited

			However, to fix this is would mean that we handle all events in managed code, but this would
			break lots of control (ScrollViewer) and ability to easily integrate an external component.
	*/

	try
		{
			var isHandledOrBubblingInManaged = default(bool);
			foreach (UITouch touch in touches)
			{
				var pt = TransientNativePointer.Get(this, touch);
				var src = forcedOriginalSource ?? touch.FindOriginalSource() ?? this;
				var args = new PointerRoutedEventArgs(pt.Id, touch, evt, src) { CanBubbleNatively = canBubbleNatively };

				if (!pt.HadMove)
				{
					// The event will bubble in managed, so as this flag is "pseudo static", make sure to raise it only once.
					pt.HadMove = true;

					// On iOS if the gesture is really fast (like a flick), we can get only 'down' and 'up'.
					// But on UWP it seems that we always have a least one move (for fingers and pen!), and even internally,
					// the manipulation events are requiring at least one move to kick-in.
					// Here we are just making sure to raise that event with the final location.
					// Note: In case of multi-touch we might raise it unnecessarily, but it won't have any negative impact.
					// Note: We do not consider the result of that move for the 'isHandledOrBubblingInManaged'
					//		 as it's kind of un-related to the 'up' itself.
					var mixedArgs = new PointerRoutedEventArgs(previous: pt.DownArgs, current: args) { CanBubbleNatively = canBubbleNatively };
					OnNativePointerMove(mixedArgs);
				}

				isHandledOrBubblingInManaged |= OnNativePointerUp(args);

				if (isHandledOrBubblingInManaged)
				{
					// Like for the Down, we need to manually generate an Exited.
					// This is expected to be done by the RootVisual, except if the "up" has been handled
					// (in order to ensure the "up" has been fully processed, including gesture recognition).
					// In that case we need to sent it by our-own directly from teh element that has handled the event.
					XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.ProcessPointerUp(args, isAfterHandledUp: true);
				}

				pt.Release(this);
			}

			if (canBubbleNatively && !isHandledOrBubblingInManaged)
			{
				// Continue native bubbling up of the event
				base.TouchesEnded(touches, evt);
			}

			NotifyParentTouchesManagersManipulationEnded();
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	public override void TouchesCancelled(NSSet touches, UIEvent evt)
	{
		if (_sequenceReRouteTarget is { } target && target != this)
		{
			_sequenceReRouteTarget = null;

			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug($"Re-routing pointer sequence (implicit capture) from {this.GetDebugName()} to {target.GetDebugName()}");

			target.TouchesCancelled(touches, evt, canBubbleNatively: false, forcedOriginalSource: target);
		}

		TouchesCancelled(touches, evt, canBubbleNatively: true);
	}

	internal void TouchesCancelled(NSSet touches, UIEvent evt, bool canBubbleNatively, UIElement forcedOriginalSource = null)
	{
		f TRACE_NATIVE_POINTER_EVENTS
		Console.WriteLine($"{this.GetDebugIdentifier()} [TOUCHES_CANCELLED]");
		ndif


	try
		{
			var isHandledOrBubblingInManaged = default(bool);
			foreach (UITouch touch in touches)
			{
				var pt = TransientNativePointer.Get(this, touch);
				var src = forcedOriginalSource ?? touch.FindOriginalSource() ?? this;
				var args = new PointerRoutedEventArgs(pt.Id, touch, evt, src) { CanBubbleNatively = canBubbleNatively };

				// Note: We should have raise either PointerCaptureLost or PointerCancelled here depending of the reason which
				//		 drives the system to bubble a lost. However we don't have this kind of information on iOS, and it's
				//		 usually due to the ScrollView which kicks in. So we always raise the CaptureLost which is the behavior
				//		 on UWP when scroll starts (even if no capture are actives at this time).

				isHandledOrBubblingInManaged |= OnNativePointerCancel(args, isSwallowedBySystem: true);

				pt.Release(this);
			}

			if (canBubbleNatively && !isHandledOrBubblingInManaged)
			{
				// Continue native bubbling up of the event
				base.TouchesCancelled(touches, evt);
			}

			NotifyParentTouchesManagersManipulationEnded();
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private PointerEventArgs CreatePointerEventArgs(UITouch touch)
	{
		var position = touch.LocationInView(this);
		var pointerDeviceType = touch.Type.ToPointerDeviceType();
		var isInContact =
			touch.Phase == UITouchPhase.Began
			|| touch.Phase == UITouchPhase.Moved
			|| touch.Phase == UITouchPhase.Stationary;
		var pointerDevice = PointerDevice.For(pointerDeviceType);
		var pointerIdentifier = new PointerIdentifier(pointerDeviceType, 0); // TODO:MZ: Pointer identifiers
		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = touch.Type == UITouchType.Indirect,
			IsRightButtonPressed = touch.Type == UITouchType.Direct,
			IsMiddleButtonPressed = false,
			IsXButton1Pressed = false,
			IsXButton2Pressed = false,
			PointerUpdateKind = isInContact ? PointerUpdateKind.Other : PointerUpdateKind.Canceled,
			PointerUpdateKind = PointerUpdateKind.Other,
			IsBarrelButtonPressed = false,
			IsEraser = false,
			IsHorizontalMouseWheel = false,
			IsPrimary = true,
			IsInRange = true,
			IsTouch = true,
			Orientation = 0,
			Pressure = touch.Force,
			TouchConfidence = isInContact ? TouchConfidence.High : TouchConfidence.Canceled,
			Twist = 0,
			Velocity = new Windows.Foundation.Point(0, 0),
		};

		var frameId = PointerHelpers.ToFrameId(touch.Timestamp);
		var timestamp = PointerHelpers.ToTimeStamp(touch.Timestamp);
		var pointerPoint = new PointerPoint(
			frameId,
			timestamp,
			pointerDevice,
			pointerIdentifier.Id,
			position.ToPoint(),
			position.ToPoint(),
			isInContact,
			properties);
		return new PointerEventArgs(pointerPoint);
	}

}
