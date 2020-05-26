#if __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using Uno;
using Uno.Extensions;
using Uno.Foundation;

namespace Windows.Gaming.Input
{
	public partial class Gamepad
    {
		private const string JsType = "Windows.Gaming.Input.Gamepad";
		private const char IdSeparator = ';';

		private readonly static Dictionary<string, Gamepad> _gamepadCache =
			 new Dictionary<string, Gamepad>();

		private readonly string _id;

		private Gamepad(string id)
		{
			_id = id;
		}

		[Preserve]
		public static int DispatchGamepadAdded(string id)
		{
			Gamepad gamepad;
			lock (_gamepadCache)
			{
				if (!_gamepadCache.TryGetValue(id, out gamepad))
				{
					gamepad = new Gamepad(id);
					_gamepadCache.Add(id, gamepad);
				}
			}
			_gamepadAdded?.Invoke(null, gamepad);
			return 0;
		}

		[Preserve]
		public static int DispatchGamepadRemoved(string id)
		{
			Gamepad gamepad;
			lock (_gamepadCache)
			{
				if (!_gamepadCache.TryGetValue(id, out gamepad))
				{
					gamepad = new Gamepad(id);
					_gamepadCache.Add(id, gamepad);
				}
			}
			_gamepadRemoved?.Invoke(null, gamepad);
			return 0;
		}

		private static IReadOnlyList<Gamepad> GetGamepadsInternal()
		{
			var getConnectedGamepadIdsCommand = $"{JsType}.getConnectedGamepadIds()";
			var serializedIds = WebAssemblyRuntime.InvokeJS(getConnectedGamepadIdsCommand);
			var connectedGamepadIds = serializedIds.Split(new[] { IdSeparator }, StringSplitOptions.RemoveEmptyEntries);

			lock (_gamepadCache)
			{
				var cachedGCControllers = _gamepadCache.Keys.ToArray();
				
				//remove disconnected
				var disconnectedDevices = cachedGCControllers.Except(connectedGamepadIds);
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
			var startGamepadAddedCommand = $"{JsType}.startGamepadAdded()";
			WebAssemblyRuntime.InvokeJS(startGamepadAddedCommand);
		}

		private static void EndGamepadAdded()
		{
			var endGamepadAddedCommand = $"{JsType}.endGamepadAdded()";
			WebAssemblyRuntime.InvokeJS(endGamepadAddedCommand);
		}

		private static void StartGamepadRemoved()
		{
			var startGamepadRemovedCommand = $"{JsType}.startGamepadRemoved()";
			WebAssemblyRuntime.InvokeJS(startGamepadRemovedCommand);
		}

		private static void EndGamepadRemoved()
		{
			var endGamepadRemovedCommand = $"{JsType}.endGamepadRemoved()";
			WebAssemblyRuntime.InvokeJS(endGamepadRemovedCommand);
		}
	}
}
#endif
