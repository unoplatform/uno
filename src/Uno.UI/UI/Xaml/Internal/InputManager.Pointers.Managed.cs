#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.UIElement;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
using PointerUpdateKind = Microsoft.UI.Input.PointerUpdateKind;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Composition;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager
{
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
					this.Log().Error("Failed to initialize the PointerManager: cannot resolve the IUnoCorePointerInputSource.");
				}
				return;
			}

			CoreWindow.GetForCurrentThreadSafe()?.SetPointerInputSource(_source);

			_source.PointerMoved += (c, e) =>
			{
				try
				{
					OnPointerMoved(e);
				}
				catch (Exception error)
				{
					OnTopLevelFatalError(nameof(OnPointerMoved), error);
				}
			};
			_source.PointerEntered += (c, e) =>
			{
				try
				{
					OnPointerEntered(e);
				}
				catch (Exception error)
				{
					OnTopLevelFatalError(nameof(OnPointerEntered), error);
				}
			};
			_source.PointerExited += (c, e) =>
			{
				try
				{
					OnPointerExited(e);
				}
				catch (Exception error)
				{
					OnTopLevelFatalError(nameof(OnPointerExited), error);
				}
			};
			_source.PointerPressed += (c, e) =>
			{
				try
				{
					OnPointerPressed(e);
				}
				catch (Exception error)
				{
					OnTopLevelFatalError(nameof(OnPointerPressed), error);
				}
			};
			_source.PointerReleased += (c, e) =>
			{
				try
				{
					OnPointerReleased(e);
				}
				catch (Exception error)
				{
					OnTopLevelFatalError(nameof(OnPointerReleased), error);
				}
			};
			_source.PointerWheelChanged += (c, e) =>
			{
				try
				{
					OnPointerWheelChanged(e);
				}
				catch (Exception error)
				{
					OnTopLevelFatalError(nameof(OnPointerWheelChanged), error);
				}
			};
			_source.PointerCancelled += (c, e) =>
			{
				try
				{
					OnPointerCancelled(e);
				}
				catch (Exception error)
				{
					OnTopLevelFatalError(nameof(OnPointerCancelled), error);
				}
			};
		}

		private void OnTopLevelFatalError(string evt, Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Critical))
			{
				this.Log().Critical($"""
					Critical error while handling pointer event '{evt}'.
					This is a top level error handler to prevent application crash, but error is not recoverable and pointers are expected to work erratically starting from now.
					Application restart is required to continue.
					Please report issue to the team.
					""",
					error);
			}

			// Attempt to recover by clearing all states that can prevent normal pointer event dispatching.
			try
			{
				_directManipulations.ClearForFatalError();
				_gestureRecognizers.ClearForFataError();
				PointerCapture.ClearForFatalError();
			}
			catch { }
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
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"A pointer is already being processed {_manager._current} while trying to raise {this}");
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

		// Cached hit-test result for iOS trackpad scroll.
		// During continuous trackpad scroll the cursor position doesn't change — only content
		// moves underneath — so we cache the originalSource from the first hit-test and reuse
		// it for subsequent wheel events at the same position. This avoids a ~17ms visual tree
		// traversal on every single scroll event.
		private UIElement? _cachedWheelHitTestSource;
		private Point _cachedWheelHitTestPosition;

		private void OnPointerWheelChanged(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			if (IsRedirectedToManipulations(args.CurrentPoint.Pointer))
			{
				TraceIgnoredForManipulations(args);
				return;
			}

			UIElement? originalSource;
			var pointerPosition = args.CurrentPoint.Position;

			// On iOS, reuse the cached hit-test result when the pointer position hasn't changed
			// AND the cached element is still in the visual tree. During continuous trackpad
			// scroll the cursor stays still; only the content moves. If the cached element
			// has been virtualized away (e.g. during a long scroll through a ListView),
			// we must re-hit-test so the event routes to an element that can still bubble
			// to the ScrollViewer.
			if (OperatingSystem.IsIOS()
				&& _cachedWheelHitTestSource is { IsLoaded: true }
				&& pointerPosition == _cachedWheelHitTestPosition)
			{
				originalSource = _cachedWheelHitTestSource;
			}
			else
			{
				if (!HitTestOrRoot(args, out originalSource))
				{
					TraceIgnoredAsNoTree(args);
					return;
				}

				if (originalSource is null)
				{
					TraceIgnoredAsNoTree(args);
					return;
				}

				if (OperatingSystem.IsIOS())
				{
					_cachedWheelHitTestSource = originalSource;
					_cachedWheelHitTestPosition = pointerPosition;
				}
			}

			TraceHandling(originalSource);

#if __SKIA__ // Currently, only Skia supports interaction tracker.
			Visual? currentVisual = originalSource.Visual;
			while (currentVisual is not null)
			{
				if (currentVisual.VisualInteractionSource is { RedirectsPointerWheel: true } vis)
				{
					foreach (var tracker in vis.Trackers)
					{
						// Not an integer division: touchpads report deltas finer than one detent.
						tracker.ReceivePointerWheel((double)args.CurrentPoint.Properties.MouseWheelDelta / global::Microsoft.UI.Xaml.Controls.ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, args.CurrentPoint.Properties.IsHorizontalMouseWheel);
					}

					return;
				}

				currentVisual = currentVisual.Parent;
			}
#endif

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			// First raise the event, either on the OriginalSource or on the capture owners if any
			var result = RaiseUsingCaptures(Wheel, originalSource, routedArgs, setCursor: true);

			// On iOS (trackpad scroll), skip the expensive post-scroll hit-test during active scrolling.
			// The pointer hasn't physically moved — only the content underneath has scrolled.
			// Hover state will be reconciled when the next PointerMoved event fires (user moves cursor).
			if (!OperatingSystem.IsIOS())
			{
				// Scrolling can change the element underneath the pointer, so we need to update over state.
				// On iOS (trackpad/wheel scroll), skip the post-scroll hit-test during active scrolling —
				// the pointer hasn't physically moved, only the content underneath has scrolled.
				// Hover state will be reconciled when the next PointerMoved fires (user moves cursor).
				HitTestOrRoot(args, _isOver, out originalSource, out var staleBranch, reason: "after_wheel");
				result += RaiseLeaveEnter(routedArgs, staleBranch, ref originalSource!, needsNonStaleOriginalSource: false);
			}

			if (!PointerCapture.TryGet(routedArgs.Pointer, out var capture) || capture.IsImplicitOnly)
			{
				// If pointer is explicitly captured, then we set it in the RaiseUsingCaptures(Wheel) call above.
				// If not, we make sure to update the cursor based on the new originalSource.
				SetSourceCursor(originalSource);
			}

			args.DispatchResult = result;
		}

		private void OnPointerEntered(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			if (BeforeEnterTryRedirectToManipulation(args))
			{
				TraceIgnoredForManipulations(args);
				return;
			}

			if (!HitTestOrRoot(args, out var originalSource))
			{
				TraceIgnoredAsNoTree(args);
				return;
			}

			TraceHandling(originalSource);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			var result = Raise(Enter, originalSource, routedArgs);

			args.DispatchResult = result;
		}

		private void OnPointerExited(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			_cachedWheelHitTestSource = null;

			if (IsRedirectedToManipulations(args.CurrentPoint.Pointer))
			{
				TraceIgnoredForManipulations(args);
				return;
			}

			// This is how UWP behaves: when out of the bounds of the Window, the root element is used.
			var originalSource = _inputManager.ContentRoot.VisualTree.RootElement;
			if (originalSource is null)
			{
				TraceIgnoredAsNoTree(args);
				return;
			}

			var overBranchLeaf = VisualTreeHelper.SearchDownForLeaf(originalSource, _isOver);

			TraceHandling(overBranchLeaf); // overBranchLeaf is the real originalSource, so we prefer to trace using it!

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			var result = Raise(Leave, overBranchLeaf, routedArgs);

			if (!args.CurrentPoint.IsInContact && (PointerDeviceType)args.CurrentPoint.Pointer.Type == PointerDeviceType.Touch)
			{
				// We release the captures on exit when pointer is not pressed
				// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
				result += ReleaseCaptures(routedArgs);
			}

			args.DispatchResult = result;
		}

		private void OnPointerPressed(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			// If 2+ mouse buttons are pressed, we only respond to the first.
			if (args.CurrentPoint is { PointerDeviceType: PointerDeviceType.Mouse, Properties.HasMultipleButtonsPressed: true })
			{
				Trace("Mouse second button pressed ignored!");
				return;
			}

			if (BeforePressTryRedirectToManipulations(args))
			{
				TraceIgnoredForManipulations(args);
				return;
			}

			if (args.CurrentPoint.Properties.IsRightButtonPressed)
			{
				_rightButtonPointerId = args.CurrentPoint.PointerId;
			}

			_isPointerDownHitTest = true;
			var hasTree = HitTestOrRoot(args, out var hitTestSource);
			_isPointerDownHitTest = false;

			if (!hasTree || hitTestSource is not { } originalSource)
			{
				TraceIgnoredAsNoTree(args);
				return;
			}

			TraceHandling(originalSource);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };
			var result = default(PointerEventDispatchResult);

			// First (try to) raise the PointerEnter on the OriginalSource (this won't do anything if already over).
			result += Raise(Enter, originalSource, routedArgs);
			HitTestIfStale(result, routedArgs, ref originalSource, "after_enter");

			// Second raise the PointerPressed event
			_pressedElements[routedArgs.Pointer] = originalSource;
			result += Raise(Pressed, originalSource, routedArgs);

			AfterPressForDirectManipulation(args);

			args.DispatchResult = result;
		}

		private void OnPointerReleased(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			_releaseDepth++;
			try
			{
				OnPointerReleasedCore(args, isInjected);
			}
			finally
			{
				if (--_releaseDepth == 0 && _afterRelease is { } afterRelease)
				{
					_afterRelease = null;
					if (_trace)
					{
						Trace($"Running {afterRelease.Count} action(s) deferred until the {Released.Name} has completed");
					}

					foreach (var action in afterRelease)
					{
						RunDeferred(action, isGesture: false);
					}
				}
			}
		}

		private void OnPointerReleasedCore(Windows.UI.Core.PointerEventArgs args, bool isInjected)
		{
			// When multiple mouse buttons are pressed and then released, we only respond to the last OnPointerReleased
			// (i.e when no more buttons are still pressed).
			if (args.CurrentPoint.PointerDeviceType == PointerDeviceType.Mouse && args.CurrentPoint.IsInContact)
			{
				Trace("Mouse second button released ignored!");
				return;
			}

			if (BeforeReleaseTryRedirectToManipulations(args))
			{
				TraceIgnoredForManipulations(args);
				return;
			}

			var overStaleBranch = default(VisualTreeHelper.Branch?);
			var isOutOfWindow = (PointerDeviceType)args.CurrentPoint.Pointer.Type is PointerDeviceType.Touch
				? !HitTest(args, out var originalSource) // No need to find the stale branch for touch as we will flush the over state on the whole tree
				: !HitTest(args, _isOver, out originalSource, out overStaleBranch);
			if (_rightButtonPointerId == args.CurrentPoint.PointerId)
			{
				_rightButtonPointerId = null;
			}

			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;
			if (originalSource is null) // Note: Theoretically impossible for the Release, but for safety
			{
				TraceIgnoredAsNoTree(args);
				return;
			}

			TraceHandling(originalSource);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };
			var hadCapture = PointerCapture.TryGet(args.CurrentPoint.Pointer, out var capture) && capture.IsImplicitOnly is false;

			var result = RaiseReleasedThenGestures(originalSource, routedArgs);

			if (isOutOfWindow || (PointerDeviceType)args.CurrentPoint.Pointer.Type != PointerDeviceType.Touch)
			{
				// We release the captures on up but only after the released event and processed the gesture
				// Note: For a "Tap" with a finger the sequence is Up / Exited / Lost, so we let the Exit raise the capture lost
				result += ReleaseCaptures(routedArgs);

				// We only set the cursor after releasing the capture, or else the cursor will be set according to
				// the element that just lost the capture
				SetSourceCursor(originalSource);
			}

			result += CleanPressedState(routedArgs);

			switch ((PointerDeviceType)args.CurrentPoint.Pointer.Type)
			{
				case PointerDeviceType.Touch:
					// For touch we need to raise the PointerExited event on all the part of the tree that is pointer over, not only those considered as stale!
					var overLeaf = VisualTreeHelper.SearchDownForLeaf(
						args.RelativeRoot as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement,
						_isOver);
					result += Raise(Leave, overLeaf, routedArgs);
					break;

				// If we had capture, we might not have raise the pointer enter on the originalSource.
				// Make sure to raise it now the pointer has been release / capture has been removed (for pointers that supports overing).
				case PointerDeviceType.Mouse when hadCapture:
				case PointerDeviceType.Pen when hadCapture && args.CurrentPoint.Properties.IsInRange:
					if (result.VisualTreeAltered)
					{
						// Keep `root` as the real visual-tree root (used for the Branch endpoint).
						// Scope ONLY the leaf search to RelativeRoot so injected events bypass
						// design-time overlays without distorting the branch walked by Leave/Enter.
						var root = _inputManager.ContentRoot.VisualTree.RootElement;
						overStaleBranch = new(root, VisualTreeHelper.SearchDownForLeaf(args.RelativeRoot as UIElement ?? root, _isOver));
					}
					result += RaiseLeaveEnter(routedArgs, overStaleBranch, ref originalSource, needsNonStaleOriginalSource: false);
					break;
			}

			AfterReleaseForManipulations(args);

			args.DispatchResult = result;
		}

		private PendingRelease? _pendingRelease;

		// What is held back while PointerReleased is bubbling, until it has reached the root (cf. RaiseReleasedThenGestures).
		private sealed class PendingRelease
		{
			public List<Action>? Gestures;
			public PointerRoutedEventArgs? UnhandledUp;
		}

		/// <summary>
		/// Defers a gesture recognized while PointerReleased is bubbling, to raise it once the release has reached the root.
		/// </summary>
		/// <returns>False if no PointerReleased is being raised, in which case the gesture must be raised right away.</returns>
		internal bool TryDeferUntilReleaseBubbled(Action raiseGesture)
		{
			if (_pendingRelease is null)
			{
				return false;
			}

			(_pendingRelease.Gestures ??= new()).Add(raiseGesture);
			return true;
		}

		private int _releaseDepth;
		private List<Action>? _afterRelease;

		/// <summary>
		/// Defers an action until the pointer release being processed (the outermost one if they are nested) has completed: PointerReleased bubbled,
		/// the gestures it ended were raised, the root cleaned up the focus and the captures, and the exit events were raised.
		/// </summary>
		/// <remarks>The action must not alter the visual tree: it runs once the dispatch result of the release has been computed.</remarks>
		/// <returns>False if no release is being processed, in which case the action must be run right away.</returns>
		internal bool TryDeferUntilReleaseCompleted(Action action)
		{
			if (_releaseDepth == 0)
			{
				return false;
			}

			(_afterRelease ??= new()).Add(action);
			return true;
		}

		// Like WinUI (cf. CInputServices::RaiseDelayedPointerUpEvent and CleanPointerProcessingState), Tapped and RightTapped are raised only once
		// PointerReleased has bubbled to the root, so ancestors (e.g. a ScrollViewer taking focus) handle the release before a gesture handler runs,
		// and the root cleans up the focus and the captures only after those gestures.
		private PointerEventDispatchResult RaiseReleasedThenGestures(UIElement originalSource, PointerRoutedEventArgs routedArgs)
		{
			var outer = _pendingRelease;
			var pending = _pendingRelease = new PendingRelease();
			PointerEventDispatchResult result;
			try
			{
				result = RaiseUsingCaptures(Released, originalSource, routedArgs, setCursor: false);
			}
			finally
			{
				_pendingRelease = outer;
			}

			if (pending.Gestures is { } gestures)
			{
				if (_trace)
				{
					Trace($"Raising {gestures.Count} gesture(s) deferred until {Released.Name} has bubbled (args: {routedArgs.GetHashCode():X8})");
				}

				// Gesture handlers can alter the visual tree just like the PointerReleased ones.
				UIElement.BeginPointerEventDispatch();
				foreach (var raiseGesture in gestures)
				{
					RunDeferred(raiseGesture, isGesture: true);
				}
				result += UIElement.EndPointerEventDispatch();
			}

			if (pending.UnhandledUp is { } unhandledUp)
			{
				result += CompletePointerUp(unhandledUp, isHandled: false, isAfterHandledUp: false);
			}

			return result;
		}

		// Like an error in a PointerReleased handler (cf. UIElement.RaisePointerEvent), an error in a deferred gesture handler or action is only logged.
		private static void RunDeferred(Action action, bool isGesture)
		{
			try
			{
				action();
			}
			catch (Exception error)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error(
						isGesture
							? $"Failed to raise a gesture deferred until the {Released.Name} has bubbled."
							: $"Failed to run an action deferred until the {Released.Name} has completed.",
						error);
				}
			}
		}

		private void OnPointerMoved(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			// Clear wheel hit-test cache — pointer has moved, so the cached source
			// is stale. PointerMoved already does a full hit-test + RaiseLeaveEnter
			// which will reconcile hover state.
			_cachedWheelHitTestSource = null;

			if (BeforeMoveTryRedirectToManipulations(args))
			{
				TraceIgnoredForManipulations(args);
				return;
			}

			if (!HitTestOrRoot(args, _isOver, out var originalSource, out var overStaleBranch))
			{
				TraceIgnoredAsNoTree(args);
				return;
			}

			TraceHandling(originalSource);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };
			var result = default(PointerEventDispatchResult);

			// Raise enter/exit events
			result += RaiseLeaveEnter(routedArgs, overStaleBranch, ref originalSource, needsNonStaleOriginalSource: true);

			// Finally raise the event, either on the OriginalSource or on the capture owners if any
			result += RaiseUsingCaptures(Move, originalSource, routedArgs, setCursor: true);

			AfterMoveForManipulations(args);

			args.DispatchResult = result;
		}

		private void OnPointerCancelled(PointerEventArgs args, bool isInjected = false)
		{
			if (BeforeCancelTryRedirectToManipulations(args))
			{
				TraceIgnoredForManipulations(args);
				return;
			}

			CancelPointer(args, isInjected: isInjected);

			AfterCancelForManipulations(args);
		}

		internal void CancelPointer(PointerEventArgs args, bool isInjected = false, bool isDirectManipulation = false, bool isDirectManipulationResume = false)
		{
			if (!HitTestOrRoot(args, _isOver, out var originalSource, out var overStaleBranch))
			{
				TraceIgnoredAsNoTree(args);
				return;
			}

			TraceHandling(originalSource);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource)
			{
				IsInjected = isInjected,
				CanceledByDirectManipulation = isDirectManipulation
			};
			var result = default(PointerEventDispatchResult);

			// First we make sure to update the over state (i.e. leave!) on the branch that is no longer under the pointer
			// (The move that trigger the direct manipulation to kick-in might include element boundaries traversal)
			result += RaiseLeave(routedArgs, overStaleBranch, ref originalSource);

			if (!isDirectManipulationResume) // Before resuming a direct manipulation we might have let dispatch only pointer enter events
			{
				// Then we raise the cancelled event on element directly under the pointer.
				// (This will also raise the leave on the "main" branch)
				result += RaiseUsingCaptures(Cancelled, originalSource, routedArgs, setCursor: false);

				// Finally we also make sure to clean the pressed state if any branch was not detected.
				result += CleanPressedState(routedArgs);
			}

			// Note: No ReleaseCaptures(routedArgs);, the cancel automatically raise it
			SetSourceCursor(originalSource);

			args.DispatchResult = result;
		}

		#region Captures
		internal void SetPointerCapture(PointerIdentifier uniqueId)
		{
			if (_trace)
			{
				Trace($"[Capture] Capturing pointer {uniqueId}.");
			}
			_source?.SetPointerCapture(uniqueId);
		}

		internal void ReleasePointerCapture(PointerIdentifier uniqueId)
		{
			if (_trace)
			{
				Trace($"[Capture] Releasing pointer {uniqueId}.");
			}
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
				OnPointerCancelled(args, isInjected: true);
			}
			else if (args.CurrentPoint.Properties.MouseWheelDelta is not 0)
			{
				OnPointerWheelChanged(args, isInjected: true);
			}
			else if (kind is PointerUpdateKind.Other)
			{
				OnPointerMoved(args, isInjected: true);
			}
			else if (((int)kind & 1) == 1)
			{
				OnPointerPressed(args, isInjected: true);
			}
			else
			{
				OnPointerReleased(args, isInjected: true);
			}
		}
		#endregion

		private PointerEventDispatchResult CleanPressedState(PointerRoutedEventArgs routedArgs)
		{
			var result = default(PointerEventDispatchResult);

			if (_pressedElements.Remove(routedArgs.Pointer, out var pressedLeaf))
			{
				// We must make sure to clear the pressed state on all elements that was flagged as pressed.
				// This is required as the current originalSource might not be the same as when we pressed (pointer moved),
				// ** OR ** the pointer has been captured by a parent element so we didn't raised to released on the sub elements.

				// Note: The event is propagated silently (public events won't be raised) as it's only to clear internal state
				var ctx = new BubblingContext { IsInternal = false, IsCleanup = true };
				result = Raise(Released, pressedLeaf, routedArgs, ctx);
			}

			return result;
		}

		private PointerEventDispatchResult RaiseLeave(
			PointerRoutedEventArgs args,
			VisualTreeHelper.Branch? overStaleBranch,
			ref UIElement originalSource,
			[CallerMemberName] string entryPoint = "",
			[CallerLineNumber] int entryLine = -1)
		{
			var result = default(PointerEventDispatchResult);

			if (overStaleBranch is { } branch)
			{
				result = Raise(Leave, branch.Leaf, args);
				HitTestIfStale(result, args, ref originalSource, nameof(RaiseLeave), entryPoint, entryLine);
			}

			return result;
		}

		private PointerEventDispatchResult RaiseLeaveEnter(
			PointerRoutedEventArgs args,
			VisualTreeHelper.Branch? overStaleBranch,
			ref UIElement originalSource,
			bool needsNonStaleOriginalSource,
			[CallerMemberName] string entryPoint = "",
			[CallerLineNumber] int entryLine = -1)
		{
			var result = default(PointerEventDispatchResult);
			var isOriginalSourceStale = false;

			if (PointerCapture.TryGet(args.CoreArgs.CurrentPoint.Pointer, out var capture) && capture.IsImplicitOnly is false)
			{
				// If the pointer is captured, we want to publicly raise the enter / leave event ONLY on the capture element itself, silently on all others.

				var captureTarget = capture.ExplicitTarget!;
				var isLeavingCaptureElementBounds = overStaleBranch?.Contains(captureTarget) is true;
				var isEnteringCaptureElementBounds = !isLeavingCaptureElementBounds && VisualTreeHelper.Branch.ToPublicRoot(originalSource).Contains(captureTarget);

				// #1 LEAVE
				if (isLeavingCaptureElementBounds)
				{
					// First we raise **publicly** the leave event on the capture target
					var leaveCaptureCtx = new BubblingContext { Mode = BubblingMode.IgnoreParents };
					var leaveCaptureResult = Raise(Leave, captureTarget, args, leaveCaptureCtx);

					// If this fails, it means we have to update HitTest to capture the list of elements onto which the event should be raised,
					// and update the event bubbling logic to use that list instead of relying on dynamic parent discovery!
					Debug.Assert(leaveCaptureResult.VisualTreeAltered is false);

					isOriginalSourceStale |= leaveCaptureResult.VisualTreeAltered;
					result += leaveCaptureResult;
				}
				if (overStaleBranch is not null)
				{
					// Second, we raise again the leave but starting from the originalSource and **silently**.
					// This is to make sure to not leave element flagged as IsOver = true.
					// Note: This means that, public listener of the exit events will **never** be raised for those elements.
					// Note 2: This will try to also raise the exited event on the capture.ExplicitTarget (if any), but this will have no effect as we already did it!
					var leaveCtx = new BubblingContext { IsCleanup = true, IsInternal = false, Mode = BubblingMode.Bubble, Root = overStaleBranch.Value.Root };
					var leaveResult = Raise(Leave, overStaleBranch.Value.Leaf, args, leaveCtx);
					isOriginalSourceStale |= leaveResult.VisualTreeAltered;
					result += leaveResult;
				}

				// #1.1 - Refresh OriginalSource
				if (isOriginalSourceStale)
				{
					HitTestOrRoot(args.CoreArgs, out originalSource!, reason: "after_leave_with_capture", entryPoint, entryLine);
					isOriginalSourceStale = false;
				}

				// #2 ENTER
				if (isEnteringCaptureElementBounds)
				{
					// Note: we set the flag IsOver true **ONLY** for the capture element branch
					//		 for all other elements, we wait for the PointerReleased

					// First we raise **publicly** the enter event on the capture target
					var enterCaptureCtx = new BubblingContext { Mode = BubblingMode.IgnoreParents };
					var enterCaptureResult = Raise(Enter, captureTarget, args, enterCaptureCtx);

					// If this fails, it means we have to update HitTest to capture the list of elements tonto which the event should be raised,
					// and update the event bubbling logic to use that list instead of relying on dynamic parent discovery!
					Debug.Assert(enterCaptureResult.VisualTreeAltered is false);

					isOriginalSourceStale |= enterCaptureResult.VisualTreeAltered;
					result += enterCaptureResult;

					// Second we make sure to also flag as IsOver true all elements in the branch
					var enterCtx = new BubblingContext { IsCleanup = true, IsInternal = false, Mode = BubblingMode.Bubble };
					var enterResult = Raise(Enter, originalSource, args, enterCtx);
					isOriginalSourceStale |= enterCaptureResult.VisualTreeAltered;
					result += enterResult;
				}
			}
			else
			{
				// #1 LEAVE
				if (overStaleBranch is { } branch)
				{
					var leaveResult = Raise(Leave, branch, args);
					isOriginalSourceStale |= leaveResult.VisualTreeAltered;
					result += leaveResult;
				}

				// #1.1 - Refresh OriginalSource
				if (isOriginalSourceStale)
				{
					HitTestOrRoot(args.CoreArgs, out originalSource!, reason: "after_leave", entryPoint, entryLine);
					isOriginalSourceStale = false;
				}

				// #2 ENTER
				var enterResult = Raise(Enter, originalSource, args);
				isOriginalSourceStale |= enterResult.VisualTreeAltered;
				result += enterResult;
			}

			// #2.1 - Refresh OriginalSource
			if (isOriginalSourceStale && needsNonStaleOriginalSource)
			{
				HitTestOrRoot(args.CoreArgs, out originalSource!, reason: "after_enter", entryPoint, entryLine);
			}

			return result;
		}

		#region Helpers

		private void HitTestIfStale(
			PointerEventDispatchResult result,
			PointerRoutedEventArgs args,
			ref UIElement originalSource,
			string reason,
			[CallerMemberName] string entryPoint = "",
			[CallerLineNumber] int entryLine = -1)
		{
			if (result is { VisualTreeAltered: true })
			{
				// The visual tree has been modified in a way that requires performing a new hit test.
				// Note: we mute the nullable annotation for the originalSource as at this point it's impossible to return null.
				HitTestOrRoot(args.CoreArgs, out originalSource!, reason, entryPoint, entryLine);

				// TODO: Should we update the args.OriginalSource to the new one?
			}
		}

		private bool HitTestOrRoot(
			PointerEventArgs args,
			[NotNullWhen(true)] out UIElement? element,
			string? reason = null,
			[CallerMemberName] string entryPoint = "",
			[CallerLineNumber] int entryLine = -1)
		{
			(element, _) = HitTestCore(args, null, entryPoint, entryLine, reason);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			element ??= _inputManager.ContentRoot.VisualTree.RootElement;

			return element is not null;
		}

		private bool HitTestOrRoot(
			PointerEventArgs args,
			StalePredicate isStale,
			[NotNullWhen(true)] out UIElement? element,
			out VisualTreeHelper.Branch? stale,
			string? reason = null,
			[CallerMemberName] string entryPoint = "",
			[CallerLineNumber] int entryLine = -1)
		{
			(element, stale) = HitTestCore(args, isStale, entryPoint, entryLine, reason);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			element ??= _inputManager.ContentRoot.VisualTree.RootElement;

			return element is not null;
		}

		private bool HitTest(
			PointerEventArgs args,
			StalePredicate isStale,
			[NotNullWhen(true)] out UIElement? element,
			out VisualTreeHelper.Branch? stale,
			string? reason = null,
			[CallerMemberName] string entryPoint = "",
			[CallerLineNumber] int entryLine = -1)
		{
			(element, stale) = HitTestCore(args, isStale, entryPoint, entryLine, reason);

			return element is not null;
		}

		private bool HitTest(
			PointerEventArgs args,
			[NotNullWhen(true)] out UIElement? element,
			string? reason = null,
			[CallerMemberName] string entryPoint = "",
			[CallerLineNumber] int entryLine = -1)
		{
			(element, _) = HitTestCore(args, null, entryPoint, entryLine, reason);

			return element is not null;
		}

		private (UIElement? element, VisualTreeHelper.Branch? stale) HitTestCore(PointerEventArgs args, StalePredicate? isStale, string entryPoint, int entryLine, string? reason)
		{
			if (_inputManager.ContentRoot.XamlRoot is null)
			{
				throw new InvalidOperationException("The XamlRoot must be properly initialized for hit testing.");
			}

			// When a RelativeRoot is set (scoped InputInjector), coordinates are
			// in that element's local space and the hit-test is scoped to its subtree.
			// SearchDownForTopMostElementAt propagates the searchRoot to GetTransform
			// so all coordinate transforms are relative to that element.
			var hitTestRoot = args.RelativeRoot as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			var result = VisualTreeHelper.HitTest(
				args.CurrentPoint.Position,
				hitTestRoot,
				null, isStale, entryPoint, entryLine, reason);

			return HitTestWithLightDismissAwareness(result, args, hitTestRoot, isStale, entryPoint, entryLine, reason);
		}

		// The pointer that has the right button down, which passes through the light-dismiss layer until it's released.
		private uint? _rightButtonPointerId;
		private bool _isPointerDownHitTest;

		private bool ShouldEventAlwaysPassThroughPopupLightDismissLayer(PointerEventArgs args)
			=> args.CurrentPoint.Properties.IsRightButtonPressed || _rightButtonPointerId == args.CurrentPoint.PointerId;

		// MUX Reference PointerInputProcessor.cpp, HitTestWithLightDismissAwareness, tag winui3/release/2.5.1, commit ba3a8d59e
		// When the topmost light-dismissable popup is a flyout,
		// we allow pointer events to pass through the light-dismiss layer
		// in three different scenarios:
		//
		// 1. Right-clicks always pass through the light-dismiss layer;
		//
		// 2. Drag-and-drop events always pass through the light-dismiss layer;
		//
		// 3. All other pointer events pass through the light-dismiss layer
		//    if they are over a UI element that the flyout has designated
		//    as an element that pointer events can reach through the light-dismiss layer.
		//
		private (UIElement? element, VisualTreeHelper.Branch? stale) HitTestWithLightDismissAwareness(
			(UIElement? element, VisualTreeHelper.Branch? stale) contact,
			PointerEventArgs args,
			UIElement? hitTestRoot,
			StalePredicate? isStale,
			string entryPoint,
			int entryLine,
			string? reason)
		{
			if (contact.element is not { } contactDO)
			{
				return contact;
			}


			var popupRoot = contactDO as PopupRoot;
			Popup? popup;
			UIElement? elementToDisableHitTestingOn;

			if (popupRoot is not null)
			{
				popup = popupRoot.GetTopmostPopup(PopupRoot.PopupFilter.LightDismissOnly);
				elementToDisableHitTestingOn = popupRoot;
			}
			else
			{
				popup = Popup.GetClosestPopupAncestor(contactDO);

				// Uno: the hit test walks the popup's child, and not the popup.
				elementToDisableHitTestingOn = popup?.Child;
			}

			var overlayInputPassThroughElementNoRef = popup?.GetOverlayInputPassThroughElement();

			// If this popup isn't light dismiss but has an overlay input pass-through element,
			// then we'll need to handle that separately, as the popup root being returned as a
			// hit-tested element only happens for light-dismiss popups.
			if (popup is not null
				&& (popupRoot is not null || (overlayInputPassThroughElementNoRef is not null && !popup.IsFlyout && !popup.IsLightDismiss)))
			{
				if (popup.IsFlyout || overlayInputPassThroughElementNoRef is not null)
				{
					(UIElement? element, VisualTreeHelper.Branch? stale) HitTestWithoutPopup()
						=> VisualTreeHelper.HitTest(
							args.CurrentPoint.Position,
							hitTestRoot,
							VisualTreeHelper.DefaultGetTestability.Except(elementToDisableHitTestingOn!),
							isStale, entryPoint, entryLine, reason);

					bool hitTestAgain = true;
					bool shouldEventPassThroughLightDismissLayer = ShouldEventAlwaysPassThroughPopupLightDismissLayer(args);

					// If the event shouldn't always pass through the light-dismiss overlay,
					// then we need to check to see if the pointer is currently over the
					// pass-through element, if one exists.  If one does, and the pointer is over it,
					// then we'll pass the event through.  Otherwise, we won't.
					if (!shouldEventPassThroughLightDismissLayer)
					{
						if (overlayInputPassThroughElementNoRef is not null)
						{
							// If the app specified that the overlay input pass-through element is the root visual,
							// then we want to always allow the event to pass through the light-dismiss layer.
							// This is needed because setting the root visual as the pass-through element implies that
							// you want all input to go through the light-dismiss layer, but since popups don't have
							// the root visual as their ancestor in the visual tree, the test for ancestry will fail.
							if (_inputManager.ContentRoot.VisualTree.PublicRootVisual == overlayInputPassThroughElementNoRef)
							{
								shouldEventPassThroughLightDismissLayer = true;
							}
							else
							{
								var subtreeHit = HitTestWithoutPopup();

								if (subtreeHit.element is { } subtreeHitTestDO
									&& (overlayInputPassThroughElementNoRef == subtreeHitTestDO || overlayInputPassThroughElementNoRef.IsAncestorOf(subtreeHitTestDO)))
								{
									// If the popup in question is a flyout with a button placement target that this hit-test
									// result is a part of, then we want to prevent that button's flyout from being opened by this input, since
									// that would cause the flyout to close and then immediately be reopened.
									if (popup.AssociatedFlyout?.Target is { } placementTargetNoRef
										&& (placementTargetNoRef == subtreeHitTestDO || placementTargetNoRef.IsAncestorOf(subtreeHitTestDO))
										&& placementTargetNoRef is Button placementTargetAsButtonNoRef)
									{
										placementTargetAsButtonNoRef.SuppressFlyoutOpening();
									}

									contact = subtreeHit;
									shouldEventPassThroughLightDismissLayer = true;
									hitTestAgain = false;
								}
							}
						}
					}

					if (shouldEventPassThroughLightDismissLayer)
					{
						if (hitTestAgain)
						{
							contact = HitTestWithoutPopup();
						}

						// Pointer-down messages should close the topmost flyout.
						// The others can pass through the light-dismiss layer, but should leave it open.
						if (_isPointerDownHitTest)
						{
							if (popup.IsFlyout)
							{
								// If the input hit a flyout, then we'll want to close child popups of that flyout
								// instead of closing all flyouts.  Otherwise, we'll close all flyouts.
								// If we have a chain of open flyouts, then closing the root flyout will close
								// all its nested children.
								FlyoutBase.CloseOpenFlyout(Popup.GetClosestPopupAncestor(contact.element)?.AssociatedFlyout);
							}
							else if (popupRoot is not null)
							{
								popupRoot.CloseTopmostPopup(FocusState.Pointer, PopupRoot.PopupFilter.LightDismissOnly);
							}
						}
					}
				}
			}

			return contact;
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
			=> Raise(evt, originalSource, routedArgs, BubblingContext.Bubble);

		private PointerEventDispatchResult Raise(PointerEvent evt, VisualTreeHelper.Branch branch, PointerRoutedEventArgs routedArgs)
			=> Raise(evt, branch.Leaf, routedArgs, BubblingContext.BubbleUpTo(branch.Root));

		private PointerEventDispatchResult Raise(PointerEvent evt, UIElement element, PointerRoutedEventArgs routedArgs, BubblingContext ctx)
		{
			using var dispatch = StartDispatch(evt, routedArgs);

			if (_trace)
			{
				Trace($"[Ignoring captures] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to element [{element.GetDebugName()}] with context {ctx}");
			}

			evt.Invoke(element, routedArgs, ctx);

			return dispatch.End();
		}

		private PointerEventDispatchResult RaiseUsingCaptures(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs, bool setCursor)
		{
			using var dispatch = StartDispatch(evt, routedArgs);

			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				var targets = capture.Targets.ToList();
				if (capture.ExplicitTarget is { } explicitTarget)
				{
					if (_trace)
					{
						Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{explicitTarget.GetDebugName()}]");
					}

					evt.Invoke(explicitTarget, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (target.Element == explicitTarget)
						{
							continue;
						}

						if (_trace)
						{
							Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to alternative (implicit) target [{explicitTarget.GetDebugName()}] (-- no bubbling--)");
						}

						evt.Invoke(target.Element, routedArgs.Reset(), BubblingContext.NoBubbling);
					}

					if (setCursor)
					{
						SetSourceCursor(explicitTarget);
					}
				}
				else
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
		}
		#endregion

		#region Tracing
		private static void Trace(string text)
		{
			_log.Trace(text);
		}

		private void TraceIgnoredAsNoTree(Windows.UI.Core.PointerEventArgs args, [CallerMemberName] string caller = "")
		{
			if (_trace)
			{
				Trace($"{caller} ({args.CurrentPoint.Position}) **undispatched** (root element not set yet).");
			}
		}

		private void TraceHandling(UIElement element, [CallerMemberName] string caller = "")
		{
			if (_trace)
			{
				Trace($"{caller} [{element.GetDebugName()}]");
			}
		}
		#endregion
	}
}
#endif
