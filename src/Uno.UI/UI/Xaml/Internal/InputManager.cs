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

namespace Uno.UI.Xaml.Core;

internal partial class InputManager : IInputInjectorTarget
{
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
		//TODO Uno: match WinUI
		if (bringIntoView)
		{
			((UIElement)focusedElement!).StartBringIntoView(new BringIntoViewOptions
			{
				AnimationDesired = animateIfBringIntoView
			});
		}
	}

	internal bool LastInputWasNonFocusNavigationKeyFromSIP()
	{
		//TODO Uno: Implement
		return false;
	}
}
