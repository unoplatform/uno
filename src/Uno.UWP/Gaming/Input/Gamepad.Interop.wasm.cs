using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Gaming.Input
{
	internal partial class Gamepad
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Gaming.Input.Gamepad";

			[JSImport($"{JsType}.endGamepadAdded")]
			internal static partial void EndGamepadAdded();

			[JSImport($"{JsType}.endGamepadRemoved")]
			internal static partial void EndGamepadRemoved();

			[JSImport($"{JsType}.getConnectedGamepadIds")]
			internal static partial string GetConnectedGamepadIds();

			[JSImport($"{JsType}.getReading")]
			internal static partial string GetReading(double id);

			[JSImport($"{JsType}.startGamepadAdded")]
			internal static partial void StartGamepadAdded();

			[JSImport($"{JsType}.startGamepadRemoved")]
			internal static partial void StartGamepadRemoved();
		}
	}
}
