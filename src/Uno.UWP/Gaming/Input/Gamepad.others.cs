#if !__ANDROID__ && !__IOS__ && !__TVOS__ && !__WASM__ && !__SKIA__
namespace Windows.Gaming.Input;

/// <summary>
/// Represents a gamepad.
/// </summary>
public partial class Gamepad
{
	private Gamepad()
	{
		// Ensure public constructor is not available
		// on unsupported platforms.
	}
}
#endif
