// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.Globalization;

namespace Uno.Sdk.MacDev;

public class PReal(double value) : PValueObject<double>(value)
{
	public override PObject Clone() => new PReal(Value);

	public override PObjectType Type => PObjectType.Real;

	public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
	{
		if (double.TryParse(text, NumberStyles.AllowDecimalPoint, formatProvider, out var result))
		{
			Value = result;
			return true;
		}
		return false;
	}
}
