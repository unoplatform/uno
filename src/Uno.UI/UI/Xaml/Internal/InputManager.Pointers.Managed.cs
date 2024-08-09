#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Windows.UI.Xaml.UIElement;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
using PointerUpdateKind = Windows.UI.Input.PointerUpdateKind;
using Windows.UI.Composition.Interactions;
using Windows.UI.Composition;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager
{
	partial void ConstructPointerManager_Managed()
	{
		// Injector supports only pointers for now, so configure only in by managed pointer
		// (should be moved to the InputManager ctor once the injector supports other input types)
		InputInjector.SetTargetForCurrentThread(this);
	}

	partial void InitializePointers_Managed(object host)
		=> Pointers.Init(host);

	partial void InjectPointerAdded(PointerEventArgs args)
		=> Pointers.InjectPointerAdded(args);

	partial void InjectPointerUpdated(PointerEventArgs args)
		=> Pointers.InjectPointerUpdated(args);

	partial void InjectPointerRemoved(PointerEventArgs args)
		=> Pointers.InjectPointerRemoved(args);

	internal partial class PointerManager
	{
		// TODO: Use pointer ID for the predicates
		private static readonly StalePredicate _isOver = new(e => e.IsPointerOver, "IsPointerOver");

		private readonly Dictionary<Pointer, UIElement> _pressedElements = new();

		private IUnoCorePointerInputSource? _source;

		// ONLY USE THIS FOR TESTING PURPOSES
		internal IUnoCorePointerInputSource? PointerInputSourceForTestingOnly => _source;

		/// <summary>
		/// Initialize the InputManager.
		/// This has to be invoked only once the host of the owning ContentRoot has been set.
		/// </summary>
		public void Init(object host)
		{
			if (!ApiExtensibility.CreateInstance(host, out _source))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(
						"Failed to initialize the PointerManager: cannot resolve the IUnoCorePointerInputSource.");
				}
				return;
			}

			if (_inputManager.ContentRoot.Type == ContentRootType.CoreWindow)
			{
				CoreWindow.GetForCurrentThreadSafe()?.SetPointerInputSource(_source);
			}

			_source.PointerMoved += (c, e) => OnPointerMoved(e);
			_source.PointerEntered += (c, e) => OnPointerEntered(e);
			_source.PointerExited += (c, e) => OnPointerExited(e);
			_source.PointerPressed += (c, e) => OnPointerPressed(e);
			_source.PointerReleased += (c, e) => OnPointerReleased(e);
			_source.PointerWheelChanged += (c, e) => OnPointerWheelChanged(e);
			_source.PointerCancelled += (c, e) => OnPointerCancelled(e);
		}

		#region Current event dispatching transaction
		private PointerDispatching? _current;

		/// <summary>
		/// Gets the currently dispatched event.
		/// </summary>
		/// <remarks>This is set only while a pointer event is currently being dispatched.</remarks>
		internal PointerRoutedEventArgs? Current => _current?.Args;

		private PointerDispatching StartDispatch(in PointerEvent evt, in PointerRoutedEventArgs args)
			=> new(this, evt, args);

		private readonly record struct PointerDispatching : IDisposable
		{
			private readonly PointerManager _manager;
			public PointerEvent Event { get; }
			public PointerRoutedEventArgs Args { get; }

			public PointerDispatching(PointerManager manager, PointerEvent @event, PointerRoutedEventArgs args)
			{
				_manager = manager;
				Args = args;
				Event = @event;

				// Before any dispatch, we make sure to reset the event to it's original state
				Debug.Assert(args.CanBubbleNatively == PointerRoutedEventArgs.PlatformSupportsNativeBubbling);
				args.Reset();

				// Set us as the current dispatching
				if (_manager._current is not null)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"A pointer is already being processed {_manager._current} while trying to raise {this}");
					}
					Debug.Fail($"A pointer is already being processed {_manager._current} while trying to raise {this}.");
				}
				_manager._current = this;

				// Then notify all external components that the dispatching is starting
				_manager._inputManager.LastInputDeviceType = args.CoreArgs.CurrentPoint.PointerDeviceType switch
				{
					PointerDeviceType.Touch => InputDeviceType.Touch,
					PointerDeviceType.Pen => InputDeviceType.Pen,
					PointerDeviceType.Mouse => InputDeviceType.Mouse,
					_ => _manager._inputManager.LastInputDeviceType
				};
				UIElement.BeginPointerEventDispatch();
			}

			public PointerEventDispatchResult End()
			{
				Dispose();
				var result = UIElement.EndPointerEventDispatch();

				// Once this dispatching has been removed from the _current dispatch (i.e. dispatch is effectively completed),
				// we re-dispatch the event to the requested target (if any)
				// Note: We create a new PointerRoutedEventArgs with a new OriginalSource == reRouted.To
				if (_manager._reRouted is { } reRouted)
				{
					_manager._reRouted = null;

					// Note: Here we are not validating the current result.VisualTreeAltered nor we perform a new hit test as we should if `true`
					// This is valid only because the single element that is able to re-route the event is the PopupRoot, which is already at the top of the visual tree.
					// When the PopupRoot performs the HitTest, the visual tree is already updated.
					if (Event == Pressed)
					{
						// Make sure to have a logical state regarding current over check use to determine if events are relevant or not
						// Note: That check should be removed for managed only events, but too massive in the context of current PR.
						result += _manager.Raise(
							Enter,
							new VisualTreeHelper.Branch(reRouted.From, reRouted.To),
							new PointerRoutedEventArgs(reRouted.Args.CoreArgs, reRouted.To) { CanBubbleNatively = false });
					}

					result += _manager.Raise(
						Event,
						new VisualTreeHelper.Branch(reRouted.From, reRouted.To),
						new PointerRoutedEventArgs(reRouted.Args.CoreArgs, reRouted.To) { CanBubbleNatively = false });
				}

				return result;
			}

			/// <inheritdoc />
			public override string ToString()
				=> $"[{Event.Name}] {Args.Pointer.UniqueId}";

			public void Dispose()
			{
				if (_manager._current == this)
				{
					_manager._current = null;
				}
			}
		}
		#endregion

		private void OnPointerWheelChanged(Windows.UI.Core.PointerEventArgs args)
		{
			if (IsRedirectedToInteractionTracker(args.CurrentPoint.PointerId))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerWheel ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerWheelChanged [{originalSource.GetDebugName()}]");
			}

