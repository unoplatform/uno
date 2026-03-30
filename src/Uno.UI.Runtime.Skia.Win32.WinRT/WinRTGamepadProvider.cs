using System;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.Runtime.Skia.Win32.Support.WinRT;
using WinRTGamepad = Windows.Gaming.Input.Gamepad;

namespace Uno.UI.Runtime.Skia.Win32.WinRT;

/// <summary>
/// Implements <see cref="IWinRTGamepadProvider"/> using actual WinRT
/// <c>Windows.Gaming.Input.Gamepad</c> APIs available on Windows 10+.
/// </summary>
public class WinRTGamepadProvider : IWinRTGamepadProvider
{
	private readonly object _lock = new();
	private readonly Dictionary<WinRTGamepad, int> _gamepadToId = new();
	private readonly Dictionary<int, WinRTGamepad> _idToGamepad = new();
	private int _nextId;
	private bool _monitoring;
	private bool _disposed;

	public event Action<int>? GamepadConnected;
	public event Action<int>? GamepadDisconnected;

	public int[] GetConnectedGamepadIds()
	{
		lock (_lock)
		{
			// Sync with current WinRT state
			var currentGamepads = WinRTGamepad.Gamepads;

			// Remove gamepads no longer in the list
			var knownGamepads = _gamepadToId.Keys.ToArray();
			foreach (var gamepad in knownGamepads)
			{
				if (!currentGamepads.Contains(gamepad))
				{
					var id = _gamepadToId[gamepad];
					_gamepadToId.Remove(gamepad);
					_idToGamepad.Remove(id);
				}
			}

			// Add new gamepads
			foreach (var gamepad in currentGamepads)
			{
				EnsureGamepadRegistered(gamepad);
			}

			return _idToGamepad.Keys.ToArray();
		}
	}

	public WinRTGamepadReading GetReading(int gamepadId)
	{
		WinRTGamepad? gamepad;
		lock (_lock)
		{
			if (!_idToGamepad.TryGetValue(gamepadId, out gamepad))
			{
				return default;
			}
		}

		var reading = gamepad.GetCurrentReading();

		return new WinRTGamepadReading(
			Timestamp: reading.Timestamp,
			Buttons: (uint)reading.Buttons,
			LeftTrigger: reading.LeftTrigger,
			RightTrigger: reading.RightTrigger,
			LeftThumbstickX: reading.LeftThumbstickX,
			LeftThumbstickY: reading.LeftThumbstickY,
			RightThumbstickX: reading.RightThumbstickX,
			RightThumbstickY: reading.RightThumbstickY);
	}

	public void StartMonitoring()
	{
		if (_monitoring)
		{
			return;
		}

		_monitoring = true;
		WinRTGamepad.GamepadAdded += OnGamepadAdded;
		WinRTGamepad.GamepadRemoved += OnGamepadRemoved;
	}

	public void StopMonitoring()
	{
		if (!_monitoring)
		{
			return;
		}

		_monitoring = false;
		WinRTGamepad.GamepadAdded -= OnGamepadAdded;
		WinRTGamepad.GamepadRemoved -= OnGamepadRemoved;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		StopMonitoring();

		lock (_lock)
		{
			_gamepadToId.Clear();
			_idToGamepad.Clear();
		}
	}

	private void OnGamepadAdded(object? sender, WinRTGamepad gamepad)
	{
		int id;
		lock (_lock)
		{
			id = EnsureGamepadRegistered(gamepad);
		}

		GamepadConnected?.Invoke(id);
	}

	private void OnGamepadRemoved(object? sender, WinRTGamepad gamepad)
	{
		int id;
		lock (_lock)
		{
			if (!_gamepadToId.TryGetValue(gamepad, out id))
			{
				return;
			}

			_gamepadToId.Remove(gamepad);
			_idToGamepad.Remove(id);
		}

		GamepadDisconnected?.Invoke(id);
	}

	private int EnsureGamepadRegistered(WinRTGamepad gamepad)
	{
		if (!_gamepadToId.TryGetValue(gamepad, out var id))
		{
			id = _nextId++;
			_gamepadToId[gamepad] = id;
			_idToGamepad[id] = gamepad;
		}

		return id;
	}
}
