using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI;

public struct IconId : IEquatable<IconId>
{
	public ulong Value;

	public IconId(ulong _Value)
	{
		Value = _Value;
	}

	public static bool operator ==(IconId x, IconId y)
		=> x.Value == y.Value;

	public static bool operator !=(IconId x, IconId y)
		=> x.Value != y.Value;

	public bool Equals(IconId other)
		=> Value == other.Value;

	public override bool Equals(object obj)
		=> (obj is IconId y) && Equals(y);

	public override int GetHashCode()
		=> Value.GetHashCode();
}
