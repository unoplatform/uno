#nullable enable

#if UNO_HAS_MANAGED_POINTERS
using System;
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Devices.Input;
using Uno.UI.Extensions;
using System.Runtime.CompilerServices;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	partial class PointerManager
	{
		internal interface IDirectManipulationHandler
		{
			ManipulationModes OnStarting(GestureRecognizer recognizer, ManipulationStartingEventArgs args);

			void OnStarted(GestureRecognizer recognizer, ManipulationStartedEventArgs args) { }

			void OnUpdated(GestureRecognizer recognizer, ManipulationUpdatedEventArgs args, ref ManipulationDelta unhandledDelta) { }

			void OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, ref bool isHandled) { }

			void OnCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs args) { }
		}

		// TODO: Consider using https://github.com/dotnet/roslyn/blob/d9272a3f01928b1c3614a942bdbbfeb0fb17a43b/src/Compilers/Core/Portable/Collections/SmallDictionary.cs
		// This dictionary is not going to be large, so SmallDictionary might be more efficient.
		private Dictionary<PointerIdentifier, DirectManipulation>? _directManipulations;
		private Windows.UI.Core.PointerEventArgs? _directManipulationsCurrentPointerArgs;

		private CurrentArgSubscription AsCurrentForDirectManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			Debug.Assert(_directManipulationsCurrentPointerArgs is null);
			_directManipulationsCurrentPointerArgs = args;
			return new CurrentArgSubscription(this);
		}

		internal void RegisterDirectManipulationTarget(PointerIdentifier pointer, IDirectManipulationHandler directManipulationTarget)
			=> RegisterDirectManipulationTargetCore(pointer, directManipulationTarget);

		internal void RedirectPointer(Windows.UI.Input.PointerPoint pointer, InteractionTracker tracker)
			=> RegisterDirectManipulationTargetCore(pointer.Pointer, new InteractionTrackerToDirectManipulationHandler(tracker));

		private void RegisterDirectManipulationTargetCore(PointerIdentifier pointer, IDirectManipulationHandler directManipulationTarget)
		{
			ref var manip = ref CollectionsMarshal.GetValueRefOrAddDefault(_directManipulations ??= new(), pointer, out var exists);
			if (exists)
			{
				manip!.Handlers.Add(directManipulationTarget);
			}
			else
			{
				manip = new DirectManipulation(this) { Handlers = { directManipulationTarget } };
			}

			if (_trace)
			{
				Trace($"[DirectManipulation] [{pointer}] Redirection requested to {directManipulationTarget.GetDebugName()}");
			}
		}

		private bool IsRedirectedToDirectManipulation(PointerIdentifier pointerId)
			=> _directManipulations?.ContainsKey(pointerId) == true;

		private bool TryRedirectPressToDirectManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			if (_directManipulations is not null)
			{
				// Currently we do not support to redirect pointer before the press event (e.g. in pointer hover move).
				// This is theoretically possible using composition APIs, but currently never the case in current source code (as of 2025-03-24),
				// and is also most probably refused by WinUI.

				// So we cancel all gesture recognizer, except the one that is for the same kind of pointer (to allow multi-touch manipulation).
				var currentPointer = args.CurrentPoint.Pointer;
				foreach ((var manipPointer, var manip) in new Dictionary<PointerIdentifier, DirectManipulation>(_directManipulations))
				{
					if (manipPointer.Type != currentPointer.Type // Not the same type
						|| manip.Recognizer.PendingManipulation is { Inertia: not null } // Manipulation is processing inertia, we want to stop it as soon as a new pointer is pressed
						|| manip.Recognizer.PendingManipulation?.IsActive(currentPointer) is true) // Second press on the same pointer, we cancel the previous one!
					{
						if (_trace)
						{
							Trace($"[DirectManipulation] [{manipPointer}] Completing previous manipulation (as we have a down for {currentPointer}).");
						}

						// Note: This will most probably invoke the Completed event, which will remove the manipulation from the dictionary (therefore we use a clone).
						manip.Recognizer.CompleteGesture();
					}
				}
			}

			return false;
		}

		private void EndPressForDirectManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			// SV will register them during the PointerPressed event bubbling, then once bubbling is over,
			// we forward the press event to the gesture recognizer (so we will fire the ManipStarting event).

			if (_directManipulations?.TryGetValue(args.CurrentPoint.Pointer, out var manip) is true
				&& manip.Recognizer.PendingManipulation?.IsActive(args.CurrentPoint.Pointer) is not true)
			{
				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Adding pointer --POST DISPATCH-- (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
				}

				using var _ = AsCurrentForDirectManipulation(args);
				manip.Recognizer.ProcessDownEvent(args.CurrentPoint);
			}
		}

		private bool TryRedirectMoveToDirectManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			if (_directManipulations?.TryGetValue(args.CurrentPoint.Pointer, out var manip) is true)
			{
				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Handling move (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
				}

				using var _ = AsCurrentForDirectManipulation(args);
				manip.Recognizer.ProcessMoveEvents([args.CurrentPoint]);

				return manip.HasStarted;
			}

			return false;
		}

		private bool TryRedirectReleaseToDirectManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			if (_directManipulations?.TryGetValue(args.CurrentPoint.Pointer, out var manip) is true)
			{
				// Note: We do NOT remove the recognizer from the manipulation, it will self-remove when the manipulation is completed (i.e. once inertia is completed!).
				//		 This is to allow the manipulation to be cancelled if the pointer is re-pressed.

				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Releasing pointer (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
				}

				using var _ = AsCurrentForDirectManipulation(args);
				manip.Recognizer.ProcessUpEvent(args.CurrentPoint);

				return manip.HasStarted;
			}

			return false;
		}

		private bool TryClearDirectManipulationRedirection(PointerIdentifier pointerId)
			=> _directManipulations?.Remove(pointerId) is true;

		private void TraceIgnoredForDirectManipulation(Windows.UI.Core.PointerEventArgs args, [CallerMemberName] string caller = "")
		{
			if (_trace)
			{
				Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Is redirected, ignore the {caller} (Event is NOT being forwarded to the visual tree).");
			}
		}

		private sealed class DirectManipulation
		{
			private readonly PointerManager _pointerManager;

			public GestureRecognizer Recognizer { get; }

			public List<IDirectManipulationHandler> Handlers { get; } = new();

			public DirectManipulation(PointerManager pointerManager)
			{
				_pointerManager = pointerManager;
				Recognizer = CreateDirectManipulationGestureRecognizer();
			}

			/// <summary>
			/// Indicates that the manipulation has started and all pointer events now has to be forwarded to this manipulation instead of being propagated to the visual tree.
			/// </summary>
			public bool HasStarted { get; set; }

			private GestureRecognizer CreateDirectManipulationGestureRecognizer()
			{
				var recognizer = new GestureRecognizer(this)
				{
					GestureSettings = GestureSettingsHelper.Manipulations,
					PatchCases = WinRTFeatureConfiguration.GestureRecognizer.PatchCasesForDirectManipulation
				};
				recognizer.ManipulationStarting += OnDirectManipulationStarting;
				recognizer.ManipulationStarted += OnDirectManipulationStarted;
				recognizer.ManipulationUpdated += OnDirectManipulationUpdated;
				recognizer.ManipulationInertiaStarting += OnDirectManipulationInertiaStarting;
				recognizer.ManipulationCompleted += OnDirectManipulationCompleted;

				return recognizer;
			}

			private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> OnDirectManipulationStarting = (sender, args) =>
			{
				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.Pointer}] Starting");
				}

				// TODO: Make sure ManipulationStarting is fired on UIElement.

				var manipulation = (DirectManipulation)sender.Owner;
				var supportedMode = ManipulationModes.None;

				foreach (var handler in manipulation.Handlers)
				{
					supportedMode |= handler.OnStarting(sender, args);
				}

				args.Settings = supportedMode.ToGestureSettings();

				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.Pointer}] Starting ==> Final configured mode is {supportedMode}.");
				}
			};

			private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> OnDirectManipulationStarted = (sender, args) =>
			{
				// Note: We MUST NOT use the PointerRoutedEventArgs.LastPointerEvent in this handler, as it might be raised directly from a PointerEventArgs (NOT routed).

				if (sender.Owner is not DirectManipulation { _pointerManager: { _directManipulationsCurrentPointerArgs: { } pointerArgs } manager } that)
				{
					return;
				}

				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.Pointers[0]}] Started @={args.Position.ToDebugString()}");
				}

				that.HasStarted = true;
				manager.CancelPointer(pointerArgs, isDirectManipulation: true);

				foreach (var handler in that.Handlers)
				{
					handler.OnStarted(sender, args);
				}
			};

			private static readonly TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> OnDirectManipulationUpdated = (sender, args) =>
			{
				var that = (DirectManipulation)sender.Owner;

				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.Pointers[0]}] Update @={args.Position.ToDebugString()} | Δ=({args.Delta} | v={args.Velocities}){(args.IsInertial ? " *inertial*" : "")}");
				}

				var unhandledDelta = args.Delta;
				foreach (var handler in that.Handlers)
				{
					handler.OnUpdated(sender, args, ref unhandledDelta);
				}
			};

			private static readonly TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> OnDirectManipulationInertiaStarting = (sender, args) =>
			{
				var that = (DirectManipulation)sender.Owner;

				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.Pointers[0]}] Inertia starting @={args.Position.ToDebugString()} | Δ=({args.Delta}) | v=({args.Velocities})");
				}

				var isHandled = false;
				foreach (var handler in that.Handlers)
				{
					handler.OnInertiaStarting(sender, args, ref isHandled);
				}
			};

			private static readonly TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> OnDirectManipulationCompleted = (sender, args) =>
			{
				var that = (DirectManipulation)sender.Owner;
				that._pointerManager._directManipulations!.Remove(args.Pointers[0]);

				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.Pointers[0]}] Completed @={args.Position.ToDebugString()}");
				}

				foreach (var handler in that.Handlers)
				{
					handler.OnCompleted(sender, args);
				}
			};
		}

		private record InteractionTrackerToDirectManipulationHandler(InteractionTracker Tracker) : IDirectManipulationHandler
		{
			/// <inheritdoc />
			public ManipulationModes OnStarting(GestureRecognizer recognizer, ManipulationStartingEventArgs args)
			{
				Tracker.StartUserManipulation();
				return ManipulationModes.All;
			}

			/// <inheritdoc />
			public void OnUpdated(GestureRecognizer recognizer, ManipulationUpdatedEventArgs args, ref ManipulationDelta unhandledDelta)
			{
				Tracker.ReceiveManipulationDelta(unhandledDelta.Translation);
				unhandledDelta = ManipulationDelta.Empty;
			}

			/// <inheritdoc />
			public void OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, ref bool isHandled)
				=> Tracker.ReceiveInertiaStarting(new Point(args.Velocities.Linear.X * 1000, args.Velocities.Linear.Y * 1000));

			/// <inheritdoc />
			public void OnCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs args)
				=> Tracker.CompleteUserManipulation(new Vector3((float)(args.Velocities.Linear.X * 1000), (float)(args.Velocities.Linear.Y * 1000), 0));
		}

		private struct CurrentArgSubscription(PointerManager manager) : IDisposable
		{
			/// <inheritdoc />
			public void Dispose()
				=> manager._directManipulationsCurrentPointerArgs = null;
		}
	}
}

#endif
