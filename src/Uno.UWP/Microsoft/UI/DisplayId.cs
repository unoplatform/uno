using System;

namespace Microsoft.UI;

public struct DisplayId : IEquatable<DisplayId>
{
	public ulong Value;

	public DisplayId(ulong _Value)
		=> Value = _Value;

	public static bool operator ==(DisplayId x, DisplayId y)
		=> x.Value == y.Value;

	public static bool operator !=(DisplayId x, DisplayId y)
		=> x.Value != y.Value;

	public bool Equals(DisplayId other)
		=> Value == other.Value;

	public override bool Equals(object obj)
		=> (obj is DisplayId y) && Equals(y);

	public override int GetHashCode()
		=> Value.GetHashCode();
}
