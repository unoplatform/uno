#nullable enable

using System;
using System.Globalization;

namespace Windows.Foundation;

public static partial class TokenizerHelper
{
	/// <summary>
	/// Gets the separator used between numbers in a list: ';' when the culture's decimal separator is ','.
	/// </summary>
	public static char GetNumericListSeparator(IFormatProvider? provider)
	{
		var decimalSeparator = NumberFormatInfo.GetInstance(provider).NumberDecimalSeparator;
		return decimalSeparator.Length > 0 && decimalSeparator[0] == ',' ? ';' : ',';
	}
}
