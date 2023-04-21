#nullable enable

using System;

namespace Windows.UI.Input.Preview.Injection;

public partial struct InjectedInputPoint
{
	public int PositionX;

	public int PositionY;

	public bool Equals(InjectedInputPoint other) => PositionX == other.PositionX && PositionY == other.PositionY;

	public override bool Equals(object? obj) => obj is InjectedInputPoint other && Equals(other);

	public override int GetHashCode() => new { PositionX, PositionY }.GetHashCode();

	public static bool operator ==(InjectedInputPoint left, InjectedInputPoint right) => left.Equals(right);

	public static bool operator !=(InjectedInputPoint left, InjectedInputPoint right) => !(left == right);
}
