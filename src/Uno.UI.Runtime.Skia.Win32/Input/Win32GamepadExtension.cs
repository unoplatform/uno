#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Gaming.Input.Internal;
using Uno.UI.Runtime.Skia.Win32.Support.WinRT;
using Windows.Gaming.Input;

namespace Uno.UI.Runtime.Skia.Win32.Input;

internal class Win32GamepadExtension : IGamepadExtension
{
	private readonly IWinRTGamepadProvider? _provider;
	private readonly Dictionary<int, Gamepad> _gamepadCache = new();
	private bool _monitoring;

	public Win32GamepadExtension(IWinRTGamepadProvider? provider)
	{
		_provider = provider;
	}

	public IReadOnlyList<Gamepad> GetGamepads()
	{
		if (_provider is null)
		{
			return Array.Empty<Gamepad>();
		}

		var connectedIds = _provider.GetConnectedGamepadIds();

		// Remove disconnected gamepads
		var cachedIds = _gamepadCache.Keys.ToArray();
		foreach (var id in cachedIds)
		{
			if (Array.IndexOf(connectedIds, id) < 0)
			{
				_gamepadCache.Remove(id);
			}
		}

		// Add newly connected gamepads
		foreach (var id in connectedIds)
		{
			if (!_gamepadCache.ContainsKey(id))
			{
				_gamepadCache[id] = new Gamepad(id);
			}
		}

		return _gamepadCache.Values.OrderBy(g => g.UserIndex).ToArray();
	}

	public GamepadReading GetCurrentReading(Gamepad gamepad)
	{
		if (_provider is null)
		{
			return default;
		}

		var winrtReading = _provider.GetReading(gamepad.UserIndex);

		return new GamepadReading
		{
			Timestamp = winrtReading.Timestamp,
			Buttons = (GamepadButtons)winrtReading.Buttons,
			LeftTrigger = winrtReading.LeftTrigger,
			RightTrigger = winrtReading.RightTrigger,
			LeftThumbstickX = winrtReading.LeftThumbstickX,
			LeftThumbstickY = winrtReading.LeftThumbstickY,
			RightThumbstickX = winrtReading.RightThumbstickX,
			RightThumbstickY = winrtReading.RightThumbstickY,
		};
	}

	public void StartMonitoring()
	{
		if (_provider is null || _monitoring)
		{
			return;
		}

		_monitoring = true;
		_provider.GamepadConnected += OnGamepadConnected;
		_provider.GamepadDisconnected += OnGamepadDisconnected;
		_provider.StartMonitoring();
	}

	public void StopMonitoring()
	{
		if (_provider is null || !_monitoring)
		{
			return;
		}

		_monitoring = false;
		_provider.GamepadConnected -= OnGamepadConnected;
		_provider.GamepadDisconnected -= OnGamepadDisconnected;
		_provider.StopMonitoring();
	}

	private void OnGamepadConnected(int gamepadId)
	{
		Win32EventLoop.Schedule(() =>
		{
			if (!_gamepadCache.ContainsKey(gamepadId))
			{
				var gamepad = new Gamepad(gamepadId);
				_gamepadCache[gamepadId] = gamepad;
				Gamepad.OnGamepadAdded(gamepad);
			}
		}, Uno.UI.Dispatching.NativeDispatcherPriority.Normal);
	}

	private void OnGamepadDisconnected(int gamepadId)
	{
		Win32EventLoop.Schedule(() =>
		{
			if (_gamepadCache.TryGetValue(gamepadId, out var gamepad))
			{
				_gamepadCache.Remove(gamepadId);
				Gamepad.OnGamepadRemoved(gamepad);
			}
		}, Uno.UI.Dispatching.NativeDispatcherPriority.Normal);
	}
}
