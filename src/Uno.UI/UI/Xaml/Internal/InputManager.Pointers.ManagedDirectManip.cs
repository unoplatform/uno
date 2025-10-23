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

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

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
		/// Cancel direct manipulation which are handled by the given <paramref name="requestingElement"/>.
		/// </summary>
		internal bool CancelDirectManipulations(UIElement requestingElement)
		{
			var cancelled = false;
			foreach (var manipulation in _directManipulations)
			{
				if (manipulation.Handlers.Any(IsForParentOfRequestingElement))
				{
					cancelled |= manipulation.Cancel();
				}
			}

			return cancelled;

			bool IsForParentOfRequestingElement(IDirectManipulationHandler handler)
				=> handler.Owner is DependencyObject owner && requestingElement.GetAllParents(includeCurrent: true).Contains(owner);
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
