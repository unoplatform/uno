#nullable enable

using System;
using System.Collections.Generic;
using Windows.System;

namespace Uno.UI.Xaml.Input;

/// <summary>
/// A button or gesture of a TV remote (e.g. the Siri Remote), independent of the native platform.
/// </summary>
internal enum RemoteControlButton
{
	Up,
	Down,
	Left,
	Right,
	Select,
	Menu,
	PlayPause,
}

/// <summary>
/// Translates TV remote input into the virtual keys WinUI uses for a gamepad or remote on Xbox, so the regular
/// keyboard pipeline drives XY focus navigation, activation, light-dismiss and back navigation.
/// </summary>
internal sealed class RemoteControlInputTranslator
{
	// VK_MEDIA_PLAY_PAUSE is not part of the VirtualKey enum; Android maps its media key to the same value.
	internal const VirtualKey MediaPlayPause = (VirtualKey)179;

	private readonly Func<VirtualKey, bool, bool> _raiseKey;
	private readonly Func<bool> _requestBack;
	private readonly Action<Exception> _onError;
	private readonly HashSet<RemoteControlButton> _forwardedToSystem = new();

	/// <param name="raiseKey">Raises a key down (true) or up (false) through the keyboard pipeline, returning whether it was handled.</param>
	/// <param name="requestBack">Raises SystemNavigationManager.BackRequested, returning whether it was handled.</param>
	/// <param name="onError">Reports an exception thrown by an app handler, which then counts as not handling the input.</param>
	public RemoteControlInputTranslator(Func<VirtualKey, bool, bool> raiseKey, Func<bool> requestBack, Action<Exception> onError)
	{
		_raiseKey = raiseKey;
		_requestBack = requestBack;
		_onError = onError;
	}

	internal static VirtualKey ToVirtualKey(RemoteControlButton button) => button switch
	{
		RemoteControlButton.Up => VirtualKey.GamepadDPadUp,
		RemoteControlButton.Down => VirtualKey.GamepadDPadDown,
		RemoteControlButton.Left => VirtualKey.GamepadDPadLeft,
		RemoteControlButton.Right => VirtualKey.GamepadDPadRight,
		RemoteControlButton.Select => VirtualKey.GamepadA,
		RemoteControlButton.Menu => VirtualKey.GamepadB,
		RemoteControlButton.PlayPause => MediaPlayPause,
		_ => VirtualKey.None,
	};

	/// <returns>
	/// <see langword="false"/> when the app left the press unhandled and it should be passed on to the system,
	/// e.g. an unhandled Menu press at the root of the app, which returns to the home screen.
	/// </returns>
	public bool OnButtonDown(RemoteControlButton button)
	{
		var handled = TryRaiseKey(button, isDown: true);

		// Like B on Xbox, an unhandled Menu press is a back request.
		if (!handled && button == RemoteControlButton.Menu)
		{
			handled = TryInvoke(_requestBack);
		}

		if (!handled)
		{
			_forwardedToSystem.Add(button);
		}

		return handled;
	}

	/// <returns>
	/// <see langword="false"/> when the matching press down was passed on to the system, which then expects the release too.
	/// </returns>
	public bool OnButtonUp(RemoteControlButton button)
	{
		TryRaiseKey(button, isDown: false);

		return !_forwardedToSystem.Remove(button);
	}

	/// <summary>
	/// A swipe on the touch surface moves focus by one step, like a press of the matching arrow.
	/// </summary>
	public void OnSwipe(RemoteControlButton direction)
	{
		TryRaiseKey(direction, isDown: true);
		TryRaiseKey(direction, isDown: false);
	}

	private bool TryRaiseKey(RemoteControlButton button, bool isDown)
		=> TryInvoke(() => _raiseKey(ToVirtualKey(button), isDown));

	// Caught here rather than by the caller, so a throwing handler can't skip the press/release bookkeeping.
	private bool TryInvoke(Func<bool> handler)
	{
		try
		{
			return handler();
		}
		catch (Exception e)
		{
			_onError(e);
			return false;
		}
	}
}
