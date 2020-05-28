#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input
{
	public  partial class KeyboardCapabilities 
	{
		[global::Uno.NotImplemented]
		public  int KeyboardPresent
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.KeyboardCapabilities", "KeyboardCapabilities.KeyboardPresent");
				return 0;
			}
		}

		public KeyboardCapabilities() 
		{
		}
	}
}
