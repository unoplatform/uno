// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// InputManager.h, InputManager.cpp

using System;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager : IInputInjectorTarget
{
	private ContentRoot _contentRoot;

	public InputManager(ContentRoot contentRoot)
	{
		_contentRoot = contentRoot;

		ConstructManagedPointers();
	}
	partial void ConstructManagedPointers();

	/// <summary>
	/// Initialize the InputManager.
	/// </summary>
	internal void Initialize(object host) => InitializeManagedPointers(host);

	partial void InitializeManagedPointers(object host);

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
}
