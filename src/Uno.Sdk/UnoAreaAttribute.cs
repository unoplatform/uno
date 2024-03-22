using System;

namespace Uno.Sdk;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class UnoAreaAttribute(UnoArea area) : Attribute
{
	public UnoArea Area => area;
}
