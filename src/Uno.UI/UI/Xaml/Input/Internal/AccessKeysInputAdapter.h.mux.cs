// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Uno-specific adapter struct bridging Uno's KeyEventArgs to the AccessKey engine's needs.

#if __SKIA__
#nullable enable

using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Adapter struct that bridges Uno's KeyEventArgs/KeyRoutedEventArgs to the AccessKey engine.
/// Replaces C++ InputMessage*.
/// </summary>
internal readonly struct AccessKeyInputMessage
{
	/// <summary>The virtual key code.</summary>
	internal VirtualKey VirtualKey { get; init; }

	/// <summary>True if this is a key down message.</summary>
	internal bool IsKeyDown { get; init; }

	/// <summary>True if this is a key up message.</summary>
	internal bool IsKeyUp { get; init; }

	/// <summary>True if this is a character (XCP_CHAR) message.</summary>
	internal bool IsChar { get; init; }

	/// <summary>The modifier keys pressed during this message.</summary>
	internal VirtualKeyModifiers Modifiers { get; init; }

	/// <summary>True if the Alt key is down during this message (from KeyStatus.IsMenuKeyDown).</summary>
	internal bool IsMenuKeyDown { get; init; }

	/// <summary>True if this is an extended key (right-side Alt/Ctrl).</summary>
	internal bool IsExtendedKey { get; init; }

	/// <summary>The unicode character for XCP_CHAR messages.</summary>
	internal char UnicodeKey { get; init; }

	/// <summary>True if the key was already down (repeat).</summary>
	internal bool WasKeyDown { get; init; }

	/// <summary>
	/// Creates an AccessKeyInputMessage from a KeyEventArgs (from the keyboard input source).
	/// </summary>
	internal static AccessKeyInputMessage FromKeyEventArgs(KeyEventArgs args, bool isKeyDown)
	{
		return new AccessKeyInputMessage
		{
			VirtualKey = args.VirtualKey,
			IsKeyDown = isKeyDown,
			IsKeyUp = !isKeyDown,
			IsChar = false,
			Modifiers = args.KeyboardModifiers,
			IsMenuKeyDown = args.KeyStatus.IsMenuKeyDown,
			IsExtendedKey = args.KeyStatus.IsExtendedKey,
			UnicodeKey = args.UnicodeKey ?? '\0',
			WasKeyDown = args.KeyStatus.WasKeyDown,
		};
	}

	/// <summary>
	/// Creates a character message from a unicode key code.
	/// </summary>
	internal static AccessKeyInputMessage FromCharacter(char character)
	{
		return new AccessKeyInputMessage
		{
			VirtualKey = (VirtualKey)character, // In C++, the platformKeyCode holds the char for XCP_CHAR
			IsKeyDown = false,
			IsKeyUp = false,
			IsChar = true,
			Modifiers = VirtualKeyModifiers.None,
			IsMenuKeyDown = false,
			IsExtendedKey = false,
			UnicodeKey = character,
			WasKeyDown = false,
		};
	}

	/// <summary>
	/// Returns true if the message represents an Alt key (VirtualKey.Menu) that is not an extended key (left Alt only).
	/// </summary>
	internal bool IsAltKey => VirtualKey == VirtualKey.Menu && !IsExtendedKey;

	/// <summary>
	/// Returns true if Alt modifier is present and key was not already down.
	/// </summary>
	internal bool HasMenuKeyDown => IsMenuKeyDown && !WasKeyDown;
}
#endif
