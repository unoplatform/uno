#if UNO_HAS_MANAGED_POINTERS
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using _PointerEventArgs = Windows.UI.Core.PointerEventArgs;

namespace Uno.UI.Xaml.Core;

internal sealed class DirectManipulation : InputManager.PointerManager.IGestureRecognizer
{
	/*
			                      "Continuing"            "Resuming"                      
			                  ┌─────────────────────┐   ┌────────────┐                    
			                  │                     ▼   ▼            │                    
			                  │  ┌─────────┐    ┌───────────┐    ┌───┴────┐    ┌─────────┐
			 Pointer pressed ─┴─►│Preparing├───►│Interacting├───►│Inertial├───►│Completed│
			                     └───┬─────┘    └─────┬─────┘    └───┬────┘    └─────────┘
			                         │                │              │              ▲     
			                         │                │              │              │     
			                         │                │              │              │     
			                         │                │              │         ┌────┴────┐
			                         └────────────────┴──────────────┴────────►│Cancelled│
			                                                                   └─────────┘

		Note: Cancelled is "cancelled by the user" using the UIElement.CancelDirectManipulation method.
			  It will only prevent any pointer event processing by the recognizer, except the pt cancel / remove (treated as pt cancel in that case).
			  It's not like the "aborted" event, which is fired when the manipulation is aborted by the recognizer
			  (Which is usually because `Settings` configured in the starting event prevents any manipulation detection).
			  It's neither caused by a pointer cancelled event, which is only completing the current gesture,
			  just like a pointer released event (but without any possible gesture).
	 */

#pragma warning disable IDE0055
	public enum States
	{	//		can:		Add handler		Add pointer									Scavenge on pt remove
		Preparing,		//	true			false										true
		Interacting,	//	false			true (multi-touch, a.k.a. "continuing")		false
		Inertial,		//	false			true (a.k.a. "resuming")					false
		Cancelled,		//	false			false										true
		Completed,		//	false			false										true
	}
#pragma warning restore IDE0055

	private static readonly Logger _log = LogExtensionPoint.Log(typeof(InputManager.PointerManager));
	private static readonly Action<string>? Trace = _log.IsEnabled(LogLevel.Trace) ? _log.Trace : null;

	private readonly InputManager.PointerManager _pointerManager;
	private readonly DirectManipulationCollection _collection;
	private readonly PointerIdentifier _originalPointer; // The original pointer that started the manipulation. Valid only when _state is States.Preparing, might have changed for all other states.
	private readonly GestureRecognizer _recognizer;

	private bool _isResuming;
	private GestureSettings _settings;
	private Windows.UI.Core.PointerEventArgs? _currentPointerArgs;

	private States _state = States.Preparing;

	// The handler for inertial manipulations, if any.
	// Note: once inertia as started we support only one handler!
	// Note 2: In case of resume, that handler might change!
	private IDirectManipulationHandler? _inertiaHandler;

	public List<IDirectManipulationHandler> Handlers { get; } = new();

	public DirectManipulation(InputManager.PointerManager pointerManager, DirectManipulationCollection collection, PointerIdentifier originalPointer)
	{
		_pointerManager = pointerManager;
		_collection = collection;
		_originalPointer = originalPointer;

		_recognizer = new GestureRecognizer(this)
		{
			GestureSettings = GestureSettingsHelper.Manipulations,
			PatchCases = WinRTFeatureConfiguration.GestureRecognizer.PatchCasesForDirectManipulation
		};
		_recognizer.ManipulationStarting += _onDirectManipulationStarting;
		_recognizer.ManipulationStarted += _onDirectManipulationStarted;
		_recognizer.ManipulationUpdated += _onDirectManipulationUpdated;
		_recognizer.ManipulationInertiaStarting += _onDirectManipulationInertiaStarting;
		_recognizer.ManipulationCompleted += _onDirectManipulationCompleted;
		_recognizer.ManipulationAborted += _onDirectManipulationAborted;
	}

