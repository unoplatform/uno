#if UNO_HAS_MANAGED_POINTERS
#nullable enable
using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

/// <remarks>
/// Unlike manipulations on a UIElement, we can "resume" it, so the sequence can be:
/// Starting{1} ──► Started{1} ──► Updated{1-*} ───────────────────────────────────────────────────► Completed{1}
///                                  ▲   │                                                               ▲
///                                  │   └────► InertiaStarting{1} ──► Updated(isInertial=true){1-*} ────┤
///                                  │                                                                   │
///                                  └─────────────────── Started(isResuming=true){1} ◄──────────────────┘
/// 
/// While on UIElement it could be only
/// Starting{1} ──► Started{1} ──► Updated{1-*} ───────────────────────────────────────────────────► Completed{1}
///                                      │                                                               ▲
///                                      └────► InertiaStarting{1} ──► Updated(isInertial=true){1-*} ────┘
/// </remarks>
internal interface IDirectManipulationHandler
{
	object? Owner { get; }

	ManipulationModes OnStarting(GestureRecognizer recognizer, ManipulationStartingEventArgs args);

	/// <summary>
	/// Side-effect-free preview of the modes this handler would currently claim if a manipulation
	/// were to start right now. Used by <see cref="InputManager.PointerManager.CancelDirectManipulations"/>
	/// to decide whether a descendant's cancel request actually conflicts with this handler
	/// (e.g. a <see cref="ManipulationModes.TranslateX"/>-only descendant must not cancel a
	/// <see cref="ManipulationModes.TranslateY"/>-only ancestor scroller).
	/// </summary>
	/// <remarks>
	/// Returning <see cref="ManipulationModes.All"/> (the default) preserves the legacy "cancel everything"
	/// behavior for handlers that don't expose their effective modes.
	/// </remarks>
	ManipulationModes GetCurrentlyAcceptedModes()
		=> ManipulationModes.All;

	/// <summary>
	/// Determines if a pointer point is in handler's bounds, so a new pointer can be added to the current direct-manipulation.
	/// </summary>
	bool CanAddPointerAt(in Point absoluteLocation)
		=> false;

	/// <summary>
	/// Invoked when the manipulation has started.
	/// Starting from here, other elements in the tree will no longer receive pointer events (get PointerCaptureLost)
	/// </summary>
	/// <param name="recognizer">The associated gesture recognizer.</param>
	/// <param name="args"></param>
	/// <param name="isResuming">
	/// Indicates if this start is resuming a previous manipulation (e.g. touch again while in an inertial translation).
	/// WARNING: Args.Cumulative will be reset starting from here. If using it in OnUpdated or any other handlers, make sure to aggregate them!
	/// </param>
	void OnStarted(GestureRecognizer recognizer, ManipulationStartedEventArgs args, bool isResuming) { }

	void OnUpdated(GestureRecognizer recognizer, ManipulationUpdatedEventArgs args, ref ManipulationDelta unhandledDelta) { }

	/// <remarks>
	/// Be aware that unlike manipulations on a UIElement, we can "resume" it, so this can be invoked more than once.
	/// </remarks>
	bool OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, bool isHandled)
		=> false;

	void OnCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs? args) { }
}
#endif
