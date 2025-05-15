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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Devices.Input;
using Uno.UI.Extensions;
using System.Runtime.CompilerServices;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using System.Reflection;
using Uno.Extensions;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	partial class PointerManager
	{
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
			void OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, ref bool isHandled) { }

			void OnCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs? args) { }
		}

		private interface IGestureRecognizer
		{
			void ProcessDown(Windows.UI.Core.PointerEventArgs args);

			void ProcessMove(Windows.UI.Core.PointerEventArgs args);

			void ProcessUp(Windows.UI.Core.PointerEventArgs args);

			void ProcessCancel(Windows.UI.Core.PointerEventArgs args);
		}

		private readonly PointerTypePseudoDictionary<DirectManipulation> _directManipulations = new();
		private readonly PointerTypePseudoDictionary<Queue<IGestureRecognizer>> _gestureRecognizers = new();

		internal void RegisterDirectManipulationHandler(PointerIdentifier pointer, IDirectManipulationHandler handler)
			=> RegisterDirectManipulationHandlerCore(pointer, handler);

		internal void RedirectPointer(Windows.UI.Input.PointerPoint pointer, InteractionTracker tracker)
			=> RegisterDirectManipulationHandlerCore(pointer.Pointer, new InteractionTrackerToDirectManipulationHandler(tracker));

		private void RegisterDirectManipulationHandlerCore(PointerIdentifier pointer, IDirectManipulationHandler handler)
		{
			if (!_directManipulations.TryGetValue(pointer.Type, out var manip))
			{
				manip = _directManipulations[pointer.Type] = new(this);
				RegisterGestureRecognizerCore(pointer, manip);
			}

			manip.Handlers.Add(handler);

			if (_trace)
			{
				Trace($"[DirectManipulation] [{pointer}] Redirection requested to {handler.GetDebugName()}");
			}
		}

		internal void RegisterUiElementManipulationRecognizer(PointerIdentifier pointer, UIElement element, GestureRecognizer recognizer)
			=> RegisterGestureRecognizerCore(pointer, new UIElementRecognizer(element, recognizer));

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
				if (_directManipulations.TryGetValue(pointer.Type, out var manip))
				{
					cancelled |= manip.IsActive;
					manip.Cancel();
				}
			}

			return cancelled;
		}

		/// <summary>
		/// Cancel direct manipulation which are handled by the given <paramref name="requestingElement"/>.
		/// </summary>
		internal bool CancelDirectManipulations(UIElement requestingElement)
		{
			var cancelled = false;
			foreach (var (_, manip) in _directManipulations)
			{
				if (manip.Handlers.Any(handler => handler.Owner == requestingElement))
				{
					cancelled |= manip.IsActive;
					manip.Cancel();
				}
			}

			return cancelled;
		}

		private bool IsRedirectedToManipulations(PointerIdentifier pointerId)
			=> _directManipulations.ContainsKey(pointerId.Type);

		private bool BeforePressTryRedirectToManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			// Currently we do not support to redirect pointer before the press event (e.g. in pointer hover move).
			// This is theoretically possible using composition APIs, but currently never the case in current source code (as of 2025-03-24),
			// and is also most probably refused by WinUI.
			// So we cancel all gesture recognizer, except the one that is for the same kind of pointer (to allow multi-touch manipulation).

			var currentPointer = args.CurrentPoint.Pointer;
			var redirected = false;
			foreach ((var pointer, var manipulation) in _directManipulations)
			{
				if (pointer == currentPointer.Type && manipulation.IsActive)
				{
					// The pointer is of the same type of the pending manipulation:
					//		* Single touch: inertial
					//		* Multi touch: multiples pinches (to zoom) with the release of only one pointer **NOT SUPPORTED**

					if (_trace)
					{
						Trace($"[DirectManipulation] [{pointer}] Resuming previous manipulation (as we have a down for {currentPointer}).");
					}

					using var _ = manipulation.Resuming();
					using var __ = manipulation.WithCurrent(args);

					manipulation.Recognizer.CompleteGesture();
					manipulation.Recognizer.ProcessDownEvent(args.CurrentPoint); // Starts a new manipulation

					redirected = true;
				}
				else
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{pointer}] Completing previous manipulation (as we have a down for {currentPointer}).");
					}

					if (manipulation.IsActive)
					{
						manipulation.Recognizer.CompleteGesture();
					}

					_directManipulations.Remove(pointer); // Using the PointerTypePseudoDictionary we can safely remove while enumerating
				}
			}

			return redirected;
		}

		private void AfterPressForDirectManipulation(Windows.UI.Core.PointerEventArgs args)
		{
			// Direct manip handlers will register them during the PointerPressed event bubbling, then once bubbling is over,
			// we forward the press event to the gesture recognizer (so we will fire the ManipStarting event)

			if (_gestureRecognizers.TryGetValue(args.CurrentPoint.PointerDeviceType, out var recognizers))
			{
				foreach (var recognizer in recognizers)
				{
					recognizer.ProcessDown(args);
				}
			}
		}

		private bool BeforeMoveTryRedirectToManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_directManipulations.TryGetValue(args.CurrentPoint.PointerDeviceType, out var manip)
				&& manip.HasStarted) // We defer the handle of the move to **AFTER** while the manipulation has not started, so UIElement's manipulation will be able to kick-in before we stole all the pointers.
			{
				if (manip.IsActive)
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Handling move (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
					}

					using var _ = manip.WithCurrent(args);
					manip.Recognizer.ProcessMoveEvents([args.CurrentPoint]);
				}

				return true; // If the manipulation has been cancelled, we still do not want to forward the event to the visual tree until the next release/cancel.
			}

			return false;
		}

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
			if (_directManipulations.TryGetValue(args.CurrentPoint.PointerDeviceType, out var manip))
			{
				if (manip.IsActive)
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Releasing pointer (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
					}

					using var _ = manip.WithCurrent(args);
					manip.Recognizer.ProcessUpEvent(args.CurrentPoint);
				}

				var redirected = manip.HasStarted;
				if (redirected)
				{
					// If redirected, we won't receive the AfterReleaseForManipulations, make sure to clean-up UIElement recognizers.
					// Note: We do NOT remove the recognizer from the direct manipulation, it will self-remove when the manipulation is completed (i.e. once inertia is completed!).
					//		 This is to allow the manipulation to be cancelled if the pointer is re-pressed.
					_gestureRecognizers.Remove(args.CurrentPoint.PointerDeviceType);
				}

				return redirected;
			}

			return false;
		}

		private void AfterReleaseForManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_gestureRecognizers.TryGetValue(args.CurrentPoint.PointerDeviceType, out var recognizers))
			{
				foreach (var recognizer in recognizers)
				{
					recognizer.ProcessUp(args);
				}

				_gestureRecognizers.Remove(args.CurrentPoint.PointerDeviceType);
			}
		}

		private bool BeforeCancelTryRedirectToManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_directManipulations.TryGetValue(args.CurrentPoint.PointerDeviceType, out var manip))
			{
				if (manip.IsActive)
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Cancelling pointer (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
					}

					using var _ = manip.WithCurrent(args);
					manip.Cancel(); // Makes sure to complete handlers. 
				}

				var redirected = manip.HasStarted;
				if (redirected)
				{
					// If redirected, we won't receive the AfterReleaseForManipulations, make sure to clean-up UIElement recognizers.
					// Note: Even if no possible inertia here for direct-manipulation, we follow the same path as for the release and let him self-clean itself in complete.
					_gestureRecognizers.Remove(args.CurrentPoint.PointerDeviceType);
				}

				return redirected;
			}

			return false;
		}

		private void AfterCancelForManipulations(Windows.UI.Core.PointerEventArgs args)
		{
			if (_gestureRecognizers.TryGetValue(args.CurrentPoint.PointerDeviceType, out var recognizers))
			{
				foreach (var recognizer in recognizers)
				{
					recognizer.ProcessCancel(args);
				}

				_gestureRecognizers.Remove(args.CurrentPoint.PointerDeviceType);
			}
		}

		private void TraceIgnoredForManipulations(Windows.UI.Core.PointerEventArgs args, [CallerMemberName] string caller = "")
		{
			if (_trace)
			{
				Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Is redirected, ignore the {caller} (Event is NOT being forwarded to the visual tree).");
			}
		}

		private sealed class DirectManipulation : IGestureRecognizer
		{
			private readonly PointerManager _pointerManager;

			private bool _isResuming;
			private bool _isCancelled; // Has been cancelled by an external actor (UIElement.CancelDirectManipulation). Manip cannot be resumed.
			private bool _isCompleted; // Has been completed by the recognizer. Manip cannot be resumed.
			private GestureSettings _settings;
			private Windows.UI.Core.PointerEventArgs? _currentPointerArgs;

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
			/// <remarks>Once true, will remain true forever! Complete will NOT set this back to false.</remarks>
			public bool HasStarted { get; private set; }

			/// <summary>
			/// Indicates that the direct manipulation can accept new pointer events.
			/// </summary>
			public bool IsActive => !_isCancelled && !_isCompleted;

			public void Cancel()
			{
				_isCancelled = true;
				Recognizer.CompleteGesture(); // This will actually cause `_isCompleted = true` so the `_isCancelled` is useless so far, but we keep it for clarity.
			}

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
				recognizer.ManipulationAborted += OnDirectManipulationAborted;

				return recognizer;
			}

			public readonly struct ResumingManipulation(DirectManipulation manipulation) : IDisposable
			{
				/// <inheritdoc />
				public void Dispose()
					=> manipulation._isResuming = false;
			}

			public ResumingManipulation Resuming()
			{
				_isResuming = true;
				return new ResumingManipulation(this);
			}

			public readonly struct CurrentArgSubscription(DirectManipulation manipulation) : IDisposable
			{
				/// <inheritdoc />
				public void Dispose()
					=> manipulation._currentPointerArgs = null;
			}

			public CurrentArgSubscription WithCurrent(Windows.UI.Core.PointerEventArgs args)
			{
				Debug.Assert(_currentPointerArgs is null);
				_currentPointerArgs = args;
				return new CurrentArgSubscription(this);
			}

			#region Pointers input (IGestureRecognizerRegistration)
			/// <inheritdoc />
			public void ProcessDown(PointerEventArgs args)
			{
				if (!HasStarted // resuming, no needs to forward pointer again
					&& IsActive
					&& Recognizer.PendingManipulation?.IsActive(args.CurrentPoint.Pointer) is not true)
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Adding pointer --POST DISPATCH-- (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
					}

					using var _ = WithCurrent(args);
					Recognizer.ProcessDownEvent(args.CurrentPoint);
				}
			}

			/// <inheritdoc />
			public void ProcessMove(PointerEventArgs args)
			{
				if (!HasStarted && IsActive)
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.CurrentPoint.Pointer}] Handling POST move (@{args.CurrentPoint.Position.ToDebugString()} | ts={args.CurrentPoint.Timestamp}).");
					}

					using var _ = WithCurrent(args);
					Recognizer.ProcessMoveEvents([args.CurrentPoint]);
				}
			}

			/// <inheritdoc />
			public void ProcessUp(PointerEventArgs args) { }

			/// <inheritdoc />
			public void ProcessCancel(PointerEventArgs args) { }
			#endregion

			#region Manipulation event handlers
			private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> OnDirectManipulationStarting = (sender, args) =>
			{
				// TODO: Make sure ManipulationStarting is fired on UIElement.

				var that = (DirectManipulation)sender.Owner;
				var supportedMode = ManipulationModes.None;

				if (that.HasStarted)
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] **RESUMING** Starting. ==> Restoring mode {that._settings}.");
					}

					args.Settings = that._settings;
				}
				else if (PointerCapture.TryGet(args.Pointer, out var capture) && capture.Options.HasFlag(PointerCaptureOptions.PreventDirectManipulation))
				{
					if (_trace)
					{
						Trace("[DirectManipulation] Ignored ==> An element in the tree prevented direct-manipulation.");
					}

					// If a control has captured the pointer with our internal PreventDirectManipulation patch flag,
					// it means that it does not want to be redirected to direct manipulation.
					that._settings = args.Settings = GestureSettings.None;
				}
				else
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.Pointer}] Starting");
					}

					foreach (var handler in that.Handlers)
					{
						supportedMode |= handler.OnStarting(sender, args);
					}

					that._settings = args.Settings = supportedMode.ToGestureSettings();

					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.Pointer}] Starting ==> Final configured mode is {supportedMode}.");
					}
				}
			};

			private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> OnDirectManipulationStarted = (sender, args) =>
			{
				// Note: We MUST NOT use the PointerRoutedEventArgs.LastPointerEvent in this handler, as it might be raised directly from a PointerEventArgs (NOT routed).

				if (sender.Owner is not DirectManipulation { _currentPointerArgs: { } pointerArgs } that)
				{
					return;
				}

				if (that.HasStarted)
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.Pointers[0]}] **RESUMING** Started @={args.Position.ToDebugString()}");
					}

					foreach (var handler in that.Handlers)
					{
						handler.OnStarted(sender, args, isResuming: true);
					}
				}
				else
				{
					if (_trace)
					{
						Trace($"[DirectManipulation] [{args.Pointers[0]}] Started @={args.Position.ToDebugString()}");
					}

					that.HasStarted = true;
					that._pointerManager.CancelPointer(pointerArgs, isDirectManipulation: true);

					foreach (var handler in that.Handlers)
					{
						handler.OnStarted(sender, args, isResuming: false);
					}
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
				args.UseCompositionTimer = WinRTFeatureConfiguration.GestureRecognizer.UseCompositionTimerForDirectManipulation;

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

				if (that._isResuming) // Possible only when cancelled during inertia, cf. BeforePressTryRedirectToDirectManipulation
				{
					return;
				}

				// We do not self scavenge this manipulation from the _manipluations.
				// We prefer to wait for the next down so we can keep track of the completion state
				// (and more specifically the Cancelled state set using the UIElement.CancelDirectManipulation).
				that._isCompleted = true;

				if (_trace)
				{
					Trace($"[DirectManipulation] [{args.Pointers[0]}] Completed @={args.Position.ToDebugString()}");
				}

				foreach (var handler in that.Handlers)
				{
					handler.OnCompleted(sender, args);
				}
			};

			private static readonly TypedEventHandler<GestureRecognizer, GestureRecognizer.Manipulation> OnDirectManipulationAborted = (sender, manip) =>
			{
				var that = (DirectManipulation)sender.Owner;

				if (_trace)
				{
					Trace($"[DirectManipulation] Aborted");
				}

				// We do not self scavenge this manipulation from the _manipluations.
				// We prefer to wait for the next down so we can keep track of the completion state
				// (and more specifically the Cancelled state set using the UIElement.CancelDirectManipulation).
				that._isCompleted = true;

				foreach (var handler in that.Handlers)
				{
					handler.OnCompleted(sender, null);
				}
			};
			#endregion
		}

		private record InteractionTrackerToDirectManipulationHandler(InteractionTracker Tracker) : IDirectManipulationHandler
		{
			public object? Owner => Tracker;

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
			public void OnCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs? args)
				=> Tracker.CompleteUserManipulation(new Vector3((float)(args?.Velocities.Linear.X * 1000 ?? 0), (float)(args?.Velocities.Linear.Y * 1000 ?? 0), 0));
		}

		private class UIElementRecognizer(UIElement element, GestureRecognizer recognizer) : IGestureRecognizer
		{
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
				var relativePosition = UIElement.GetTransform(element, null).Transform(args.CurrentPoint.Position);

				return absolutePoint.At(relativePosition);
			}

			/// <inheritdoc />
			public override string ToString()
				=> element.GetDebugName();
		}

		private class PointerTypePseudoDictionary<TValue> : IEnumerable<KeyValuePair<PointerDeviceType, TValue>>
			where TValue : notnull
		{
			private static readonly int _length = Enum.GetValues(typeof(PointerDeviceType)).Length;

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

			public bool ContainsKey(PointerDeviceType pointer)
				=> _hasValues[(int)pointer];

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
		}
	}
}

#endif