	/// <summary>
	/// Indicates that the manipulation has started and all pointer events now has to be forwarded to this manipulation instead of being propagated to the visual tree.
	/// </summary>
	/// <remarks>Once true, will remain true forever! Complete will NOT set this back to false.</remarks>
	public bool HasStarted { get; private set; }

	public bool IsCompleted => _state is States.Completed;

	public bool IsPointerType(Windows.Devices.Input.PointerDeviceType type)
		=> _recognizer.PendingManipulation is { PointerDeviceType: var currentType } && currentType == (Microsoft.UI.Input.PointerDeviceType)type;

	/// <summary>
	/// Gets a boolean indicating that this manipulation is currently tracking the given pointer.
	/// "Tracking" means that the manipulation is interested in all updates of the pointer (i.e. not inertial).
	/// (This does NOT mean "interacting" !!)
	/// </summary>
	public bool IsTracking(PointerIdentifier pointer)
		=> _state switch
		{
			States.Preparing => pointer == _originalPointer,
			not States.Inertial => _recognizer.PendingManipulation?.IsActive(pointer) is true, // Note: Will return false once completed
			_ => false
		};

	public bool Cancel()
	{
		// Note: When cancelled, the GestureRecognizer is no longer updated until the pointer up / cancel.
		var wasActive = _state is States.Interacting or States.Inertial;
		_state = States.Cancelled;
		return wasActive;
	}

	#region Pointers input (direct-manip specific try-redirect methods [pre dispatch] + IGestureRecognizer [post dispatch])
	public bool TryProcessEnter(_PointerEventArgs args)
	{
		// WARNING: Unlike others TryProcess*** methods, this method is called for all pointers of same type, not only those that was considered as IsInteracting.

		Debug.Assert(_state is not States.Completed, "Inactive manipulation should have been scavenged prior trying to continue/resume.");

		// Note: We can resume an inertial manip ONLY if the inertial handler accepts it ... usually it will be when the pressed pointer is again on the target element.
		if (_inertiaHandler?.CanAddPointerAt(args.CurrentPoint.Position) ?? false)
		{
			Debug.Assert(_state is States.Inertial);

			// We don't need to do anything here, we wait for the down to .

			return true;
		}
		// "continue" multi-touch: else if(lastActiveHandler.IsInBoundsForResume())
		else
		{
			// Pointer is out-of-range, let continue normal processing (and potentially start another direct-manipulation).

			return false;
		}
	}

	public bool TryProcessDown(_PointerEventArgs args)
	{
		// WARNING: Unlike others TryProcess*** methods, this method is called for all pointers of same type, not only those that was considered as IsInteracting.

		Debug.Assert(_state is not States.Completed, "Inactive manipulation should have been scavenged prior trying to continue/resume.");

		// There are 2 cases where a manipulation can process a down event:
		//		* Single touch: inertial
		//		* Multi touch: multiples pinches (to zoom) with the release of only one pointer **NOT SUPPORTED**

		// Note: We can resume an inertial manip ONLY if the inertial handler accepts it ... usually it will be when the pressed pointer is again on the target element.
		if (_inertiaHandler?.CanAddPointerAt(args.CurrentPoint.Position) ?? false)
		{
			Debug.Assert(_state is States.Inertial);

			// Pointer is again above the element which currently handles the inertia, we can resume the direct manipulation.
			_isResuming = true;
			try
			{
				// For now we do not support multi-touch direct-manipulations, so we complete the previous manipulation and start a new one.
				// This has be changed to support pinch to zoom.
				using var _ = WithCurrent(args);
				_recognizer.CompleteGesture();
				_recognizer.ProcessDownEvent(args.CurrentPoint); // Starts a new manipulation (in starting state for now).
			}
			finally
			{
				_isResuming = false;
			}

			return true;
		}
		// "continue" multi-touch: else if(lastActiveHandler.IsInBoundsForResume())
		else
		{
			// Pointer is out-of-range, let continue normal processing (and potentially start another direct-manipulation).

			return false;
		}
	}

