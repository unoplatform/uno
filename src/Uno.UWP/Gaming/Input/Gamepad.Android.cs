#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Android.Content;
using Android.Hardware.Input;
using Android.Views;
using Uno.Extensions;
using Uno.Gaming.Input.Internal;
using Uno.UI;

namespace Windows.Gaming.Input;

public partial class Gamepad
{
	private static InputManager? _inputManager;
	private static InputManager.IInputDeviceListener? _listener;
	private static Dictionary<int, Gamepad> _gamepadCache = new Dictionary<int, Gamepad>();

	private readonly int _nativeDeviceId;

	private GamepadReading _gamepadReading = new GamepadReading();

	public Gamepad(int nativeDeviceId)
	{
		_nativeDeviceId = nativeDeviceId;
	}

	public GamepadReading GetCurrentReading() => _gamepadReading;

	internal static bool TryHandleKeyEvent(KeyEvent e)
	{
		if (IsGamepad(e.Device))
		{
			if (TryGetOrCreateGamepad(e.DeviceId, out var gamepad))
			{
				gamepad!._gamepadReading.Timestamp = (ulong)e.EventTime;

				if (e.Action == KeyEventActions.Down)
				{
					var button = KeycodeToGamepadButtons(e.KeyCode);
					gamepad!._gamepadReading.Buttons =
						gamepad._gamepadReading.Buttons | button;

					return button != GamepadButtons.None;
				}
				else
				{
					var button = KeycodeToGamepadButtons(e.KeyCode);
					gamepad!._gamepadReading.Buttons =
						gamepad._gamepadReading.Buttons & (~button);

					return button != GamepadButtons.None;
				}
			}
		}
		return false;
	}

	internal static bool OnGenericMotionEvent(MotionEvent motionEvent)
	{
		if (IsGamepad(motionEvent.Device))
		{
			if (TryGetOrCreateGamepad(motionEvent.DeviceId, out var gamepad))
			{
				var reading = gamepad._gamepadReading;
				reading.Timestamp = (ulong)motionEvent.EventTime;
				if (GamepadDpad.GetDirectionPressed(motionEvent) is { } direction)
				{
					reading.Buttons = reading.Buttons & (~GamepadButtons.DPadDown);
					reading.Buttons = reading.Buttons & (~GamepadButtons.DPadUp);
					reading.Buttons = reading.Buttons & (~GamepadButtons.DPadLeft);
					reading.Buttons = reading.Buttons & (~GamepadButtons.DPadRight);

					reading.Buttons = reading.Buttons | direction;
				}

				var inputDevice = motionEvent.Device!;

				reading.LeftThumbstickX = GetCenteredAxis(motionEvent, inputDevice, Axis.X);
				reading.LeftThumbstickY = -1 * GetCenteredAxis(motionEvent, inputDevice, Axis.Y);
				reading.RightThumbstickX = GetCenteredAxis(motionEvent, inputDevice, Axis.Z);
				reading.RightThumbstickY = -1 * GetCenteredAxis(motionEvent, inputDevice, Axis.Rz);

				var leftTrigger = GetCenteredAxis(motionEvent, inputDevice, Axis.Brake);
				if (leftTrigger == 0)
				{
					leftTrigger = GetCenteredAxis(motionEvent, inputDevice, Axis.Rx);
				}
				reading.LeftTrigger = leftTrigger;

				var rightTrigger = GetCenteredAxis(motionEvent, inputDevice, Axis.Gas);
				if (rightTrigger == 0)
				{
					rightTrigger = GetCenteredAxis(motionEvent, inputDevice, Axis.Ry);
				}
				reading.RightTrigger = rightTrigger;

				gamepad._gamepadReading = reading;

				return true;
			}
		}
		return false;
	}

	private static float GetCenteredAxis(
		MotionEvent motionEvent,
		InputDevice device,
		Axis axis)
	{
		var range = device.GetMotionRange(axis, motionEvent.Source);

		// A joystick at rest does not always report an absolute position of
		// (0,0). Use the getFlat() method to determine the range of values
		// bounding the joystick axis center.
		if (range != null)
		{
			float flat = range.Flat;
			float value = motionEvent.GetAxisValue(axis);

			// Ignore axis values that are within the 'flat' region of the
			// joystick axis center.
			if (Math.Abs(value) > flat)
			{
				return value;
			}
		}
		else
		{
			return motionEvent.GetAxisValue(axis);
		}

		return 0;
	}

