#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Gaming.Input
{
    public partial class Gamepad
    {
		private readonly static object _syncLock = new object();

		private static EventHandler<Gamepad> _gamepadAdded;
		private static EventHandler<Gamepad> _gamepadRemoved;

		public static IReadOnlyList<Gamepad> Gamepads => GetGamepadsInternal();

		public static event EventHandler<Gamepad> GamepadAdded
		{			
			add
			{
				lock (_syncLock)
				{
					var isFirstSubscriber = _gamepadAdded == null;
					_gamepadAdded += value;
					if (isFirstSubscriber)
					{
						StartGamepadAdded();
					}
				}
			}		
			remove
			{
				lock (_syncLock)
				{					
					_gamepadAdded -= value;
					if(_gamepadAdded == null)
					{
						EndGamepadAdded();
					}
				}
			}
		}
		
		public static event EventHandler<Gamepad> GamepadRemoved
		{			
			add
			{
				lock (_syncLock)
				{
					var isFirstSubscriber = _gamepadRemoved == null;
					_gamepadRemoved += value;
					if (isFirstSubscriber)
					{
						StartGamepadRemoved();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_gamepadRemoved -= value;
					if (_gamepadRemoved == null)
					{
						EndGamepadRemoved();
					}
				}
			}
		}

		internal static void OnGamepadAdded(Gamepad gamepad) =>
			_gamepadAdded?.Invoke(null, gamepad);

		internal static void OnGamepadRemoved(Gamepad gamepad) =>
			_gamepadRemoved?.Invoke(null, gamepad);
    }
}
#endif
