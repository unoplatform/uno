#nullable disable

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
namespace Windows.Gaming.Input;

/// <summary>
/// The core interface required to be implemented by all controller devices,
/// regardless of their actual type (gamepad, racing wheel, flight stick, and so on).
/// </summary>
public partial interface IGameController
{
}
#endif
