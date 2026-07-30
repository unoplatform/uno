#nullable enable

#if UNO_HAS_MANAGED_POINTERS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Devices.Input;
using Uno.UI.Extensions;
using System.Runtime.CompilerServices;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using System.Reflection;
using Uno.Extensions;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	partial class PointerManager
	{
		internal interface IGestureRecognizer
		{
			bool IsTracking(PointerIdentifier pointer);

			void ProcessDown(Windows.UI.Core.PointerEventArgs args);

			void ProcessMove(Windows.UI.Core.PointerEventArgs args);

			void ProcessUp(Windows.UI.Core.PointerEventArgs args);

			void ProcessCancel(Windows.UI.Core.PointerEventArgs args);
		}

		// The direct manipulations, i.e. the manipulation that "stole" the pointer to send events only to its handlers
		// Could be either composition-based (e.g. InteractionTracker) or UIElement-based (e.g. ScrollViewer).
		private readonly DirectManipulationCollection _directManipulations = new();

		// All the gesture recognizer currently active in the visual tree, including both direct-manipulation and plain UIElement recognizers (for manipulation events, not ScrollViewer).
		// This unified ordered list is required to make sure, in case of conflicting manipulations kinds, it's the top most recognizer that is able to start first.
		private readonly PointerTypePseudoDictionary<Queue<IGestureRecognizer>> _gestureRecognizers = new();

		private UIElement? _pendingDirectManipulationCancelRequester;

		internal void RegisterDirectManipulationHandler(PointerIdentifier pointer, IDirectManipulationHandler handler)
			=> RegisterDirectManipulationHandlerCore(pointer, handler);

		internal void RedirectPointer(Windows.UI.Input.PointerPoint pointer, InteractionTracker tracker)
			=> RegisterDirectManipulationHandlerCore(pointer.Pointer, new InteractionTrackerToDirectManipulationHandler(tracker));

		private void RegisterDirectManipulationHandlerCore(PointerIdentifier pointer, IDirectManipulationHandler handler)
		{
			if (_trace)
			{
				Trace($"[DirectManipulation] [{pointer}] Redirection requested to {handler.GetDebugName()}");
			}

			var manipulation = _directManipulations.Get(pointer);
			if (manipulation is null)
			{
				// Stop any inertial DMs sharing this handler to prevent dual-DM coexistence
				// where old inertial deltas fight the new manipulation's direction.
				_directManipulations.CompleteInertialForHandler(handler);

				manipulation = new DirectManipulation(this, _directManipulations, pointer);

				_directManipulations.Add(manipulation);
				RegisterGestureRecognizerCore(pointer, manipulation);
			}

			manipulation.Handlers.Add(handler);
		}

		/// <summary>
		/// When a gesture recognizers on a UIElement is STARTING
		/// </summary>
		internal void RegisterUiElementManipulationRecognizer(PointerIdentifier pointer, UIElement element, GestureRecognizer recognizer)
			=> RegisterGestureRecognizerCore(pointer, new UIElementRecognizer(element, recognizer));

		/// <summary>
		/// When a gesture recognizers on a UIElement completes
		/// </summary>
		internal void UnregisterUiElementManipulationRecognizer(PointerIdentifier[] pointers, GestureRecognizer recognizer) { }

		private void RegisterGestureRecognizerCore(PointerIdentifier pointer, IGestureRecognizer recognizer)
		{
			if (!_gestureRecognizers.TryGetValue(pointer.Type, out var recognizers))
			{
				recognizers = _gestureRecognizers[pointer.Type] = new();
			}

			recognizers.Enqueue(recognizer);

			if (_trace)
			{
				Trace($"[GestureRecognition] [{pointer}] Recognizer registered {recognizer}");
			}
		}

		/// <summary>
		/// Cancel direct manipulation for the given pointers, no matter their handlers/owner.
		/// </summary>
		internal bool CancelAllDirectManipulations(PointerIdentifier[] identifiers)
		{
			var cancelled = false;
			foreach (var pointer in identifiers)
			{
				cancelled |= _directManipulations.Get(pointer)?.Cancel() is true;
			}

			return cancelled;
		}

		/// <summary>
		/// Cancel direct manipulations whose handler is owned by <paramref name="requestingElement"/>
		/// itself or by any of its ancestors, when they would actually conflict with the element's
		/// declared <see cref="UIElement.ManipulationMode"/>.
		/// </summary>
		/// <remarks>
		/// Self-owned handlers are intentionally included: <see cref="ScrollContentPresenter"/>'s
		/// <see cref="IDirectManipulationHandler.Owner"/> is its <c>ScrollOwner</c>
		/// (the containing <see cref="ScrollViewer"/>), so callers like <see cref="FlipView"/> that
		/// invoke <see cref="UIElement.CancelDirectManipulations"/> directly on their inner
		/// <see cref="ScrollViewer"/> can release that ScrollViewer's own DM. Strict-ancestor-only
		/// semantics would silently no-op those call sites.
		///
		/// History: PR #23135 added an after-press redo of this walk in <see cref="AfterPressForDirectManipulation"/>
		/// to catch ancestor DMs that register LATE in the press bubble (specifically <see cref="ScrollContentPresenter"/>),
		/// fixing pinch on a <c>ManipulationMode=Scale</c> grid inside a scrollable ScrollViewer on Skia Android.
		/// That redo made the walk also cancel orthogonal-axis ancestors, which broke the common pattern of
		/// <c>SwipeControl</c> (<c>TranslateX</c>) inside a <c>ListView</c> (<c>TranslateY</c>): the parent's
		/// vertical-scroll DM was being cancelled, killing vertical scrolling. The walk is now axis-aware:
		/// it only cancels matching DMs whose currently-accepted modes share an axis with the requester
		/// (or whenever the requester declares <c>Scale</c>/<c>Rotate</c>/<c>All</c>/no specific axis, in which
		/// case the previous aggressive behavior is preserved).
		/// The axis aggregation in <see cref="ConflictsWithRequester"/> is restricted to handlers that
		/// match the requester-or-ancestor predicate, so a co-resident handler on the same pointer
		/// outside that chain (e.g. an <see cref="InteractionTracker"/> redirection whose default modes
		/// preview is <see cref="ManipulationModes.All"/>) cannot widen the effective modes and force a
		/// false-positive cancel.
		/// </remarks>
		internal bool CancelDirectManipulations(UIElement requestingElement)
		{
			_pendingDirectManipulationCancelRequester ??= requestingElement;

			var requesterModes = requestingElement.ManipulationMode;

			// Materialize the requester's self-and-ancestor chain once: the predicate runs up to
			// twice per handler (once for the Any() pre-check, once inside ConflictsWithRequester's
			// modes aggregation), and re-walking the visual tree each time would be O(handlers * depth)
			// on every pointer-down. A HashSet gives O(1) Contains and is built in one pass.
			var requesterAndAncestors = new HashSet<DependencyObject>(requestingElement.GetAllParents(includeCurrent: true));

			// Matches handlers owned by `requestingElement` itself or by any of its ancestors.
			// Self-inclusion is required for the FlipView-style "cancel my inner ScrollViewer's DM"
			// path (see method <remarks>). Hoisted to a local Func so the delegate is allocated once.
			Func<IDirectManipulationHandler, bool> isForRequesterOrAncestor = handler =>
				handler.Owner is DependencyObject owner && requesterAndAncestors.Contains(owner);

			var cancelled = false;
			foreach (var manipulation in _directManipulations)
			{
				if (manipulation.Handlers.Any(isForRequesterOrAncestor)
					&& ConflictsWithRequester(requesterModes, manipulation, isForRequesterOrAncestor))
				{
					cancelled |= manipulation.Cancel();
				}
			}

			return cancelled;
		}

		private static bool ConflictsWithRequester(
			ManipulationModes requesterModes,
			DirectManipulation manipulation,
			Func<IDirectManipulationHandler, bool> isHandlerForRequesterOrAncestor)
		{
			// Canonical translation axes: only TranslateX/Y count as actual translation on the
			// handler side. Rails (TranslateRailsX/Y) are gesture-settings flags meant to constrain
			// a translation axis, not to declare scrolling on their own. ScrollContentPresenter
			// advertises rails whenever IsHorizontal/VerticalRailEnabled is set even on axes it
			// doesn't scroll, so including rails in the handler-side mask would create false-positive
			// overlaps with an X-axis requester (e.g. a SwipeControl) against a Y-only scroller.
			const ManipulationModes XAxis = ManipulationModes.TranslateX;
			const ManipulationModes YAxis = ManipulationModes.TranslateY;
			// On the requester side rails still imply the corresponding translation axis (a
			// TranslateRailsX-only descendant is still intending to translate on X), so they
			// participate in the axis-claim test instead of falling through to the legacy
			// cancel-everything path.
			const ManipulationModes RequesterXClaim = XAxis | ManipulationModes.TranslateRailsX;
			const ManipulationModes RequesterYClaim = YAxis | ManipulationModes.TranslateRailsY;
			const ManipulationModes MultiPointer = ManipulationModes.Scale | ManipulationModes.Rotate;

			// No declared translation axis and no multi-pointer claim (e.g. ManipulationMode = System/None,
			// TranslateInertia alone, or an explicit user-facing CancelDirectManipulations() call from an
			// element that hasn't set ManipulationMode) -> fall back to the pre-axis-aware behavior of
			// cancelling every ancestor DM.
			var requesterClaimsX = (requesterModes & RequesterXClaim) != 0;
			var requesterClaimsY = (requesterModes & RequesterYClaim) != 0;
			var requesterIsMultiPointer = (requesterModes & MultiPointer) != 0;
			if (!requesterClaimsX && !requesterClaimsY && !requesterIsMultiPointer)
			{
				return true;
			}

			// Multi-pointer gestures (pinch/rotate) and a translating ancestor share the first pointer:
			// the ancestor would lock onto it before the second pointer arrives, aborting the descendant
			// gesture. Cancel the ancestor unconditionally to preserve PR #23135's fix.
			if (requesterIsMultiPointer)
			{
				return true;
			}

			// Pure translation request: only cancel handlers (owned by the requester or its ancestors)
			// whose currently-accepted modes overlap on the same axis. We aggregate modes from
			// requester-or-ancestor handlers only — a co-resident handler on the same DirectManipulation
			// that falls outside that chain (e.g. an InteractionTracker redirection registered via
			// RedirectPointer) whose default GetCurrentlyAcceptedModes() preview is ManipulationModes.All
			// must not widen handlerModes to All and force cancellation of an in-chain handler whose
			// effective axis doesn't actually conflict with the requester.
			// In-chain handlers that don't expose an axis preview still return ManipulationModes.All
			// by default, which keeps the legacy aggressive behavior for them.
			var handlerModes = ManipulationModes.None;
			foreach (var handler in manipulation.Handlers)
			{
				if (!isHandlerForRequesterOrAncestor(handler))
				{
					continue;
				}

				handlerModes |= handler.GetCurrentlyAcceptedModes();
			}

			// In-chain handler wants to do multi-pointer gestures: a single-finger gesture on the
			// descendant would steal pointers it needs, so treat it as a conflict to be safe.
			if ((handlerModes & MultiPointer) != 0)
			{
				return true;
			}

			return (requesterClaimsX && (handlerModes & XAxis) != 0)
				|| (requesterClaimsY && (handlerModes & YAxis) != 0);
		}

		private bool IsRedirectedToManipulations(PointerIdentifier pointerId)
			=> _directManipulations.Get(pointerId)?.HasStarted is true;

		private bool BeforeEnterTryRedirectToManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			// First we scavenge all manipulations that are no longer active
			_directManipulations.Scavenge();

			// Search for the first direct-manipulation that is able to handle this new pointer
			foreach (var manipulation in _directManipulations.OfType(args.CurrentPoint.PointerDeviceType))
			{
				if (manipulation.TryProcessEnter(args))
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Enter for a resuming/continuing manipulation.");
					}

					return true; // We was abled to find a direct-manipulation for the given args, we can stop here and prevent args to be dispatched to the visual tree.
				}
			}

			return false;
		}

		private bool BeforePressTryRedirectToManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			_pendingDirectManipulationCancelRequester = null;

			// First we scavenge all manipulations that are no longer active
			_directManipulations.Scavenge();

			// Search for the first direct-manipulation that is able to handle this new pointer
			foreach (var manipulation in _directManipulations.OfType(args.CurrentPoint.PointerDeviceType))
			{
				if (manipulation.TryProcessDown(args))
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Down which resumed/continued a previous manipulation.");
					}

					return true; // We was abled to find a direct-manipulation for the given args, we can stop here and prevent args to be dispatched to the visual tree.
				}
			}

			return false;
		}

		private void AfterPressForDirectManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			// Direct-manipulation handlers are typically registering them during the PointerPressed event bubbling, then once bubbling is over,
			// we forward the press event to the gesture recognizers (which will fire the ManipStarting event)

			if (_gestureRecognizers.TryGetValue(args.CurrentPoint.PointerDeviceType, out var recognizers))
			{
				foreach (var recognizer in recognizers)
				{
					recognizer.ProcessDown(args);
				}
			}

			if (_pendingDirectManipulationCancelRequester is { } requester)
			{
				CancelDirectManipulations(requester);
				_pendingDirectManipulationCancelRequester = null;
			}
		}

		private bool BeforeMoveTryRedirectToManipulations(Windows.UI.Core.PointerEventArgs args)
			=> _directManipulations.Get(args.CurrentPoint.Pointer)?.TryProcessMove(args) ?? false;

		private void AfterMoveForManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_gestureRecognizers.TryGetValue(args.CurrentPoint.PointerDeviceType, out var recognizers))
			{
				foreach (var recognizer in recognizers)
				{
					recognizer.ProcessMove(args);
				}
			}
		}

		private bool BeforeReleaseTryRedirectToManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_directManipulations.Get(args.CurrentPoint.Pointer)?.TryProcessUp(args) is true)
			{
				// The AfterReleaseForManipulations will **not** be invoked, so make sure to clean-up the recognizers here.
				_gestureRecognizers.Remove(args.CurrentPoint.Pointer.Type); // This is valid only because currently GestureRecognizer are completing gesture as soon as a pointer is being removed.

				return true;
			}
			else
			{
				return false;
			}
		}

		private void AfterReleaseForManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_gestureRecognizers.TryGetValue(args.CurrentPoint.PointerDeviceType, out var recognizers))
			{
				foreach (var recognizer in recognizers)
				{
					recognizer.ProcessUp(args);
				}

				_gestureRecognizers.Remove(args.CurrentPoint.PointerDeviceType); // This is valid only because currently GestureRecognizer are completing gesture as soon as a pointer is being removed.
			}
		}

		private bool BeforeCancelTryRedirectToManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_directManipulations.Get(args.CurrentPoint.Pointer)?.TryProcessCancel(args) is true)
			{
				// The AfterCancelForManipulations will **not** be invoked, so make sure to clean-up the recognizers here.
				_gestureRecognizers.Remove(args.CurrentPoint.Pointer.Type); // This is valid only because currently GestureRecognizer are completing gesture as soon as a pointer is being removed.

				return true;
			}
			else
			{
				return false;
			}
		}

		private void AfterCancelForManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_gestureRecognizers.TryGetValue(args.CurrentPoint.PointerDeviceType, out var recognizers))
			{
				foreach (var recognizer in recognizers)
				{
					recognizer.ProcessCancel(args);
				}

				_gestureRecognizers.Remove(args.CurrentPoint.PointerDeviceType); // This is valid only because currently GestureRecognizer are completing gesture as soon as a pointer is being removed.
			}
		}

		private void TraceIgnoredForManipulations(Windows.UI.Core.PointerEventArgs args, [CallerMemberName] string caller = "")
		{
			if (_trace)
			{
				Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Is redirected, ignore the {caller} (Event is NOT being forwarded to the visual tree).");
			}
		}

		private record InteractionTrackerToDirectManipulationHandler(InteractionTracker Tracker) : IDirectManipulationHandler
		{
			public object? Owner => Tracker;

			/// <inheritdoc />
			public ManipulationModes OnStarting(GestureRecognizer recognizer, ManipulationStartingEventArgs args)
				=> ManipulationModes.All;

			/// <inheritdoc />
			public void OnStarted(GestureRecognizer recognizer, ManipulationStartedEventArgs args, bool isResuming)
				=> Tracker.StartUserManipulation();

			/// <inheritdoc />
			public void OnUpdated(GestureRecognizer recognizer, ManipulationUpdatedEventArgs args, ref ManipulationDelta unhandledDelta)
			{
				Tracker.ReceiveManipulationDelta(unhandledDelta.Translation);
				unhandledDelta = ManipulationDelta.Empty;
			}

			/// <inheritdoc />
			public bool OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, bool isHandled)
			{
				if (isHandled)
				{
					return false;
				}

				Tracker.ReceiveInertiaStarting(new Point(args.Velocities.Linear.X * 1000, args.Velocities.Linear.Y * 1000));
				return true;
			}

			/// <inheritdoc />
			public void OnCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs? args)
				=> Tracker.CompleteUserManipulation(new Vector3((float)(args?.Velocities.Linear.X * 1000 ?? 0), (float)(args?.Velocities.Linear.Y * 1000 ?? 0), 0));
		}

		private class UIElementRecognizer(UIElement element, GestureRecognizer recognizer) : IGestureRecognizer
		{
			/// <inheritdoc />
			public bool IsTracking(PointerIdentifier pointer)
				=> recognizer.IsTracking(pointer);

			/// <inheritdoc />
			public void ProcessDown(PointerEventArgs args) { } // Currently, to avoid any regression for 6.0 SR1, only the move is processed.

			/// <inheritdoc />
			public void ProcessMove(PointerEventArgs args)
			{
				recognizer.ProcessMoveEvents([GetRelativePoint(args)]);
				if (recognizer.IsDragging)
				{
					XamlRoot.GetCoreDragDropManager(element.XamlRoot).ProcessMoved(new PointerRoutedEventArgs(args, element));
				}
			}

			/// <inheritdoc />
			public void ProcessUp(PointerEventArgs args) { } // Currently, to avoid any regression for 6.0 SR1, only the move is processed.

			/// <inheritdoc />
			public void ProcessCancel(PointerEventArgs args) { } // Currently, to avoid any regression for 6.0 SR1, only the move is processed.

			/// <summary>
			/// Get the PointerPoint relative to the location of the element.
			/// This is to be backward compatible with the current behavior, but we should consider to use only absolute location in the GestureRecognizer
			/// and then make the position relative to the UIElement only in the conversion from ManipXXXEventArgs to ManipXXX**Routed**EventArgs.
			/// </summary>
			private PointerPoint GetRelativePoint(PointerEventArgs args)
			{
				var absolutePoint = args.CurrentPoint;
				var relativePosition = UIElement.GetTransform(element, null).Inverse().Transform(args.CurrentPoint.Position);

				return absolutePoint.At(relativePosition);
			}

			/// <inheritdoc />
			public override string ToString()
				=> element.GetDebugName();
		}

		private class PointerTypePseudoDictionary<TValue> : IEnumerable<KeyValuePair<PointerDeviceType, TValue>>
			where TValue : notnull
		{
			private static readonly int _length = Enum.GetValues<PointerDeviceType>().Length;

			private readonly bool[] _hasValues = new bool[_length];
			private readonly TValue[] _values = new TValue[_length];

			public TValue? this[PointerDeviceType pointer]
			{
				get => _values[(int)pointer];
				set
				{
					var index = (int)pointer;
					_hasValues[index] = true;
					_values[index] = value!;
				}
			}

			//public bool ContainsKey(PointerDeviceType pointer)
			//	=> _hasValues[(int)pointer];

			public bool TryGetValue(PointerDeviceType pointer, [NotNullWhen(true)] out TValue? value)
			{
				var index = (int)pointer;
				if (_hasValues[index])
				{
					value = _values[index];
					return true;
				}
				else
				{
					value = default;
					return false;
				}
			}

			public void Remove(PointerDeviceType pointer)
				=> _hasValues[(int)pointer] = false;

			/// <inheritdoc />
			public IEnumerator<KeyValuePair<PointerDeviceType, TValue>> GetEnumerator()
			{
				for (var i = 0; i < _length; i++)
				{
					if (_hasValues[i])
					{
						yield return new KeyValuePair<PointerDeviceType, TValue>((PointerDeviceType)i, _values[i]);
					}
				}
			}

			/// <inheritdoc />
			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();

			public void ClearForFataError()
			{
				for (var i = 0; i < _length; i++)
				{
					_hasValues[i] = false;
					_values[i] = default!;
				}
			}
		}
	}
}

#endif
