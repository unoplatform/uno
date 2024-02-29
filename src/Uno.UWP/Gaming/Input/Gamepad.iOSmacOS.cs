using System.Collections.Generic;
using System.Linq;
using Foundation;
using GameController;
using Uno.Extensions;

namespace Windows.Gaming.Input;

public partial class Gamepad
{
	private readonly static Dictionary<GCController, Gamepad> _gamepadCache =
		new Dictionary<GCController, Gamepad>();

	private static int _nextGamepadId = 1;
	private static NSObject? _didConnectObserver;
	private static NSObject? _didDisconnectObserver;
	private readonly GCController _controller;
	private readonly int _id;

	private Gamepad(GCController controller)
	{
		_controller = controller;
		_id = _nextGamepadId++;
	}

	public GamepadReading GetCurrentReading()
	{
		if (_controller.ExtendedGamepad != null)
		{
			return ReadExtendedGamepad();
		}
		else if (_controller.MicroGamepad != null)
		{
			return ReadMicroGamepad();
		}
		return new GamepadReading();
	}

	private GamepadReading ReadExtendedGamepad()
	{
		var reading = new GamepadReading();

		reading.Timestamp = (ulong)(_controller.ExtendedGamepad!.LastEventTimestamp * 100000);

		if (_controller.ExtendedGamepad.ButtonA.IsPressed)
		{
			reading.Buttons |= GamepadButtons.A;
		}

		if (_controller.ExtendedGamepad.ButtonB.IsPressed)
		{
			reading.Buttons |= GamepadButtons.B;
		}

		if (_controller.ExtendedGamepad.ButtonY.IsPressed)
		{
			reading.Buttons |= GamepadButtons.Y;
		}

		if (_controller.ExtendedGamepad.ButtonX.IsPressed)
		{
			reading.Buttons |= GamepadButtons.X;
		}

		if (_controller.ExtendedGamepad.LeftShoulder.IsPressed)
		{
			reading.Buttons |= GamepadButtons.LeftShoulder;
		}

		if (_controller.ExtendedGamepad.RightShoulder.IsPressed)
		{
			reading.Buttons |= GamepadButtons.RightShoulder;
		}

		if (_controller.ExtendedGamepad.LeftThumbstickButton?.IsPressed == true)
		{
			reading.Buttons |= GamepadButtons.LeftThumbstick;
		}

		if (_controller.ExtendedGamepad.RightThumbstickButton?.IsPressed == true)
		{
			reading.Buttons |= GamepadButtons.RightThumbstick;
		}

		if (_controller.ExtendedGamepad.ButtonMenu.IsPressed)
		{
			reading.Buttons |= GamepadButtons.Menu;
		}

		if (_controller.ExtendedGamepad.ButtonOptions?.IsPressed == true)
		{
			reading.Buttons |= GamepadButtons.View;
		}

		if (_controller.ExtendedGamepad.DPad.Left.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadLeft;
		}

		if (_controller.ExtendedGamepad.DPad.Up.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadUp;
		}

		if (_controller.ExtendedGamepad.DPad.Right.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadRight;
		}

		if (_controller.ExtendedGamepad.DPad.Down.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadDown;
		}

		reading.LeftThumbstickX = _controller.ExtendedGamepad.LeftThumbstick.XAxis.Value;
		reading.LeftThumbstickY = _controller.ExtendedGamepad.LeftThumbstick.YAxis.Value;
		reading.RightThumbstickX = _controller.ExtendedGamepad.RightThumbstick.XAxis.Value;
		reading.RightThumbstickY = _controller.ExtendedGamepad.RightThumbstick.YAxis.Value;
		reading.LeftTrigger = _controller.ExtendedGamepad.LeftTrigger.Value;
		reading.RightTrigger = _controller.ExtendedGamepad.RightTrigger.Value;

		return reading;
	}

	private GamepadReading ReadMicroGamepad()
	{
		var reading = new GamepadReading();

		reading.Timestamp = (ulong)_controller.MicroGamepad!.LastEventTimestamp;

		if (_controller.MicroGamepad.ButtonA.IsPressed)
		{
			reading.Buttons |= GamepadButtons.A;
		}

		if (_controller.MicroGamepad.ButtonX.IsPressed)
		{
			reading.Buttons |= GamepadButtons.X;
		}

		if (_controller.MicroGamepad.ButtonMenu.IsPressed)
		{
			reading.Buttons |= GamepadButtons.Menu;
		}

		if (_controller.MicroGamepad.Dpad.Left.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadLeft;
		}

		if (_controller.MicroGamepad.Dpad.Up.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadUp;
		}

		if (_controller.MicroGamepad.Dpad.Right.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadRight;
		}

		if (_controller.MicroGamepad.Dpad.Down.IsPressed)
		{
			reading.Buttons |= GamepadButtons.DPadDown;
		}

		return reading;
	}

	private static IReadOnlyList<Gamepad> GetGamepadsInternal()
	{
		lock (_gamepadCache)
		{
			var cachedGCControllers = _gamepadCache.Keys.ToArray();
			var connectedGCControllers = GCController.Controllers;

			//remove disconnected
			var disconnectedDevices = cachedGCControllers.Except(connectedGCControllers);
			_gamepadCache.RemoveKeys(disconnectedDevices);

			//add newly connected
			foreach (var controller in connectedGCControllers)
			{
				if (!_gamepadCache.TryGetValue(controller, out var gamepad))
				{
					gamepad = new Gamepad(controller);
					_gamepadCache.Add(controller, gamepad);
				}
			}

			return _gamepadCache.Values.OrderBy(g => g._id).ToArray();
		}
	}

	private static void StartGamepadAdded()
	{
		_didConnectObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			GCController.DidConnectNotification,
			OnDidConnect);
	}

	private static void EndGamepadAdded()
	{
		NSNotificationCenter.DefaultCenter.RemoveObserver(
			_didConnectObserver!);
		_didConnectObserver = null;
	}

	private static void StartGamepadRemoved()
	{
		_didDisconnectObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			GCController.DidDisconnectNotification,
			OnDidDisconnect);
	}

	private static void EndGamepadRemoved()
	{
		NSNotificationCenter.DefaultCenter.RemoveObserver(
			_didDisconnectObserver!);
		_didDisconnectObserver = null;
	}

	private static void OnDidConnect(NSNotification notification)
	{
		var controller = (GCController)notification.Object!;
		Gamepad? gamepad;
		lock (_gamepadCache)
		{
			if (!_gamepadCache.TryGetValue(controller, out gamepad))
			{
				gamepad = new Gamepad(controller);
				_gamepadCache.Add(controller, gamepad);
			}
		}
		_gamepadAddedWrapper.Event?.Invoke(null, gamepad);
	}

	private static void OnDidDisconnect(NSNotification notification)
	{
		var controller = (GCController)notification.Object!;
		Gamepad? gamepad;
		lock (_gamepadCache)
		{
			if (!_gamepadCache.TryGetValue(controller, out gamepad))
			{
				gamepad = new Gamepad(controller);
				_gamepadCache.Add(controller, gamepad);
			}
		}
		_gamepadRemovedWrapper.Event?.Invoke(null, gamepad);
	}
}
