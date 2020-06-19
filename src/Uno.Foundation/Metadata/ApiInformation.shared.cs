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
					// See https://docs.microsoft.com/en-us/uwp/extension-sdks/windows-universal-sdk
					return majorVersion <= 6; // SDK 10.0.17134.1

				default:
					return false;
			}
		}
	}
}
