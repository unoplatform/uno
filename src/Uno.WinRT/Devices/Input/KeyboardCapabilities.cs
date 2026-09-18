#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input
{
	public partial class KeyboardCapabilities
	{
		public int KeyboardPresent => GetKeyboardPresent();

		public KeyboardCapabilities()
		{
		}

		private partial int GetKeyboardPresent();
	}
}
