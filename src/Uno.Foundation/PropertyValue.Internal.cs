using System;

namespace Windows.Foundation
{
	// Those methods are for WinUI code compatibility
	partial class PropertyValue
	{
		internal static void CreateFromDateTime(DateTimeOffset date, out object value)
		{
			value = CreateDateTime(date);
		}
	}
}
