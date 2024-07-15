// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;

namespace Uno.Sdk.MacDev;

public class PData(byte[] value) : PValueObject<byte[]>(value ?? Empty)
{
	static readonly byte[] Empty = [];

	public override PObject Clone() => new PData(Value);

	public override PObjectType Type => PObjectType.Data;

	public override bool TrySetValueFromString(string text, IFormatProvider formatProvider) => false;
}
