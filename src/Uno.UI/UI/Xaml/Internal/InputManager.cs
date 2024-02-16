// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// InputManager.h, InputManager.cpp

#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core;

internal partial class InputManager : IInputInjectorTarget
{
	private sealed class GestureRecognizerOwner(List<InteractionTracker> trackers, InputManager inputManager)
	{
		public List<InteractionTracker> Trackers => trackers;
		public InputManager InputManager => inputManager;
	}

	// TODO: Consider using https://github.com/dotnet/roslyn/blob/d9272a3f01928b1c3614a942bdbbfeb0fb17a43b/src/Compilers/Core/Portable/Collections/SmallDictionary.cs
	// This dictionary is not going to be large, so SmallDictionary might be more efficient.
	private Dictionary<uint, GestureRecognizer>? _pointerRedirections;

	public InputManager(ContentRoot contentRoot)
	{
		ContentRoot = contentRoot;

		ConstructKeyboardManager();

		ConstructPointerManager();

		InitDragAndDrop();
	}

	partial void ConstructKeyboardManager();

	partial void ConstructPointerManager();

	/// <summary>
	/// Initialize the InputManager.
	/// </summary>
	internal void Initialize(object host)
	{
		InitializeKeyboard(host);
		InitializeManagedPointers(host);
	}

	partial void InitializeKeyboard(object host);

	partial void InitializeManagedPointers(object host);

	internal ContentRoot ContentRoot { get; }

	//TODO Uno: Set along with user input - this needs to be adjusted soon
	internal InputDeviceType LastInputDeviceType { get; set; } = InputDeviceType.None;

	internal FocusInputDeviceKind LastFocusInputDeviceKind { get; set; }

	internal bool ShouldRequestFocusSound()
	{
		//TODO Uno: Implement
		return false;
	}

	internal void NotifyFocusChanged(DependencyObject? focusedElement, bool bringIntoView, bool animateIfBringIntoView)
	{
		//TODO Uno: Implement
	}

	internal bool LastInputWasNonFocusNavigationKeyFromSIP()
	{
		//TODO Uno: Implement
		return false;
	}

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
		// TODO: Currently this dictionary is only used for UNO_HAS_MANAGED_POINTERS.
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
#if UNO_HAS_MANAGED_POINTERS
		var owner = (GestureRecognizerOwner)sender.Owner;
		var currentPoint = PointerRoutedEventArgs.LastPointerEvent.GetCurrentPoint(null);
		var (originalSource, _) = VisualTreeHelper.HitTest(currentPoint.Position, owner.InputManager.ContentRoot.XamlRoot);
		originalSource?.RaiseEvent(UIElement.PointerCaptureLostEvent, new PointerRoutedEventArgs(new(currentPoint, Windows.System.VirtualKeyModifiers.None), originalSource));
#endif
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
			owner.InputManager._pointerRedirections!.Remove(args.Pointers[0].Id);
			tracker.CompleteUserManipulation(new Vector3((float)(args.Velocities.Linear.X * 1000), (float)(args.Velocities.Linear.Y * 1000), 0));
		}
	};
}
