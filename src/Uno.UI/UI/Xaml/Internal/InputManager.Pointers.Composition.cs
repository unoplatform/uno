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

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	partial class PointerManager
	{
		public interface IDirectManipulationHandler
		{
			ManipulationModes OnStarting(GestureRecognizer recognizer, ManipulationStartingEventArgs args);

			void OnStarted(GestureRecognizer recognizer, ManipulationStartedEventArgs args) { }

			void OnUpdated(GestureRecognizer recognizer, ManipulationUpdatedEventArgs args, ref ManipulationDelta unhandledDelta) { }

			void OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args) { }

			void OnCompleted(GestureRecognizer recognizer, ManipulationCompletedEventArgs args) { }
		}

		private sealed class DirectManipulation(PointerManager pointerManager)
		{
			public List<InteractionTracker> Trackers { get; } = new();
			public List<IDirectManipulationHandler> Handlers { get; } = new();
			public PointerManager PointerManager => pointerManager;

			/// <summary>
			/// Indicates that the manipulation has started and all pointer events now has to be forwarded to this manipulation instead of being propagated to the visual tree.
			/// </summary>
			public bool HasStarted { get; set; }
		}

		// TODO: Consider using https://github.com/dotnet/roslyn/blob/d9272a3f01928b1c3614a942bdbbfeb0fb17a43b/src/Compilers/Core/Portable/Collections/SmallDictionary.cs
		// This dictionary is not going to be large, so SmallDictionary might be more efficient.
		private Dictionary<PointerIdentifier, GestureRecognizer>? _pointerRedirections;

		private bool IsRedirected(PointerIdentifier pointerId)
			=> _pointerRedirections?.ContainsKey(pointerId) == true;

		private bool TryRedirectPointerPress(Windows.UI.Core.PointerEventArgs args)
		{
			// Note: This DOES not support multi-touch.
			//		 We should try to re-use an existing recognizer (for same kind of pointers ?) to see if it accepts the new pointer.
			if (_pointerRedirections?.TryGetValue(args.CurrentPoint.Pointer, out var recognizer) == true)
			{
				using var _ = AsCurrent(args);
				recognizer.ProcessDownEvent(args.CurrentPoint);
				return true;
			}

			return false;
		}

		private bool TryRedirectPointerPress2(Windows.UI.Core.PointerEventArgs args)
		{
			if (_pointerRedirections?.TryGetValue(args.CurrentPoint.Pointer, out var recognizer) is true
				&& recognizer.PendingManipulation?.IsActive(args.CurrentPoint.Pointer) is not true)
			{
				using var _ = AsCurrent(args);
				recognizer.ProcessDownEvent(args.CurrentPoint);

				return true;
			}

			return false;
		}

		private bool TryRedirectPointerMove(Windows.UI.Core.PointerEventArgs args)
		{
			if (_pointerRedirections?.TryGetValue(args.CurrentPoint.Pointer, out var recognizer) is true)
			{
				using var _ = AsCurrent(args);
				recognizer.ProcessMoveEvents([args.CurrentPoint]);

				return recognizer.Owner is DirectManipulation { HasStarted: true };
			}

			return false;
		}

		private bool TryRedirectPointerRelease(Windows.UI.Core.PointerEventArgs args)
		{
			if (_pointerRedirections?.Remove(args.CurrentPoint.Pointer, out var recognizer) is true)
			{
				using var _ = AsCurrent(args);
				recognizer.ProcessUpEvent(args.CurrentPoint);

				return recognizer.Owner is DirectManipulation { HasStarted: true };
			}

			return false;
		}

		private struct CurrentArgSubscription(PointerManager manager) : IDisposable
		{
			/// <inheritdoc />
			public void Dispose()
				=> manager._currentPointerArgs = null;
		}

		private CurrentArgSubscription AsCurrent(Windows.UI.Core.PointerEventArgs args)
		{
			Debug.Assert(_currentPointerArgs is null);
			_currentPointerArgs = args;
			return new CurrentArgSubscription(this);
		}

		private Windows.UI.Core.PointerEventArgs? _currentPointerArgs;

		private bool TryClearPointerRedirection(PointerIdentifier pointerId)
			=> _pointerRedirections?.Remove(pointerId) is true;

		internal void RegisterDirectManipulationTarget(PointerIdentifier pointer, IDirectManipulationHandler directManipulationTarget)
		{
			ref var recognizer = ref CollectionsMarshal.GetValueRefOrAddDefault(_pointerRedirections ??= new(), pointer, out var exists);
			if (exists)
			{
				((DirectManipulation)recognizer!.Owner).Handlers.Add(directManipulationTarget);
			}
			else
			{
				recognizer = CreateGestureRecognizer(new DirectManipulation(this) { Handlers = { directManipulationTarget } });
			}
		}

		internal void RedirectPointer(Windows.UI.Input.PointerPoint pointer, InteractionTracker tracker)
		{
			ref var recognizer = ref CollectionsMarshal.GetValueRefOrAddDefault(_pointerRedirections ??= new(), pointer.Pointer, out var exists);
			if (exists)
			{
				var manipulation = (DirectManipulation)recognizer!.Owner;
				manipulation.Trackers.Add(tracker);

				// We consider the manipulation active as soon as we have an interaction tracker.
				// This is only to ensure compatibility with the current behavior (redirection is probably enabled only when the manipulation has started).
				// But this could cause a double start threshold (one for the request to redirect and a second for the gesture recignizer to start)
				// and it should be revisited to determine if we should also forcefully start the GestureRecognizer.
				// Note: Once changed, also update the "create case" below !!!
				manipulation.HasStarted = true;
			}
			else
			{
				recognizer = CreateGestureRecognizer(new DirectManipulation(this) { Trackers = { tracker }, HasStarted = true });
				recognizer.ProcessDownEvent(pointer);
			}
		}

		private GestureRecognizer CreateGestureRecognizer(DirectManipulation owner)
		{
			var recognizer = new GestureRecognizer(owner)
			{
				GestureSettings = GestureSettingsHelper.Manipulations
			};
			recognizer.ManipulationStarting += OnRecognizerManipulationStarting;
			recognizer.ManipulationStarted += OnRecognizerManipulationStarted;
			recognizer.ManipulationUpdated += OnRecognizerManipulationUpdated;
			recognizer.ManipulationInertiaStarting += OnRecognizerManipulationInertiaStarting;
			recognizer.ManipulationCompleted += OnRecognizerManipulationCompleted;

			return recognizer;
		}

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> OnRecognizerManipulationStarting = (sender, args) =>
		{
			// TODO: Make sure ManipulationStarting is fired on UIElement.
			var manipulation = (DirectManipulation)sender.Owner;
			foreach (var tracker in manipulation.Trackers)
			{
				tracker.StartUserManipulation();
			}

			var supportedMode = ManipulationModes.None;
			foreach (var handler in manipulation.Handlers)
			{
				supportedMode |= handler.OnStarting(sender, args);
			}
			args.Settings = supportedMode.ToGestureSettings();
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> OnRecognizerManipulationStarted = (sender, args) =>
		{
			// Note: We MUST NOT use the PointerRoutedEventArgs.LastPointerEvent in this handler, as it might be raised directly from a PointerEventArgs (NOT routed).

			if (sender.Owner is not DirectManipulation { PointerManager: { _currentPointerArgs: { } pointerArgs } manager } manipulation)
			{
				return;
			}

			var (originalSource, _) = manager.HitTest(pointerArgs);
			if (!manager.EnsureSourceAndTrace(pointerArgs, ref originalSource))
			{
				return;
			}

			manipulation.HasStarted = true;
			originalSource.OnPointerCancel(new PointerRoutedEventArgs(pointerArgs, originalSource) { CanceledByDirectManipulation = true });

			foreach (var handler in manipulation.Handlers)
			{
				handler.OnStarted(sender, args);
			}
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> OnRecognizerManipulationUpdated = (sender, args) =>
		{
			var manipulation = (DirectManipulation)sender.Owner;
			foreach (var tracker in manipulation.Trackers)
			{
				tracker.ReceiveManipulationDelta(args.Delta.Translation);
			}

			var unhandledDelta = args.Delta;
			foreach (var handler in manipulation.Handlers)
			{
				handler.OnUpdated(sender, args, ref unhandledDelta);
			}
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> OnRecognizerManipulationInertiaStarting = (sender, args) =>
		{
			var manipulation = (DirectManipulation)sender.Owner;
			foreach (var tracker in manipulation.Trackers)
			{
				tracker.ReceiveInertiaStarting(new Point(args.Velocities.Linear.X * 1000, args.Velocities.Linear.Y * 1000));
			}

			foreach (var handler in manipulation.Handlers)
			{
				handler.OnInertiaStarting(sender, args);
			}
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> OnRecognizerManipulationCompleted = (sender, args) =>
		{
			var manipulation = (DirectManipulation)sender.Owner;
			manipulation.PointerManager._pointerRedirections!.Remove(args.Pointers[0]);

			foreach (var tracker in manipulation.Trackers)
			{
				tracker.CompleteUserManipulation(new Vector3((float)(args.Velocities.Linear.X * 1000), (float)(args.Velocities.Linear.Y * 1000), 0));
			}

			foreach (var handler in manipulation.Handlers)
			{
				handler.OnCompleted(sender, args);
			}
		};
	}
}

#endif
