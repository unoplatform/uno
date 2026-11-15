using System;

namespace Windows.Graphics;

/// <summary>
/// Corresponds to the LUID (Locally Unique Identifier) associated with a graphics adapter.
/// </summary>
public partial struct DisplayAdapterId : IEquatable<DisplayAdapterId>
{
	/// <summary>
	/// The low part of the LUID.
	/// </summary>
	public uint LowPart;

	/// <summary>
	/// The high part of the LUID.
	/// </summary>
	public int HighPart;

	public bool Equals(DisplayAdapterId other) => LowPart == other.LowPart && HighPart == other.HighPart;

	public override bool Equals(object obj) => obj is DisplayAdapterId other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(LowPart, HighPart);

	public static bool operator ==(DisplayAdapterId left, DisplayAdapterId right) => left.Equals(right);

	public static bool operator !=(DisplayAdapterId left, DisplayAdapterId right) => !left.Equals(right);
}
