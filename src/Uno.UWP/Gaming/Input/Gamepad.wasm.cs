using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Uno;
using Uno.Extensions;
using Uno.Foundation;

using NativeMethods = __Windows.Gaming.Input.Gamepad.NativeMethods;

namespace Windows.Gaming.Input;

public partial class Gamepad
{
	private const char IdSeparator = ';';

	private readonly static Dictionary<long, Gamepad> _gamepadCache =
		 new Dictionary<long, Gamepad>();

	private readonly long _id;

	private Gamepad(long id)
	{
		_id = id;
	}

	public GamepadReading GetCurrentReading()
	{
		var reading = new GamepadReading();

		var result = NativeMethods.GetReading(_id);

		if (string.IsNullOrEmpty(result))
		{
			// Gamepad is not connected
			return reading;
		}

		var parts = result.Split('*');
		var timestampPart = parts[0];
		var axesPart = parts[1];
		var buttonsPart = parts[2];

		var timestampDouble = double.Parse(timestampPart, CultureInfo.InvariantCulture);
		reading.Timestamp = (ulong)(timestampDouble * 1000); // JS timestamp is in milliseconds

		if (!string.IsNullOrEmpty(axesPart))
		{
			var axes = axesPart.Split('|').Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();

			reading.LeftThumbstickX = GetGamepadValueIfExists(ref axes, 0);
			reading.LeftThumbstickY = -1 * GetGamepadValueIfExists(ref axes, 1);
			reading.RightThumbstickX = GetGamepadValueIfExists(ref axes, 2);
			reading.RightThumbstickY = -1 * GetGamepadValueIfExists(ref axes, 3);
		}

		if (!string.IsNullOrEmpty(buttonsPart))
		{
			var buttons = buttonsPart.Split('|').Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();

			var pressedButtons = GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 0) ? GamepadButtons.A : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 1) ? GamepadButtons.B : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 2) ? GamepadButtons.X : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 3) ? GamepadButtons.Y : GamepadButtons.None;

			pressedButtons |= IsButtonPressed(ref buttons, 4) ? GamepadButtons.LeftShoulder : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 5) ? GamepadButtons.RightShoulder : GamepadButtons.None;

			pressedButtons |= IsButtonPressed(ref buttons, 8) ? GamepadButtons.View : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 9) ? GamepadButtons.Menu : GamepadButtons.None;

			pressedButtons |= IsButtonPressed(ref buttons, 10) ? GamepadButtons.LeftThumbstick : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 11) ? GamepadButtons.RightThumbstick : GamepadButtons.None;

			pressedButtons |= IsButtonPressed(ref buttons, 12) ? GamepadButtons.DPadUp : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 13) ? GamepadButtons.DPadDown : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 14) ? GamepadButtons.DPadLeft : GamepadButtons.None;
			pressedButtons |= IsButtonPressed(ref buttons, 15) ? GamepadButtons.DPadRight : GamepadButtons.None;

			reading.Buttons = pressedButtons;

			reading.LeftTrigger = GetGamepadValueIfExists(ref buttons, 6);
			reading.RightTrigger = GetGamepadValueIfExists(ref buttons, 7);
		}

		return reading;
	}

	[JSExport]
	internal static int DispatchGamepadAdded(int id)
	{
		Gamepad? gamepad;
		lock (_gamepadCache)
		{
			if (!_gamepadCache.TryGetValue(id, out gamepad))
			{
				gamepad = new Gamepad(id);
				_gamepadCache.Add(id, gamepad);
			}
		}
		_gamepadAddedWrapper.Event?.Invoke(null, gamepad);
		return 0;
	}

	[JSExport]
	internal static int DispatchGamepadRemoved(int id)
	{
		Gamepad? gamepad;
		lock (_gamepadCache)
		{
			if (!_gamepadCache.TryGetValue(id, out gamepad))
			{
				gamepad = new Gamepad(id);
				_gamepadCache.Add(id, gamepad);
			}
		}
		_gamepadAddedWrapper.Event?.Invoke(null, gamepad);
		return 0;
	}

	private bool IsButtonPressed(ref double[] buttons, int index) =>
		GetGamepadValueIfExists(ref buttons, index) > 0.5;

	private double GetGamepadValueIfExists(ref double[] data, int index)
	{
		if (data.Length > index)
		{
			return data[index];
		}
		return 0.0;
	}

	private static IReadOnlyList<Gamepad> GetGamepadsInternal()
	{
		var serializedIds = NativeMethods.GetConnectedGamepadIds();

		var connectedGamepadIds =
			serializedIds
				.Split(new[] { IdSeparator }, StringSplitOptions.RemoveEmptyEntries)
				.Select(id => long.Parse(id, CultureInfo.InvariantCulture))
				.ToList();

		lock (_gamepadCache)
		{
			var cachedGamepads = _gamepadCache.Keys.ToArray();

			//remove disconnected
			var disconnectedDevices = cachedGamepads.Except(connectedGamepadIds);
			_gamepadCache.RemoveKeys(disconnectedDevices);

			//add newly connected
			foreach (var id in connectedGamepadIds)
			{
				if (!_gamepadCache.TryGetValue(id, out var gamepad))
				{
					gamepad = new Gamepad(id);
					_gamepadCache.Add(id, gamepad);
				}
			}

			return _gamepadCache.Values.OrderBy(g => g._id).ToArray();
		}
	}

	private static void StartGamepadAdded()
	{
		NativeMethods.StartGamepadAdded();
	}

	private static void EndGamepadAdded()
	{
		NativeMethods.EndGamepadAdded();
	}

	private static void StartGamepadRemoved()
	{
		NativeMethods.StartGamepadRemoved();
	}

	private static void EndGamepadRemoved()
	{
		NativeMethods.EndGamepadRemoved();
	}
}