	private static GamepadButtons KeycodeToGamepadButtons(Keycode keycode) =>
		keycode switch
		{
			Keycode.ButtonA => GamepadButtons.A,
			Keycode.ButtonX => GamepadButtons.X,
			Keycode.ButtonB => GamepadButtons.B,
			Keycode.ButtonY => GamepadButtons.Y,
			Keycode.DpadUp => GamepadButtons.DPadUp,
			Keycode.DpadRight => GamepadButtons.DPadRight,
			Keycode.DpadDown => GamepadButtons.DPadDown,
			Keycode.DpadLeft => GamepadButtons.DPadLeft,
			Keycode.DpadUpLeft => GamepadButtons.DPadUp | GamepadButtons.DPadLeft,
			Keycode.DpadUpRight => GamepadButtons.DPadUp | GamepadButtons.DPadRight,
			Keycode.DpadDownRight => GamepadButtons.DPadDown | GamepadButtons.DPadRight,
			Keycode.DpadDownLeft => GamepadButtons.DPadDown | GamepadButtons.DPadLeft,
			Keycode.ButtonThumbl => GamepadButtons.LeftThumbstick,
			Keycode.ButtonThumbr => GamepadButtons.RightThumbstick,
			Keycode.ButtonL1 => GamepadButtons.LeftShoulder,
			Keycode.ButtonR1 => GamepadButtons.RightShoulder,
			Keycode.ButtonStart => GamepadButtons.Menu,
			Keycode.ButtonSelect => GamepadButtons.View,
			_ => GamepadButtons.None,
		};

	private static IReadOnlyList<Gamepad> GetGamepadsInternal()
	{
		var cachedDeviceIds = _gamepadCache.Keys.ToArray();
		var connectedIds = InputDevice.GetDeviceIds() ?? Array.Empty<int>();

		//remove disconnected
		var disconnectedDevices = cachedDeviceIds.Except(connectedIds);
		_gamepadCache.RemoveKeys(disconnectedDevices);

		//add newly connected
		foreach (var deviceId in connectedIds)
		{
			if (TryGetOrCreateGamepad(deviceId, out var gamepad))
			{
				if (!_gamepadCache.ContainsKey(deviceId))
				{
					_gamepadCache.Add(deviceId, gamepad!);
				}
			}
		}

		return _gamepadCache.Values.OrderBy(g => g._nativeDeviceId).ToArray();
	}

	private static void EnsureInputManagerInitialized()
	{
		if (_inputManager == null)
		{
			_inputManager = (InputManager?)ContextHelper.Current.GetSystemService(Context.InputService);

			if (_inputManager == null)
			{
				throw new InvalidOperationException("Cannot access input manager");
			}
		}
	}

	private static void StartGamepadAdded() =>
		AttachInputDeviceListener();

	private static void EndGamepadAdded() =>
		DetachInputDeviceListener();

	private static void StartGamepadRemoved() =>
		AttachInputDeviceListener();

	private static void EndGamepadRemoved() =>
		DetachInputDeviceListener();

	private static void AttachInputDeviceListener()
	{
		if (_listener != null)
		{
			return;
		}

		EnsureInputManagerInitialized();

		_listener = new InputDeviceListener();
		_inputManager!.RegisterInputDeviceListener(_listener, null);
	}

	private static void DetachInputDeviceListener()
	{
		if (_gamepadAddedWrapper.IsActive ||
			_gamepadRemovedWrapper.IsActive)
		{
			return;
		}

		_inputManager!.UnregisterInputDeviceListener(_listener);
		_listener?.Dispose();
		_listener = null;
	}

	private static bool IsGamepad(InputDevice? inputDevice) =>
		inputDevice?.Sources.HasFlag(InputSourceType.Gamepad) == true;

	private static bool TryGetOrCreateGamepad(int deviceId, [NotNullWhen(true)] out Gamepad? gamepad)
	{
		gamepad = null;

		if (_gamepadCache.TryGetValue(deviceId, out gamepad))
		{
			return true;
		}

		var inputDevice = InputDevice.GetDevice(deviceId);
		if (inputDevice != null)
		{
			if (IsGamepad(inputDevice))
			{
				gamepad = new Gamepad(deviceId);
				return true;
			}
		}
		return false;
	}

	private class InputDeviceListener
		: Java.Lang.Object, InputManager.IInputDeviceListener
	{
		public void OnInputDeviceAdded(int deviceId)
		{
			if (TryGetOrCreateGamepad(deviceId, out var gamepad))
			{
				if (!_gamepadCache.ContainsKey(deviceId))
				{
					_gamepadCache.Add(deviceId, gamepad!);
				}
				OnGamepadAdded(gamepad);
			}
		}

		public void OnInputDeviceChanged(int deviceId)
		{
		}

		public void OnInputDeviceRemoved(int deviceId)
		{
			if (_gamepadCache.TryGetValue(deviceId, out var gamepad))
			{
				_gamepadCache.Remove(deviceId);
				OnGamepadRemoved(gamepad);
			}
		}
	}
}
