#if IS_UNIT_TESTS || __NETSTD_REFERENCE__ || __TVOS__
namespace Windows.Devices.Input;

public partial class KeyboardCapabilities
{
	private partial int GetKeyboardPresent() => 1;
}
#endif
