using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Foundation.Metadata
{
	public partial class ApiInformation
	{
		public static bool IsApiContractPresent(string contractName, ushort majorVersion)
			=> IsApiContractPresent(contractName, majorVersion, 0);

		public static bool IsApiContractPresent(string contractName, ushort majorVersion, ushort minorVersion)
		{
			switch (contractName)
			{
				case "Windows.Foundation.UniversalApiContract":
					// See C:\Program Files (x86)\Windows Kits\10\References\[version]\Windows.Foundation.UniversalApiContract
					return majorVersion <= 10; // SDK 10.0.19041.1

				case "Uno.WinUI":
#if HAS_UNO_WINUI
					return true;
#else
					return false;
#endif
				default:
					return false;
			}
		}
	}
}
