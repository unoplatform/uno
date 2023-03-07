using System;
using System.ComponentModel;

namespace Windows.Foundation;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct HResult : IEquatable<HResult>
{
	/// <summary>An integer that describes an error.</summary>
	public int Value;

	public override bool Equals(object obj) => obj is HResult result && Equals(result);
	public bool Equals(HResult other) => Value == other.Value;
	public override int GetHashCode() => -1937169414 + Value.GetHashCode();

	public static bool operator ==(HResult left, HResult right) => left.Equals(right);
	public static bool operator !=(HResult left, HResult right) => !left.Equals(right);
}