	/// <inheritdoc />
	public void ProcessDown(_PointerEventArgs args)
	{
		if (_state is States.Preparing // If resuming, we would have already processed the down event in TryProcessDown.
			&& args.CurrentPoint.Pointer == _originalPointer) // If a second pointer of the same type is pressed, it should have been handled in TryProcessDown.
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Adding pointer --POST DISPATCH-- (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			_recognizer.ProcessDownEvent(args.CurrentPoint);
		}
	}

	public bool TryProcessMove(_PointerEventArgs args)
	{
		Debug.Assert(_state is States.Preparing or States.Interacting or States.Cancelled);

		if (_state is States.Interacting)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Handling PRE move (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			_recognizer.ProcessMoveEvents([args.CurrentPoint]);
		}

		return HasStarted;
	}

	/// <inheritdoc />
	public void ProcessMove(_PointerEventArgs args)
	{
		if (!IsTracking(args.CurrentPoint.Pointer)) // The IGestureRecognizer is being called for all pointers of the same type, but direct-manipulation should only process the ones that are currently tracked.
		{
			return;
		}

		Debug.Assert(_state is States.Preparing or States.Cancelled, "In all other states we should it should have gone to the TryProcessMove and prevented dispatch to visual tree)");

		if (_state is not States.Cancelled)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Handling POST move (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			_recognizer.ProcessMoveEvents([args.CurrentPoint]);
		}
	}

	public bool TryProcessUp(_PointerEventArgs args)
	{
		Debug.Assert(_state is States.Preparing or States.Interacting or States.Cancelled);

		if (_state is States.Interacting)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Releasing pointer (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			_recognizer.ProcessUpEvent(args.CurrentPoint); // Will move **single-touch** manipulation to completed state
		}
		else if (_state is not States.Completed)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Releasing -abort- pointer (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			// Note: This is **not** multi-touch safe! We need to wait for the last pointer to be released/cancelled before completing the manipulation.
			_recognizer.CompleteGesture(); // Will move **single-touch** manipulation to completed state
		}

		return HasStarted;
	}

	/// <inheritdoc />
	public void ProcessUp(_PointerEventArgs args)
	{
		if (!IsTracking(args.CurrentPoint.Pointer)) // The IGestureRecognizer is being called for all pointers of the same type, but direct-manipulation should only process the ones that are currently tracked.
		{
			return;
		}

		Debug.Assert(_state is not States.Interacting, "Interacting should have been processed by TryProcessRelease (and prevented dispatch to visual tree)");

		if (_state is not States.Completed)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Releasing pointer of an non-started manip (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			// Note: This is **not** multi-touch safe! We need to wait for the last pointer to be released/cancelled before completing the manipulation.
			_recognizer.CompleteGesture(); // Will move to completed state and scavenge this direct-manipulation
		}
	}

	public bool TryProcessCancel(_PointerEventArgs args)
	{
		Debug.Assert(_state is States.Preparing or States.Interacting or States.Cancelled);

		if (_state is not States.Completed)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Cancelling pointer (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			// Note: This is **not** multi-touch safe! We need to wait for the last pointer to be released/cancelled before completing the manipulation.
			_recognizer.CompleteGesture(); // Will move to completed state and scavenge this direct-manipulation 
		}

		return HasStarted;
	}

	/// <inheritdoc />
	public void ProcessCancel(_PointerEventArgs args)
	{
		if (!IsTracking(args.CurrentPoint.Pointer)) // The IGestureRecognizer is being called for all pointers of the same type, but direct-manipulation should only process the ones that are currently tracked.
		{
			return;
		}

		Debug.Assert(_state is not States.Interacting, "Interacting should have been processed by TryProcessCancel (and prevented dispatch to visual tree)");

		if (_state is not States.Completed)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Cancelling pointer of an non-started manip (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");

			using var _ = WithCurrent(args);
			// Note: This is **not** multi-touch safe! We need to wait for the last pointer to be released/cancelled before completing the manipulation.
			_recognizer.CompleteGesture(); // Will move to completed state and scavenge this direct-manipulation 
		}
	}
	#endregion

