#nullable enable

#if UNO_HAS_MANAGED_POINTERS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	partial class PointerManager
	{
		private sealed class GestureRecognizerOwner(List<InteractionTracker> trackers, PointerManager pointerManager)
		{
			public List<InteractionTracker> Trackers => trackers;
			public PointerManager PointerManager => pointerManager;
		}

		// TODO: Consider using https://github.com/dotnet/roslyn/blob/d9272a3f01928b1c3614a942bdbbfeb0fb17a43b/src/Compilers/Core/Portable/Collections/SmallDictionary.cs
		// This dictionary is not going to be large, so SmallDictionary might be more efficient.
		private Dictionary<uint, GestureRecognizer>? _pointerRedirections;

		private bool IsRedirectedToInteractionTracker(uint pointerId)
			=> _pointerRedirections?.ContainsKey(pointerId) == true;

		private bool TryRedirectPointerPress(Windows.UI.Core.PointerEventArgs args)
		{
			if (_pointerRedirections?.TryGetValue(args.CurrentPoint.PointerId, out var recognizer) == true)
			{
#if HAS_UNO_WINUI
				recognizer.ProcessDownEvent(new PointerPoint(args.CurrentPoint));
#else
				recognizer.ProcessDownEvent(args.CurrentPoint);
#endif
				return true;
			}

			return false;
		}

		private bool TryRedirectPointerRelease(Windows.UI.Core.PointerEventArgs args)
		{
			if (_pointerRedirections?.TryGetValue(args.CurrentPoint.PointerId, out var recognizer) == true)
			{
#if HAS_UNO_WINUI
				recognizer.ProcessUpEvent(new PointerPoint(args.CurrentPoint));
#else
				recognizer.ProcessUpEvent(args.CurrentPoint);
#endif
				return true;
			}

			return false;
		}

		private bool TryRedirectPointerMove(Windows.UI.Core.PointerEventArgs args)
		{
			if (_pointerRedirections?.TryGetValue(args.CurrentPoint.PointerId, out var recognizer) == true)
			{
#if HAS_UNO_WINUI
				recognizer.ProcessMoveEvents([new PointerPoint(args.CurrentPoint)]);
#else
				recognizer.ProcessMoveEvents([args.CurrentPoint]);
#endif
				return true;
			}

			return false;
		}

		private bool TryClearPointerRedirection(uint pointerId)
			=> _pointerRedirections?.Remove(pointerId) == true;

		internal void RedirectPointer(Windows.UI.Input.PointerPoint pointer, InteractionTracker tracker)
		{
			if (_pointerRedirections?.TryGetValue(pointer.PointerId, out var recognizer) == true)
			{
				((GestureRecognizerOwner)recognizer.Owner).Trackers.Add(tracker);
				return;
			}

			_pointerRedirections ??= new();
			recognizer = new GestureRecognizer(new GestureRecognizerOwner([tracker], this));
			recognizer.GestureSettings = GestureSettingsHelper.Manipulations;
			recognizer.ManipulationStarting += OnRecognizerManipulationStarting;
			recognizer.ManipulationStarted += OnRecognizerManipulationStarted;
			recognizer.ManipulationUpdated += OnRecognizerManipulationUpdated;
			recognizer.ManipulationInertiaStarting += OnRecognizerManipulationInertiaStarting;
			recognizer.ManipulationCompleted += OnRecognizerManipulationCompleted;

#if HAS_UNO_WINUI
			recognizer.ProcessDownEvent(new PointerPoint(pointer));
#else
			recognizer.ProcessDownEvent(pointer);
#endif

			_pointerRedirections[pointer.PointerId] = recognizer;
		}

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> OnRecognizerManipulationStarting = (sender, args) =>
		{
			// TODO: Make sure ManipulationStarting is fired on UIElement.
			var trackers = ((GestureRecognizerOwner)sender.Owner).Trackers;
			foreach (var tracker in trackers)
			{
				tracker.StartUserManipulation();
			}
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> OnRecognizerManipulationStarted = (sender, args) =>
		{
			var owner = (GestureRecognizerOwner)sender.Owner;
			var currentPoint = PointerRoutedEventArgs.LastPointerEvent.GetCurrentPoint(null);
			var (originalSource, _) = VisualTreeHelper.HitTest(currentPoint.Position, owner.PointerManager._inputManager.ContentRoot.XamlRoot);
			originalSource?.RaiseEvent(UIElement.PointerCaptureLostEvent, new PointerRoutedEventArgs(new(currentPoint, Windows.System.VirtualKeyModifiers.None), originalSource));
			originalSource?.CompleteGesturesOnTree();
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> OnRecognizerManipulationUpdated = (sender, args) =>
		{
			var trackers = ((GestureRecognizerOwner)sender.Owner).Trackers;
			foreach (var tracker in trackers)
			{
				tracker.ReceiveManipulationDelta(args.Delta.Translation);
			}
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> OnRecognizerManipulationInertiaStarting = (sender, args) =>
		{
			var trackers = ((GestureRecognizerOwner)sender.Owner).Trackers;
			foreach (var tracker in trackers)
			{
				tracker.ReceiveInertiaStarting(new Point(args.Velocities.Linear.X * 1000, args.Velocities.Linear.Y * 1000));
			}
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> OnRecognizerManipulationCompleted = (sender, args) =>
		{
			var owner = (GestureRecognizerOwner)sender.Owner;
			var trackers = owner.Trackers;
			foreach (var tracker in trackers)
			{
				owner.PointerManager._pointerRedirections!.Remove(args.Pointers[0].Id);
				tracker.CompleteUserManipulation(new Vector3((float)(args.Velocities.Linear.X * 1000), (float)(args.Velocities.Linear.Y * 1000), 0));
			}
		};
	}
}
#endif
