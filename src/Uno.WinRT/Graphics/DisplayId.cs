using System;

namespace Windows.Graphics;

/// <summary>
/// Represents the identifier of a logical display.
/// </summary>
public partial struct DisplayId : IEquatable<DisplayId>
{
	/// <summary>
	/// The identifier of a logical display.
	/// </summary>
	public ulong Value;

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
