using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Foundation.Metadata
{
	public partial class ApiInformation
	{
		public static bool IsApiContractNotPresent(string contractName, ushort majorVersion) => !IsApiContractPresent(contractName, majorVersion);
	}
}
