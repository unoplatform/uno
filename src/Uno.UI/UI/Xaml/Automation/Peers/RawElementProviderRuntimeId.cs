using System;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Represents the runtime identifier for an automation element.
/// </summary>
public partial struct RawElementProviderRuntimeId : IEquatable<RawElementProviderRuntimeId>
{
	/// <summary>
	/// The first part of the identifier.
	/// </summary>
	public uint Part1;

	/// <summary>
	/// The second part of the identifier.
	/// </summary>
	public uint Part2;

	public bool Equals(RawElementProviderRuntimeId other) => Part1 == other.Part1 && Part2 == other.Part2;

	public override bool Equals(object obj) => obj is RawElementProviderRuntimeId other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Part1, Part2);

	public static bool operator ==(RawElementProviderRuntimeId left, RawElementProviderRuntimeId right) => left.Equals(right);

	public static bool operator !=(RawElementProviderRuntimeId left, RawElementProviderRuntimeId right) => !left.Equals(right);
}
