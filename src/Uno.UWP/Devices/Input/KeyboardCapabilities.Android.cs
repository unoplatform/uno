using Android.App;
using Android.Content.Res;

namespace Windows.Devices.Input;

public partial class KeyboardCapabilities
{
	private partial int GetKeyboardPresent()
	{
		var configuration = Application.Context.Resources?.Configuration;
		if (configuration is null)
		{
			return 0;
		}

		return configuration.Keyboard != KeyboardType.Nokeys
			&& configuration.HardKeyboardHidden == HardKeyboardHidden.No
			? 1
			: 0;
	}
}
