#nullable enable

using System;

namespace Windows.UI.Input.Preview.Injection;

public partial struct InjectedInputRectangle
{
	public int Left;

	public int Top;

	public int Bottom;

	public int Right;

	public bool Equals(InjectedInputRectangle other) =>
		Left == other.Left && Top == other.Top && Bottom == other.Bottom && Right == other.Right;

	public override bool Equals(object? obj) => obj is InjectedInputRectangle other && Equals(other);

	public override int GetHashCode()
	{
		var hashCode = -1083629557;
		hashCode = hashCode * -1521134295 + Left.GetHashCode();
		hashCode = hashCode * -1521134295 + Top.GetHashCode();
		hashCode = hashCode * -1521134295 + Bottom.GetHashCode();
		hashCode = hashCode * -1521134295 + Right.GetHashCode();
		return hashCode;
	}

	public static bool operator ==(InjectedInputRectangle left, InjectedInputRectangle right) => left.Equals(right);

	public static bool operator !=(InjectedInputRectangle left, InjectedInputRectangle right) => !(left == right);
}
