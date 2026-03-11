using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Windows.Foundation;

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

public partial struct ManipulationDelta : IEquatable<ManipulationDelta>
{
	/// <summary>
	/// A manipulation that does nothing.
	/// This differs to 'default' by having a 'Scale' of 1.
	/// </summary>
	internal static ManipulationDelta Empty { get; } = new ManipulationDelta
	{
		Translation = Point.Zero,
		Rotation = 0,
		Scale = 1,
		Expansion = 0
	};

	public Point Translation;
	public float Scale;
	public float Rotation;
	public float Expansion;

	// NOTE: Equality implementation should be modified if a new field/property is added.

	// IsEmpty is intentionally not included in equality since it's calculated from the other fields.
	internal bool IsEmpty => Translation == Point.Zero && Rotation == 0 && Scale == 1 && Expansion == 0;

	[Pure]
	internal ManipulationDelta Add(ManipulationDelta right)
		=> Add(this, right);

	[Pure]
	internal static ManipulationDelta Add(ManipulationDelta left, ManipulationDelta right)
		=> new()
		{
			Translation = new Point(
				left.Translation.X + right.Translation.X,
				left.Translation.Y + right.Translation.Y),
			Rotation = left.Rotation + right.Rotation,
			Scale = left.Scale * right.Scale,
			Expansion = left.Expansion + right.Expansion
		};

	[Pure]
	// Note: We should apply a velocity factor to thresholds to determine if isSignificant
	internal bool IsSignificant(GestureRecognizer.Manipulation.Thresholds thresholds)
		=> Math.Abs(Translation.X) >= thresholds.TranslateX
		|| Math.Abs(Translation.Y) >= thresholds.TranslateY
		|| Math.Abs(Rotation) >= thresholds.Rotate
		|| Math.Abs(Expansion) >= thresholds.Expansion;

	/// <inheritdoc />
	[Pure]
	public override string ToString()
		=> $"x:{Translation.X:N0};y:{Translation.Y:N0};θ:{Rotation:F2};s:{Scale:F2};e:{Expansion:F2}";

	#region Equality Members
	[Pure]
	public override bool Equals(object obj)
		=> obj is ManipulationDelta delta && Equals(delta);

	[Pure]
	public bool Equals(ManipulationDelta other)
		=> EqualityComparer<Point>.Default.Equals(Translation, other.Translation)
			&& Scale == other.Scale
			&& Rotation == other.Rotation
			&& Expansion == other.Expansion;

	[Pure]
	public override int GetHashCode()
	{
		var hashCode = 626270564;
		hashCode = hashCode * -1521134295 + Translation.GetHashCode();
		hashCode = hashCode * -1521134295 + Scale.GetHashCode();
		hashCode = hashCode * -1521134295 + Rotation.GetHashCode();
		hashCode = hashCode * -1521134295 + Expansion.GetHashCode();
		return hashCode;
	}

	public static bool operator ==(ManipulationDelta left, ManipulationDelta right) => left.Equals(right);
	public static bool operator !=(ManipulationDelta left, ManipulationDelta right) => !left.Equals(right);
	#endregion
}
