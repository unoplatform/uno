#if __SKIA__
using System.Collections.Generic;

namespace Uno.Gaming.Input.Internal;

internal interface IGamepadExtension
{
	IReadOnlyList<Windows.Gaming.Input.Gamepad> GetGamepads();
	Windows.Gaming.Input.GamepadReading GetCurrentReading(Windows.Gaming.Input.Gamepad gamepad);
	void StartMonitoring();
	void StopMonitoring();
}
#endif