	#region Manipulation event handlers
	private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> _onDirectManipulationStarting = static (sender, args) => ((DirectManipulation)sender.Owner).OnDirectManipulationStarting(sender, args);
	private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> _onDirectManipulationStarted = static (sender, args) => ((DirectManipulation)sender.Owner).OnDirectManipulationStarted(sender, args);
	private static readonly TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> _onDirectManipulationUpdated = static (sender, args) => ((DirectManipulation)sender.Owner).OnDirectManipulationUpdated(sender, args);
	private static readonly TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> _onDirectManipulationInertiaStarting = static (sender, args) => ((DirectManipulation)sender.Owner).OnDirectManipulationInertiaStarting(sender, args);
	private static readonly TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> _onDirectManipulationCompleted = static (sender, args) => ((DirectManipulation)sender.Owner).OnDirectManipulationCompleted(sender, args);
	private static readonly TypedEventHandler<GestureRecognizer, GestureRecognizer.Manipulation> _onDirectManipulationAborted = static (sender, manip) => ((DirectManipulation)sender.Owner).OnDirectManipulationAborted(sender, manip);

	private void OnDirectManipulationStarting(GestureRecognizer sender, ManipulationStartingEventArgs args)
	{
		if (_state is States.Cancelled)
		{
			return;
		}

		// Note: We MUST NOT use the PointerRoutedEventArgs.LastPointerEvent in this handler as it might be raised directly from a PointerEventArgs (NOT routed).
		if (_currentPointerArgs is null)
		{
			Debug.Fail("_currentPointerArgs must be set before requesting to the gesture recognizer to process that event!");
			return;
		}

		// TODO: Make sure ManipulationStarting is fired on UIElement.

		if (_state is not States.Preparing)
		{
			Trace?.Invoke($"[DirectManipulation] **RESUMING** Starting. ==> Restoring mode {_settings}.");

			args.Settings = _settings;

			// Resuming (from inertia), we clear the previous inertial handler
			_inertiaHandler = null;

			// When we resume a manipulation, we don't wait for the started to go in interacting mode.
			_state = States.Interacting; // Forcefully set as interacting without waiting for the recognizer to effectively (re-)start the manipulation

			// If pointer is able to give over info, we might have let them pass through before the pressed, if so make sure to clear them.
			_pointerManager.CancelPointer(_currentPointerArgs, isDirectManipulation: true, isDirectManipulationResume: true);
		}
		else if (PointerCapture.TryGet(args.Pointer, out var capture) && capture.Options.HasFlag(PointerCaptureOptions.PreventDirectManipulation))
		{
			Trace?.Invoke("[DirectManipulation] Ignored ==> An element in the tree prevented direct-manipulation.");

			// If a control has captured the pointer with our internal PreventDirectManipulation patch flag,
			// it means that it does not want to be redirected to direct manipulation.
			_settings = args.Settings = GestureSettings.None;
		}
		else
		{
			Trace?.Invoke($"[DirectManipulation] [{args.Pointer}] Starting");

			var supportedMode = ManipulationModes.None;
			foreach (var handler in Handlers)
			{
				supportedMode |= handler.OnStarting(sender, args);
			}

			_settings = args.Settings = supportedMode.ToGestureSettings();

			Trace?.Invoke($"[DirectManipulation] [{args.Pointer}] Starting ==> Final configured mode is {supportedMode}.");
		}
	}

	private void OnDirectManipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
	{
		if (_state is States.Cancelled)
		{
			return;
		}

		// Note: We MUST NOT use the PointerRoutedEventArgs.LastPointerEvent in this handler as it might be raised directly from a PointerEventArgs (NOT routed).
		if (_currentPointerArgs is null)
		{
			Debug.Fail("_currentPointerArgs must be set before requesting to the gesture recognizer to process that event!");
			return;
		}

		HasStarted = true;

		if (_state is States.Preparing)
		{
			Trace?.Invoke($"[DirectManipulation] [{args.Pointers[0]}] Started @={args.Position.ToDebugString()}");

			_state = States.Interacting;

			// Stealing the pointer! Starting from here, no other element in the visual tree will receive pointer events for this pointer,
			// and we will receive them only through the TryProcess*** methods.
			_pointerManager.CancelPointer(_currentPointerArgs, isDirectManipulation: true);

			foreach (var handler in Handlers)
			{
				handler.OnStarted(sender, args, isResuming: false);
			}
		}
		else
		{
			Trace?.Invoke($"[DirectManipulation] [{args.Pointers[0]}] **RESUMING** Started @={args.Position.ToDebugString()}");

			// Note: No needs to _state = States.Interacting, this has been done by the Starting

			foreach (var handler in Handlers)
			{
				handler.OnStarted(sender, args, isResuming: true);
			}
		}
	}

