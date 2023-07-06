using System.Diagnostics;

namespace Windows.Foundation.Metadata;

public partial class ApiInformation
{
	public static bool IsApiContractPresent(string contractName, ushort majorVersion)
		=> IsApiContractPresent(contractName, majorVersion, 0);

	public static bool IsApiContractPresent(string contractName, ushort majorVersion, ushort minorVersion)
	{
		switch (contractName)
		{
			case "Windows.Foundation.FoundationContract":
				// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.Foundation.FoundationContract
				return majorVersion <= 4; // SDK 10.0.22000.0

			case "Windows.Foundation.UniversalApiContract":
				// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.Foundation.UniversalApiContract
				return majorVersion <= 14; // SDK 10.0.22000.0

			case "Windows.Phone.PhoneContract":
				// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.Phone.PhoneContract
				return majorVersion <= 1; // SDK 10.0.22000.0					

			case "Windows.Networking.Connectivity.WwanContract":
				// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.Networking.Connectivity.WwanContract
				return majorVersion <= 2; // SDK 10.0.22000.0

			case "Windows.ApplicationModel.Calls.CallsPhoneContract":
				// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.ApplicationModel.Calls.CallsPhoneContract
				return majorVersion <= 6; // SDK 10.0.22000.0

			case "Windows.Services.Store.StoreContract":
				// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.Services.Store.StoreContract
				return majorVersion <= 4; // SDK 10.0.22000.0

			case "Windows.UI.Xaml.Hosting.HostingContract":
				// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.UI.Xaml.Hosting.HostingContract
				return majorVersion <= 5; // SDK 10.0.22000.0			

			case "Uno.WinUI":
#if HAS_UNO_WINUI
				return true;
#else
				return false;
#endif
			default:
				Debug.Fail(
					"Contract " + contractName + " is not known." +
					"If implemented, ensure it is added " +
					"in ApiInformation.IsApiContractPresent.");

				return false;
		}
	}
}
