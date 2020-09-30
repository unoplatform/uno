#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Roslyn;

namespace Windows.Foundation.Metadata
{
	public partial class ApiInformation
	{
		public static bool IsApiContractNotPresent(string contractName, ushort majorVersion) => !IsApiContractPresent(contractName, majorVersion);

		internal static bool IsTypePresent(string typeName, RoslynMetadataHelper metadataHelper)
		{
			var typeSymbol = metadataHelper.FindTypeByFullName(typeName);
			if (typeSymbol == null)
			{
				return false;
			}

			if (typeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "NotImplementedAttribute"))
			{
				return false;
			}

			return true;
		}

		internal static bool IsTypeNotPresent(string typeName, RoslynMetadataHelper metadataHelper) => !IsTypePresent(typeName, metadataHelper);
	}
}
