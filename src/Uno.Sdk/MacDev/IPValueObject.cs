// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;

namespace Uno.Sdk.MacDev;

public interface IPValueObject
{
	object Value { get; set; }
	bool TrySetValueFromString(string text, IFormatProvider formatProvider);
}