#if __SKIA__ // Currently, only Skia supports interaction tracker.
			Visual? currentVisual = originalSource.Visual;
			while (currentVisual is not null)
			{
				if (currentVisual.VisualInteractionSource is { RedirectsPointerWheel: true } vis)
				{
					foreach (var tracker in vis.Trackers)
					{
						tracker.ReceivePointerWheel(args.CurrentPoint.Properties.MouseWheelDelta / global::Windows.UI.Xaml.Controls.ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, args.CurrentPoint.Properties.IsHorizontalMouseWheel);
					}

					return;
				}

				currentVisual = currentVisual.Parent;
			}
#endif

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			// First raise the event, either on the OriginalSource or on the capture owners if any
			RaiseUsingCaptures(Wheel, originalSource, routedArgs, setCursor: true);

			// Scrolling can change the element underneath the pointer, so we need to update
			(originalSource, var staleBranch) = HitTest(args, caller: "OnPointerWheelChanged_post_wheel", isStale: _isOver);
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			// Second raise the PointerExited events on the stale branch
			if (staleBranch.HasValue)
			{
				if (Raise(Leave, staleBranch.Value, routedArgs) is { VisualTreeAltered: true })
				{
					// The visual tree has been modified in a way that requires performing a new hit test.
					originalSource = HitTest(args, caller: "OnPointerWheelChanged_post_leave").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
				}
			}

			// Third (try to) raise the PointerEnter on the OriginalSource
			// Note: This won't do anything if already over.
			Raise(Enter, originalSource!, routedArgs);

			if (!PointerCapture.TryGet(routedArgs.Pointer, out var capture) || capture.IsImplicitOnly)
			{
				// If pointer is explicitly captured, then we set it in the RaiseUsingCaptures call above.
				// If not, we make sure to update the cursor based on the new originalSource.
				SetSourceCursor(originalSource);
			}
		}

		private void OnPointerEntered(Windows.UI.Core.PointerEventArgs args)
		{
			if (IsRedirectedToInteractionTracker(args.CurrentPoint.PointerId))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Enter, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerEntered ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerEntered [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			Raise(Enter, originalSource, routedArgs);
		}

		private void OnPointerExited(Windows.UI.Core.PointerEventArgs args)
		{
			if (IsRedirectedToInteractionTracker(args.CurrentPoint.PointerId))
			{
				return;
			}

			// This is how UWP behaves: when out of the bounds of the Window, the root element is used.
			var originalSource = _inputManager.ContentRoot.VisualTree.RootElement;
			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerExited ({args.CurrentPoint.Position}) Called before window content set.");
				}

				return;
			}

			var overBranchLeaf = VisualTreeHelper.SearchDownForLeaf(originalSource, _isOver);
			if (overBranchLeaf is null)
			{
				if (_trace)
				{
					Trace($"PointerExited ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerExited [{overBranchLeaf.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			Raise(Leave, overBranchLeaf, routedArgs);
			if (!args.CurrentPoint.IsInContact && (PointerDeviceType)args.CurrentPoint.Pointer.Type == PointerDeviceType.Touch)
			{
				// We release the captures on exit when pointer if not pressed
				// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
				ReleaseCaptures(routedArgs);
			}
		}

		private void OnPointerPressed(Windows.UI.Core.PointerEventArgs args)
		{
			if (TryRedirectPointerPress(args))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Pressed, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerPressed [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			_pressedElements[routedArgs.Pointer] = originalSource;
			Raise(Pressed, originalSource, routedArgs);
		}

		private void OnPointerReleased(Windows.UI.Core.PointerEventArgs args)
		{
			if (TryRedirectPointerRelease(args))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			var isOutOfWindow = originalSource is null;

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerReleased ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerReleased [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			RaiseUsingCaptures(Released, originalSource, routedArgs, setCursor: false);
			if (isOutOfWindow || (PointerDeviceType)args.CurrentPoint.Pointer.Type != PointerDeviceType.Touch)
			{
				// We release the captures on up but only after the released event and processed the gesture
				// Note: For a "Tap" with a finger the sequence is Up / Exited / Lost, so we let the Exit raise the capture lost
				ReleaseCaptures(routedArgs);

				// We only set the cursor after releasing the capture, or else the cursor will be set according to
				// the element that just lost the capture
				SetSourceCursor(originalSource);
			}
			ClearPressedState(routedArgs);
		}

		private void OnPointerMoved(Windows.UI.Core.PointerEventArgs args)
		{
			if (TryRedirectPointerMove(args))
			{
				return;
			}

			var (originalSource, staleBranch) = HitTest(args, _isOver);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerMoved ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerMoved [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			// First raise the PointerExited events on the stale branch
			if (staleBranch.HasValue)
			{
				if (Raise(Leave, staleBranch.Value, routedArgs) is { VisualTreeAltered: true })
				{
					// The visual tree has been modified in a way that requires performing a new hit test.
					originalSource = HitTest(args, caller: "OnPointerMoved_post_leave").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
				}
			}

			// Second (try to) raise the PointerEnter on the OriginalSource
			// Note: This won't do anything if already over.
			if (Raise(Enter, originalSource, routedArgs) is { VisualTreeAltered: true })
			{
				// The visual tree has been modified in a way that requires performing a new hit test.
				originalSource = HitTest(args, caller: "OnPointerMoved_post_enter").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
			}

			// Finally raise the event, either on the OriginalSource or on the capture owners if any
			RaiseUsingCaptures(Move, originalSource, routedArgs, setCursor: true);
		}

		private void OnPointerCancelled(Windows.UI.Core.PointerEventArgs args)
		{
			if (TryClearPointerRedirection(args.CurrentPoint.PointerId))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerCancelled ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerCancelled [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			RaiseUsingCaptures(Cancelled, originalSource, routedArgs, setCursor: false);
			// Note: No ReleaseCaptures(routedArgs);, the cancel automatically raise it
			SetSourceCursor(originalSource);
			ClearPressedState(routedArgs);
		}

		#region Captures
		internal void SetPointerCapture(PointerIdentifier uniqueId)
		{
			_source?.SetPointerCapture(uniqueId);
		}

		internal void ReleasePointerCapture(PointerIdentifier uniqueId)
		{
			_source?.ReleasePointerCapture(uniqueId);
		}
		#endregion

		#region Pointer injection
		internal void InjectPointerAdded(PointerEventArgs args)
			=> OnPointerEntered(args);

		internal void InjectPointerRemoved(PointerEventArgs args)
			=> OnPointerExited(args);

		internal void InjectPointerUpdated(PointerEventArgs args)
		{
			var kind = args.CurrentPoint.Properties.PointerUpdateKind;

			if (args.CurrentPoint.Properties.IsCanceled)
			{
				OnPointerCancelled(args);
			}
			else if (args.CurrentPoint.Properties.MouseWheelDelta is not 0)
			{
				OnPointerWheelChanged(args);
			}
			else if (kind is PointerUpdateKind.Other)
			{
				OnPointerMoved(args);
			}
			else if (((int)kind & 1) == 1)
			{
				OnPointerPressed(args);
			}
			else
			{
				OnPointerReleased(args);
			}
		}
		#endregion

		private void ClearPressedState(PointerRoutedEventArgs routedArgs)
		{
			if (_pressedElements.TryGetValue(routedArgs.Pointer, out var pressedLeaf))
			{
				// We must make sure to clear the pressed state on all elements that was flagged as pressed.
				// This is required as the current originalSource might not be the same as when we pressed (pointer moved),
				// ** OR ** the pointer has been captured by a parent element so we didn't raised to released on the sub elements.

				_pressedElements.Remove(routedArgs.Pointer);

				// Note: The event is propagated silently (public events won't be raised) as it's only to clear internal state
				var ctx = new BubblingContext { IsInternal = true, IsCleanup = true };
				pressedLeaf.OnPointerUp(routedArgs, ctx);
			}
		}

		#region Helpers
		private (UIElement? element, VisualTreeHelper.Branch? stale) HitTest(PointerEventArgs args, StalePredicate? isStale = null, [CallerMemberName] string caller = "")
		{
			if (_inputManager.ContentRoot.XamlRoot is null)
			{
				throw new InvalidOperationException("The XamlRoot must be properly initialized for hit testing.");
			}

			return VisualTreeHelper.HitTest(args.CurrentPoint.Position, _inputManager.ContentRoot.XamlRoot, isStale: isStale);
		}

		private delegate void RaisePointerEventArgs(UIElement element, PointerRoutedEventArgs args, BubblingContext ctx);
		private readonly record struct PointerEvent(RaisePointerEventArgs Invoke, [CallerMemberName] string Name = "");

		private static readonly PointerEvent Wheel = new((elt, args, ctx) => elt.OnPointerWheel(args, ctx));
		private static readonly PointerEvent Enter = new((elt, args, ctx) => elt.OnPointerEnter(args, ctx));
		private static readonly PointerEvent Leave = new((elt, args, ctx) => elt.OnPointerExited(args, ctx));
		private static readonly PointerEvent Pressed = new((elt, args, ctx) => elt.OnPointerDown(args, ctx));
		private static readonly PointerEvent Released = new((elt, args, ctx) => elt.OnPointerUp(args, ctx));
		private static readonly PointerEvent Move = new((elt, args, ctx) => elt.OnPointerMove(args, ctx));
		private static readonly PointerEvent Cancelled = new((elt, args, ctx) => elt.OnPointerCancel(args, ctx));

		private PointerEventDispatchResult Raise(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs)
		{
			using var dispatch = StartDispatch(evt, routedArgs);

			if (_trace)
			{
				Trace($"[Ignoring captures] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source [{originalSource.GetDebugName()}]");
			}

			evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

			return dispatch.End();
		}

		private PointerEventDispatchResult Raise(PointerEvent evt, VisualTreeHelper.Branch branch, PointerRoutedEventArgs routedArgs)
		{
			using var dispatch = StartDispatch(evt, routedArgs);

			if (_trace)
			{
				Trace($"[Ignoring captures] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to branch [{branch}]");
			}

			evt.Invoke(branch.Leaf, routedArgs, BubblingContext.BubbleUpTo(branch.Root));

			return dispatch.End();
		}

		private PointerEventDispatchResult RaiseUsingCaptures(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs, bool setCursor)
		{
			using var dispatch = StartDispatch(evt, routedArgs);

			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				var targets = capture.Targets.ToList();
				if (capture.IsImplicitOnly)
				{
					if (_trace)
					{
						Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source first [{originalSource.GetDebugName()}]");
					}

					evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (_trace)
						{
							Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{originalSource.GetDebugName()}] (-- no bubbling--)");
						}

						evt.Invoke(target.Element, routedArgs.Reset(), BubblingContext.NoBubbling);
					}

					if (setCursor)
					{
						SetSourceCursor(originalSource);
					}
				}
				else
				{
					var explicitTarget = targets.Find(c => c.Kind.HasFlag(PointerCaptureKind.Explicit))!;

					if (_trace)
					{
						Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{explicitTarget.Element.GetDebugName()}]");
					}

					evt.Invoke(explicitTarget.Element, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (target == explicitTarget)
						{
							continue;
						}

						if (_trace)
						{
							Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to alternative (implicit) target [{explicitTarget.Element.GetDebugName()}] (-- no bubbling--)");
						}

						evt.Invoke(target.Element, routedArgs.Reset(), BubblingContext.NoBubbling);
					}

					if (setCursor)
					{
						SetSourceCursor(explicitTarget.Element);
					}
				}
			}
			else
			{
				if (_trace)
				{
					Trace($"[No capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source [{originalSource.GetDebugName()}]");
				}

				evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

				if (setCursor)
				{
					SetSourceCursor(originalSource);
				}
			}

			return dispatch.End();
		}

		private void SetSourceCursor(UIElement element)
		{
#if HAS_UNO_WINUI
			if (_source is { })
			{
				if (element.CalculatedFinalCursor is { } shape)
				{
					if (_source.PointerCursor is not { } c || c.Type != shape.ToCoreCursorType())
					{
						_source.PointerCursor = InputCursor.CreateCoreCursorFromInputSystemCursorShape(shape);
					}
				}
				else
				{
					_source.PointerCursor = null;
				}
			}
#endif
		}

		private static void Trace(string text)
		{
			_log.Trace(text);
		}
		#endregion
	}
}
#endif
