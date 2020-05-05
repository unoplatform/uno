#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Hardware.Input;
using Android.Views;
using Javax.Security.Auth;
using Uno.Extensions;
using Uno.UI;

namespace Windows.Gaming.Input
{
	public partial class Gamepad
	{
		private static InputManager _inputManager;
		private static InputManager.IInputDeviceListener _listener;
		private static Dictionary<int, Gamepad> _gamepadCache = new Dictionary<int, Gamepad>();

		private readonly int _nativeDeviceId;

		public Gamepad(int nativeDeviceId)
		{
			_nativeDeviceId = nativeDeviceId;
		}

		private static IReadOnlyList<Gamepad> GetGamepadsInternal()
		{
			var cachedDeviceIds = _gamepadCache.Keys.ToArray();
			var connectedIds = InputDevice.GetDeviceIds();

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
						_gamepadCache.Add(deviceId, gamepad);
					}
				}
			}

			return _gamepadCache.Values.OrderBy(g => g._nativeDeviceId).ToArray();
		}

		private static void EnsureInputManagerInitialized()
		{
			if (_inputManager == null)
			{
				_inputManager = (InputManager)ContextHelper.Current.GetSystemService(Context.InputService);
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
			_inputManager.RegisterInputDeviceListener(_listener, null);
		}

		private static void DetachInputDeviceListener()
		{
			if (_gamepadAdded != null || _gamepadRemoved != null)
			{
				return;
			}

			_inputManager.UnregisterInputDeviceListener(_listener);
			_listener?.Dispose();
			_listener = null;
		}

		private static bool IsGamepad(InputDevice inputDevice) =>
			inputDevice.Sources.HasFlag(InputSourceType.Gamepad);

		private static bool TryGetOrCreateGamepad(int deviceId, out Gamepad gamepad)
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
						_gamepadCache.Add(deviceId, gamepad);						
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
}
#endif
