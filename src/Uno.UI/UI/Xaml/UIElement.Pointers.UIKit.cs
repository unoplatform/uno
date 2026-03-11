// #define TRACE_NATIVE_POINTER_EVENTS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Input;
using Foundation;
using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml
{
	partial class UIElement
	{
		private class TransientNativePointer
		{
			private static readonly Dictionary<IntPtr, TransientNativePointer> _instances = new Dictionary<IntPtr, TransientNativePointer>();
			private static uint _nextAvailablePointerId;

			private readonly IntPtr _nativeId;
			private readonly HashSet<UIElement> _leases = new HashSet<UIElement>();

			public uint Id { get; }

			// The first frameId can be zero, and we're comparing it when handling
			// TouchesBegan. Setting it to -1 by default allows for handling the
			// "unset" value with a minimal performance cost.
			public long LastManagedOnlyFrameId { get; set; } = -1;

			public PointerRoutedEventArgs DownArgs { get; set; }

			public bool HadMove { get; set; }

			private TransientNativePointer(IntPtr nativeId)
			{
				_nativeId = nativeId;
				Id = _nextAvailablePointerId++;
			}

			public static TransientNativePointer Get(UIElement element, UITouch touch)
			{
				if (!_instances.TryGetValue(touch.Handle, out var id))
				{
					_instances[touch.Handle] = id = new TransientNativePointer(touch.Handle);
				}

				id._leases.Add(element);

				return id;
			}

			public void Release(UIElement element)
			{
				if (_leases.Remove(element) && _leases.Count == 0)
				{
					if (_instances.Remove(_nativeId) && _instances.Count == 0)
					{
						// When all pointers are released, we reset the pointer ID to 0.
						// This is required to detect a DoubleTap where pointer ID must be the same.
						_nextAvailablePointerId = 0;
					}
				}
			}

			public static void ReleaseAll(UIElement element)
			{
				var removed = false;
				foreach (var pointer in _instances.Values)
				{
					removed |= pointer._leases.Remove(element);
				}

				if (!removed)
				{
					return; // Nothing else to do here
				}

				foreach (var pointer in _instances.Values.ToArray())
				{
					if (pointer._leases.Count is 0)
					{
						_instances.Remove(pointer._nativeId);
					}
				}

				if (_instances.Count is 0)
				{
					_nextAvailablePointerId = 0;
				}
			}
		}

		[ThreadStatic]
		private static UIElement _sequenceReRouteTarget;
		public static void ReRoutePointerSequenceTo(UIElement target)
			=> _sequenceReRouteTarget = target;

		private IEnumerable<TouchesManager> _parentsTouchesManager;
		private bool _isManipulating;

		partial void InitializePointersPartial()
		{
#if !__TVOS__
			MultipleTouchEnabled = true;
#endif
			ArePointersEnabled = true;
		}

		partial void ClearPointerStateNative()
			=> TransientNativePointer.ReleaseAll(this);

		#region Native touch handling (i.e. source of the pointer / gesture events)
		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			if (_sequenceReRouteTarget is { } target && target != this)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
					this.Log().Debug($"Re-routing pointer sequence (implicit capture) from {this.GetDebugName()} to {target.GetDebugName()}");

				target.TouchesBegan(touches, evt, canBubbleNatively: false, forcedOriginalSource: target);
			}

			TouchesBegan(touches, evt, canBubbleNatively: true);
		}

		/// <summary>
		/// WARNING: canBubbleNatively=false on TouchesBegan has MAJOR impact regarding future events, use precautiously!
		/// (cf. remarks in the method)
		/// </summary>
		internal void TouchesBegan(NSSet touches, UIEvent evt, bool canBubbleNatively, UIElement forcedOriginalSource = null)
		{
#if TRACE_NATIVE_POINTER_EVENTS
			Console.WriteLine($"{this.GetDebugIdentifier()} [TOUCHES_BEGAN] enabled:{ArePointersEnabled}");
#endif

			if (!ArePointersEnabled)
			{
				return; // Will also prevent subsequents events
			}

			try
			{
				if (ManipulationMode == ManipulationModes.None)
				{
					// If manipulation mode is None, we make sure to disable scrollers directly on pointer pressed
					NotifyParentTouchesManagersManipulationStarted();
				}

				var isHandledOrBubblingInManaged = default(bool);
				foreach (UITouch touch in touches)
				{
					var pt = TransientNativePointer.Get(this, touch);
					var src = forcedOriginalSource ?? touch.FindOriginalSource() ?? this;
					var args = new PointerRoutedEventArgs(pt.Id, touch, evt, src) { CanBubbleNatively = canBubbleNatively };

					// We set the DownArgs only for the top most element (a.k.a. OriginalSource)
					pt.DownArgs ??= args;

					if (pt.LastManagedOnlyFrameId >= args.FrameId)
					{
						continue;
					}

					// We don't have any enter on iOS for touches, so we explicitly generate one on down.
					// That event args is requested to bubble in managed code only (args.CanBubbleNatively = false),
					// so we follow the same sequence as UWP (the whole tree gets entered before the pressed),
					// and we make sure that the event will bubble through the whole tree, no matter if the Pressed event is handle or not.
					// Note: Parents will also try to raise the "Enter" but they will be silent since the pointer is already considered as pressed.
					args.CanBubbleNatively = false;
					OnNativePointerEnter(args);

					isHandledOrBubblingInManaged |= OnNativePointerDown(args.Reset());

					if (isHandledOrBubblingInManaged)
					{
						pt.LastManagedOnlyFrameId = args.FrameId;
					}
				}

				/*
				 * If we do not propagate the "TouchesBegan" to the parents (if isHandledOrBubblingInManaged),
				 * they won't receive the "TouchesMoved" nor the "TouchesEnded". 
				 *
				 * It means that if a control (like the Button) handles the "Pressed" (or the "Entered")
				 * parent won't receive any touch event.
				 *
				 * To avoid that, we never prevent the base.TouchesBegan, but instead we keep track of the FrameId,
				 * and then in parents control filter out events that was already raised in managed.
				 */

				// Continue native bubbling up of the event
				if (canBubbleNatively)
				{
					base.TouchesBegan(touches, evt);
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
#if TRACE_NATIVE_POINTER_EVENTS
			Console.WriteLine($"{this.GetDebugIdentifier()} [TOUCHES_MOVED]");
#endif

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
#if TRACE_NATIVE_POINTER_EVENTS
			Console.WriteLine($"{this.GetDebugIdentifier()} [TOUCHES_ENDED]");
#endif

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
#if TRACE_NATIVE_POINTER_EVENTS
			Console.WriteLine($"{this.GetDebugIdentifier()} [TOUCHES_CANCELLED]");
#endif

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

		/// <summary>
		/// Used by the VisualRoot to redispatch a pointer exit on pointer up
		/// </summary>
		/// <param name="args"></param>
		internal void RedispatchPointerExited(PointerRoutedEventArgs args)
			=> OnNativePointerExited(args.Reset(canBubbleNatively: false));
		#endregion

		#region TouchesManager (Alter the parents native scroll view to make sure to receive all touches)
		partial void OnManipulationModeChanged(ManipulationModes oldMode, ManipulationModes newMode)
			// As we have to walk the tree and this method may be invoked too early, we don't try to track the state between the old and the new mode
			=> PrepareParentTouchesManagers(newMode, CanDrag);

		partial void OnCanDragChanged(bool oldValue, bool newValue)
			=> PrepareParentTouchesManagers(ManipulationMode, newValue);

		private void OnLoadedForPointers()
			=> PrepareParentTouchesManagers(ManipulationMode, CanDrag);

		private void OnUnloadedForPointers()
			=> ReleaseParentTouchesManager();

		private void PrepareParentTouchesManagers(ManipulationModes mode, bool canDrag)
		{
			// 1. Make sure to end any pending manipulation
			ReleaseParentTouchesManager();

			// 2. If this control can  Walk the tree to detect all ScrollView and register our self as a manipulation listener
			if (mode != ManipulationModes.System || canDrag)
			{
				_parentsTouchesManager = TouchesManager.GetAllParents(this).ToList();

				foreach (var manager in _parentsTouchesManager)
				{
					manager.RegisterChildListener();
				}
			}
		}

		private void ReleaseParentTouchesManager()
		{
			// 1. Make sure to end any pending manipulation
			NotifyParentTouchesManagersManipulationEnded();

			// 2. Un-register our self (so the SV can re-enable the delay)
			if (_parentsTouchesManager != null)
			{
				foreach (var manager in _parentsTouchesManager)
				{
					manager.UnRegisterChildListener();
				}

				_parentsTouchesManager = null; // prevent leak and disable manipulation started/ended reports
			}
		}

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer)
		{
			recognizer.ManipulationConfigured += (snd, manip) => NotifyParentTouchesManagersManipulationStarting(manip);
			recognizer.ManipulationStarted += (snd, args) => NotifyParentTouchesManagersManipulationStarted();

			// The manipulation can be aborted by the user before the pointer up, so the auto release on pointer up is not enough
			recognizer.ManipulationCompleted += (snd, args) => NotifyParentTouchesManagersManipulationEnded();
			recognizer.ManipulationAborted += (snd, args) => NotifyParentTouchesManagersManipulationEnded();

			// This event means that the touch was long enough and any move will actually start the manipulation,
			// so we use "Started" instead of "Starting"
			recognizer.DragReady += (snd, manip) => NotifyParentTouchesManagersManipulationStarted();
			recognizer.Dragging += (snd, args) =>
			{
				switch (args.DraggingState)
				{
					case DraggingState.Started:
						NotifyParentTouchesManagersManipulationStarted(); // Still usefull for mouse and pen
						break;
					case DraggingState.Completed:
						NotifyParentTouchesManagersManipulationEnded();
						break;
				}
			};
		}

		private void NotifyParentTouchesManagersManipulationStarting(GestureRecognizer.Manipulation manip)
		{
			if (!_isManipulating && (_parentsTouchesManager?.Any() ?? false))
			{
				foreach (var manager in _parentsTouchesManager)
				{
					_isManipulating |= manager.ManipulationStarting(manip);
				}
			}
		}

		private void NotifyParentTouchesManagersManipulationStarted()
		{
			if (!_isManipulating && (_parentsTouchesManager?.Any() ?? false))
			{
				_isManipulating = true;
				foreach (var manager in _parentsTouchesManager)
				{
					manager.ManipulationStarted();
				}
			}
		}

		private void NotifyParentTouchesManagersManipulationEnded()
		{
			if (_isManipulating && (_parentsTouchesManager?.Any() ?? false))
			{
				_isManipulating = false;
				foreach (var manager in _parentsTouchesManager)
				{
					manager.ManipulationEnded();
				}
			}
		}
		#endregion
	}
}
