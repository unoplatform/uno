// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// InputManager.h, InputManager.cpp

#nullable enable

using System;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Windows.Foundation;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager : IInputInjectorTarget, IPointerRedirector
{
	// TODO: This should not be static.
	// It should be per-InputManager
	// TODO: Consider using https://github.com/dotnet/roslyn/blob/d9272a3f01928b1c3614a942bdbbfeb0fb17a43b/src/Compilers/Core/Portable/Collections/SmallDictionary.cs
	// This dictionary is not going to be large, so SmallDictionary might be more efficient.
	private static Dictionary<uint, GestureRecognizer>? _pointerRedirections;

	public InputManager(ContentRoot contentRoot)
	{
		// For now, we have a shared compositor, but we can have multiple `InputManager`s
		// For that reason, IPointerRedirector deals with a static dictionary.
		// And so, we don't really care which InputManager is used for the shared compositor.
		// All `InputManager`s will do the same thing as they use the same dictionary (because it's static)
		// In future, we may want to have multiple compositors, and each should have its own
		// pointer redirector (and make the dictionary non-static)
		Compositor.GetSharedCompositor().PointerRedirector ??= this;

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

	void IPointerRedirector.RedirectPointer(Windows.UI.Input.PointerPoint pointer, List<InteractionTracker> trackers)
		=> RedirectPointer(pointer, trackers);

	private static void RedirectPointer(Windows.UI.Input.PointerPoint pointer, List<InteractionTracker> trackers)
	{
		_pointerRedirections ??= new();
		var recognizer = new GestureRecognizer(trackers);
		recognizer.GestureSettings = GestureSettingsHelper.Manipulations;
		recognizer.ManipulationStarting += OnRecognizerManipulationStarting;
		recognizer.ManipulationStarted += OnRecognizerManipulationStarted;
		recognizer.ManipulationUpdated += OnRecognizerManipulationUpdated;
		recognizer.ManipulationInertiaStarting += OnRecognizerManipulationInertiaStarting;
		recognizer.ManipulationCompleted += OnRecognizerManipulationCompleted;
		recognizer.ProcessDownEvent(new PointerPoint(pointer));

		Console.WriteLine($"Pointer {pointer.PointerId} redirected.");
		_pointerRedirections[pointer.PointerId] = recognizer;
	}

	private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> OnRecognizerManipulationStarting = (sender, args) =>
	{
		Console.WriteLine($"OnRecognizerManipulationStarting");
		// TODO: Make sure ManipulationStarting is fired on UIElement.
		var trackers = (List<InteractionTracker>)sender.Owner;
		foreach (var tracker in trackers)
		{
			Console.WriteLine($"StartUserManipulation");
			tracker.StartUserManipulation();
		}
	};

	private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> OnRecognizerManipulationStarted = (sender, args) =>
	{
		// TODO: We should raise PointerCaptureLost on the right UIElement that received ManipulationStarting.
		Console.WriteLine($"OnRecognizerManipulationStarted");
	};

	private static readonly TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> OnRecognizerManipulationUpdated = (sender, args) =>
	{
		Console.WriteLine($"OnRecognizerManipulationUpdated");
		var trackers = (List<InteractionTracker>)sender.Owner;
		foreach (var tracker in trackers)
		{
			Console.WriteLine($"ReceiveManipulationDelta");
			tracker.ReceiveManipulationDelta(args.Delta.Translation);
		}
	};

	private static readonly TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> OnRecognizerManipulationInertiaStarting = (sender, args) =>
	{
		Console.WriteLine($"OnRecognizerManipulationInertiaStarting");
		var trackers = (List<InteractionTracker>)sender.Owner;
		foreach (var tracker in trackers)
		{
			tracker.ReceiveInertiaStarting(args.Velocities.Linear);
		}
	};

	private static readonly TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> OnRecognizerManipulationCompleted = (sender, args) =>
	{
		Console.WriteLine($"OnRecognizerManipulationCompleted");
		var trackers = (List<InteractionTracker>)sender.Owner;
		foreach (var tracker in trackers)
		{
			Console.WriteLine($"ReceiveManipulationDelta");
			_pointerRedirections!.Remove(args.Pointers[0].Id);
			tracker.CompleteUserManipulation();
		}
	};

}
