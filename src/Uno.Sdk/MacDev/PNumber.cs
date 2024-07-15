// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.Globalization;

namespace Uno.Sdk.MacDev;

public class PNumber(long value) : PValueObject<long>(value)
{
	public override PObject Clone() => new PNumber(Value);

	public override PObjectType Type => PObjectType.Number;

	public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
	{
		if (long.TryParse(text, NumberStyles.Integer, formatProvider, out var result))
		{
			Value = result;
			return true;
		}
		return false;
	}
}
