// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.Globalization;

namespace Uno.Sdk.MacDev;

public class PDate(DateTime value) : PValueObject<DateTime>(value)
{
	public override PObject Clone() => new PDate(Value);

	public override PObjectType Type => PObjectType.Date;

	public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
	{
		if (DateTime.TryParse(text, formatProvider, DateTimeStyles.None, out var result))
		{
			Value = result;
			return true;
		}
		return false;
	}
}
