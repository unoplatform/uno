// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// InputManager.h, InputManager.cpp

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Internal;
using Windows.Devices.Input;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager : IInputInjectorTarget
{
	private readonly ContextMenuProcessor _contextMenuProcessor;

	public InputManager(ContentRoot contentRoot)
	{
		ContentRoot = contentRoot;

		ConstructKeyboardManager();

		ConstructPointerManager();

		_contextMenuProcessor = new ContextMenuProcessor(contentRoot);
	}

	partial void ConstructKeyboardManager();

	partial void ConstructPointerManager();

	/// <summary>
	/// Initialize the InputManager.
	/// </summary>
	internal void Initialize(object host)
	{
		InitializeKeyboard(host);
		InitializePointers(host);
		InitDragAndDrop();
		Initialized = true;
	}

	partial void InitializeKeyboard(object host);

	internal ContentRoot ContentRoot { get; }

	internal bool Initialized { get; private set; }

	//TODO Uno: Set along with user input - this needs to be adjusted soon
	internal InputDeviceType LastInputDeviceType { get; set; } = InputDeviceType.None;

	internal FocusInputDeviceKind LastFocusInputDeviceKind { get; set; }

	internal bool ShouldRequestFocusSound()
	{
		//TODO Uno: Implement
		return false;
	}

	internal void NotifyFocusChanged(DependencyObject focusedElement, bool bringIntoView, bool animateIfBringIntoView)
	{
		//TODO Uno: match WinUI
		if (bringIntoView)
		{
			((UIElement)focusedElement).StartBringIntoView(new BringIntoViewOptions
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
