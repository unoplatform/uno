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
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
using PointerUpdateKind = Windows.UI.Input.PointerUpdateKind;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Composition;

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
				_reRouted = null;
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
					//Debug.Fail($"A pointer is already being processed {_manager._current} while trying to raise {this}.");
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

		private void OnPointerWheelChanged(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			if (IsRedirectedToManipulations(args.CurrentPoint.Pointer))
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

#if __SKIA__ // Currently, only Skia supports interaction tracker.
			Visual? currentVisual = originalSource.Visual;
			while (currentVisual is not null)
			{
				if (currentVisual.VisualInteractionSource is { RedirectsPointerWheel: true } vis)
				{
					foreach (var tracker in vis.Trackers)
					{
						tracker.ReceivePointerWheel(args.CurrentPoint.Properties.MouseWheelDelta / global::Microsoft.UI.Xaml.Controls.ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, args.CurrentPoint.Properties.IsHorizontalMouseWheel);
					}

					return;
				}

				currentVisual = currentVisual.Parent;
			}
#endif

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			// First raise the event, either on the OriginalSource or on the capture owners if any
			var result = RaiseUsingCaptures(Wheel, originalSource, routedArgs, setCursor: true);

			// Scrolling can change the element underneath the pointer, so we need to update over state
			HitTestOrRoot(args, _isOver, out originalSource, out var staleBranch, reason: "after_wheel");
			result += RaiseLeaveEnter(routedArgs, staleBranch, ref originalSource!, needsNonStaleOriginalSource: false);

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

			if (!HitTestOrRoot(args, out var originalSource))
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
			var isOutOfWindow = args.CurrentPoint.Pointer.Type is PointerDeviceType.Touch
				? !HitTest(args, out var originalSource) // No need to find the stale branch for touch as we will flush the over state on the whole tree
				: !HitTest(args, _isOver, out originalSource, out overStaleBranch);
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;
			if (originalSource is null) // Note: Theoretically impossible for the Release, but for safety
			{
				TraceIgnoredAsNoTree(args);
				return;
			}

			TraceHandling(originalSource);

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };
			var hadCapture = PointerCapture.TryGet(args.CurrentPoint.Pointer, out var capture) && capture.IsImplicitOnly is false;

			var result = RaiseUsingCaptures(Released, originalSource, routedArgs, setCursor: false);

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
					var overLeaf = VisualTreeHelper.SearchDownForLeaf(_inputManager.ContentRoot.VisualTree.RootElement, _isOver);
					result += Raise(Leave, overLeaf, routedArgs);
					break;

				// If we had capture, we might not have raise the pointer enter on the originalSource.
				// Make sure to raise it now the pointer has been release / capture has been removed (for pointers that supports overing).
				case PointerDeviceType.Mouse when hadCapture:
				case PointerDeviceType.Pen when hadCapture && args.CurrentPoint.Properties.IsInRange:
					if (result.VisualTreeAltered)
					{
						var root = _inputManager.ContentRoot.VisualTree.RootElement;
						overStaleBranch = new(root, VisualTreeHelper.SearchDownForLeaf(root, _isOver));
					}
					result += RaiseLeaveEnter(routedArgs, overStaleBranch, ref originalSource, needsNonStaleOriginalSource: false);
					break;
			}

			AfterReleaseForManipulations(args);

			args.DispatchResult = result;
		}

		private void OnPointerMoved(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
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

			return VisualTreeHelper.HitTest(args.CurrentPoint.Position, _inputManager.ContentRoot.XamlRoot, null, isStale, entryPoint, entryLine, reason);
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
