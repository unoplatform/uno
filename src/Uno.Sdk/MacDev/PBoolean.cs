// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;

namespace Uno.Sdk.MacDev;

public class PBoolean(bool value) : PValueObject<bool>(value)
{
	public override PObject Clone() => new PBoolean(Value);

	public override PObjectType Type => PObjectType.Boolean;

	public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
	{
		const StringComparison ic = StringComparison.OrdinalIgnoreCase;

		if ("true".Equals(text, ic) || "yes".Equals(text, ic))
		{
			Value = true;
			return true;
		}

		if ("false".Equals(text, ic) || "no".Equals(text, ic))
		{
			Value = false;
			return true;
		}

		return false;
	}
}