	private void OnDirectManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
	{
		if (_state is States.Cancelled)
		{
			return;
		}

		Trace?.Invoke($"[DirectManipulation] [{args.Pointers[0]}] Update @={args.Position.ToDebugString()} | Δ=({args.Delta} | v={args.Velocities}){(args.IsInertial ? " *inertial*" : "")}");

		Debug.Assert(!args.IsInertial || _inertiaHandler is not null);

		var unhandledDelta = args.Delta;
		if (args.IsInertial && _inertiaHandler is { } inertialHandler)
		{
			inertialHandler.OnUpdated(sender, args, ref unhandledDelta);
		}
		else
		{
			foreach (var handler in Handlers)
			{
				handler.OnUpdated(sender, args, ref unhandledDelta);
			}
		}
	}

	private void OnDirectManipulationInertiaStarting(GestureRecognizer sender, ManipulationInertiaStartingEventArgs args)
	{
		if (_state is States.Cancelled)
		{
			return;
		}

		Trace?.Invoke($"[DirectManipulation] [{args.Pointers[0]}] Inertia starting @={args.Position.ToDebugString()} | Δ=({args.Delta}) | v=({args.Velocities})");

		args.UseCompositionTimer = WinRTFeatureConfiguration.GestureRecognizer.UseCompositionTimerForDirectManipulation;

		_state = States.Inertial;

		var isHandled = false;
		foreach (var handler in Handlers)
		{
			if (handler.OnInertiaStarting(sender, args, isHandled))
			{
				_inertiaHandler ??= handler; // We assume that only one handler will handle the inertia.
				isHandled = true;
			}
		}

		// If no handler claimed the inertia, complete the gesture to prevent orphan inertia updates.
		// This avoids the assertion failure in OnDirectManipulationUpdated when IsInertial is true but _inertiaHandler is null.
		// This can happen when scrolling to the edge of content - velocity may exceed inertia threshold but there's nothing to scroll.
		if (!isHandled)
		{
			Trace?.Invoke("[DirectManipulation] Inertia not claimed by any handler, completing gesture.");
			sender.CompleteGesture();
		}
	}

	private void OnDirectManipulationCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs args)
	{
		if (_isResuming) // Possible only when cancelled during inertia, cf. TryProcessDown
		{
			return;
		}

		Trace?.Invoke($"[DirectManipulation] [{args.Pointers[0]}] Completed @={args.Position.ToDebugString()}");

		_state = States.Completed;

		// Even if cancelled we still want to notify the handlers that the manipulation has completed to avoid leaking state.
		foreach (var handler in Handlers)
		{
			handler.OnCompleted(recognizer, args);
		}
	}

	private void OnDirectManipulationAborted(GestureRecognizer recognizer, GestureRecognizer.Manipulation manip)
	{
		Trace?.Invoke("[DirectManipulation] Aborted");

		_state = States.Completed;

		// Even if cancelled we still want to notify the handlers that the manipulation has completed to avoid leaking state.
		foreach (var handler in Handlers)
		{
			handler.OnCompleted(recognizer, null);
		}
	}
	#endregion

	private readonly struct CurrentArgSubscription(DirectManipulation manipulation) : IDisposable
	{
		/// <inheritdoc />
		public void Dispose()
			=> manipulation._currentPointerArgs = null;
	}

	private CurrentArgSubscription WithCurrent(Windows.UI.Core.PointerEventArgs args)
	{
		Debug.Assert(_currentPointerArgs is null);
		_currentPointerArgs = args;
		return new CurrentArgSubscription(this);
	}

}
#endif
