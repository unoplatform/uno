// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;

namespace Uno.Sdk.MacDev;

public class PString : PValueObject<string>
{
	public PString(string value) : base(value)
	{
		if (value is null)
		{
			throw new ArgumentNullException(nameof(value));
		}
	}

	public override PObject Clone() => new PString(Value);

	public override PObjectType Type => PObjectType.String;

	public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
	{
		Value = text;
		return true;
	}
}
