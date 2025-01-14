#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
using System;
using System.Collections.Generic;
using Uno.Helpers;

namespace Windows.Gaming.Input;

/// <summary>
/// Represents a gamepad.
/// </summary>
public partial class Gamepad : IGameController
{
	private static readonly object _syncLock = new object();

	private static readonly StartStopEventWrapper<Gamepad> _gamepadAddedWrapper;
	private static readonly StartStopEventWrapper<Gamepad> _gamepadRemovedWrapper;

	static Gamepad()
	{
		_gamepadAddedWrapper = new StartStopEventWrapper<Gamepad>(
			StartGamepadAdded, EndGamepadAdded, _syncLock);
		_gamepadRemovedWrapper = new StartStopEventWrapper<Gamepad>(
			StartGamepadRemoved, EndGamepadRemoved, _syncLock);
	}

	/// <summary>
	/// The list of all connected gamepads.
	/// </summary>
	public static IReadOnlyList<Gamepad> Gamepads => GetGamepadsInternal();

	/// <summary>
	/// Signals when a new gamepad is connected.
	/// </summary>
	public static event EventHandler<Gamepad> GamepadAdded
	{
		add => _gamepadAddedWrapper.AddHandler(value);
		remove => _gamepadAddedWrapper.RemoveHandler(value);
	}

	/// <summary>
	/// Signals when a gamepad is disconnected.
	/// </summary>
	public static event EventHandler<Gamepad> GamepadRemoved
	{
		add => _gamepadRemovedWrapper.AddHandler(value);
		remove => _gamepadRemovedWrapper.RemoveHandler(value);
	}

	internal static void OnGamepadAdded(Gamepad gamepad) =>
		_gamepadAddedWrapper.Event?.Invoke(null, gamepad);

	internal static void OnGamepadRemoved(Gamepad gamepad) =>
		_gamepadRemovedWrapper.Event?.Invoke(null, gamepad);
}
#endif
