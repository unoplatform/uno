#if __IOS__ || __MACOS__
using System.Collections.Generic;
using System.Linq;
using Foundation;
using GameController;
using Uno.Extensions;

namespace Windows.Gaming.Input
{
	public partial class Gamepad
	{
		private readonly static Dictionary<GCController, Gamepad> _gamepadCache =
			new Dictionary<GCController, Gamepad>();

		private static int _nextGamepadId = 1;
		private static NSObject _didConnectObserver;
		private static NSObject _didDisconnectObserver;
		private readonly GCController _controller;
		private readonly int _id;

		private Gamepad(GCController controller)
		{
			_controller = controller;
			_id = _nextGamepadId++;
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
				_didConnectObserver);
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
				_didDisconnectObserver);
			_didDisconnectObserver = null;
		}

		private static void OnDidConnect(NSNotification notification)
		{
			var controller = (GCController)notification.Object;
			Gamepad gamepad;
			lock (_gamepadCache)
			{
				if (!_gamepadCache.TryGetValue(controller, out gamepad))
				{
					gamepad = new Gamepad(controller);
					_gamepadCache.Add(controller, gamepad);
				}
			}
			_gamepadAdded?.Invoke(null, gamepad);
		}

		private static void OnDidDisconnect(NSNotification notification)
		{
			var controller = (GCController)notification.Object;
			Gamepad gamepad;
			lock (_gamepadCache)
			{
				if (!_gamepadCache.TryGetValue(controller, out gamepad))
				{
					gamepad = new Gamepad(controller);
					_gamepadCache.Add(controller, gamepad);
				}
			}
			_gamepadRemoved?.Invoke(null, gamepad);
		}
	}
}
#endif
