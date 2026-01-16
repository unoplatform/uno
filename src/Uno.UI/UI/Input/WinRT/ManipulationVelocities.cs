// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Input
{
	public partial struct ManipulationVelocities : IEquatable<ManipulationVelocities>
	{
		internal static ManipulationVelocities Empty { get; }

		/// <summary>
		/// The expansion, or scaling, velocity in device-independent pixel (DIP) per millisecond.
		/// </summary>
		public Point Linear;

		/// <summary>
		/// The rotational velocity in degrees per millisecond.
		/// </summary>
		public float Angular;

		/// <summary>
		/// The expansion, or scaling, velocity in device-independent pixel (DIP) per millisecond.
		/// </summary>
		public float Expansion;

		// NOTE: Equality implementation should be modified if a new field/property is added.

		// Note: We should apply a velocity factor to thresholds to determine if isSignificant
		internal bool IsAnyAbove(GestureRecognizer.Manipulation.Thresholds thresholds)
			=> Math.Abs(Linear.X) > thresholds.TranslateX
				|| Math.Abs(Linear.Y) > thresholds.TranslateY
				|| Math.Abs(Angular) > thresholds.Rotate
				|| Math.Abs(Expansion) > thresholds.Expansion;

		internal Point Apply(Point point, double δTimeMs)
			=> new(point.X + Linear.X * δTimeMs, point.Y + Linear.Y * δTimeMs);

		/// <inheritdoc />
		public override string ToString()
			=> $"x:{Linear.X:F2};y:{Linear.Y:F2};θ:{Angular};e:{Expansion:F2}";

		#region Equality Members
		public override bool Equals(object obj) => obj is ManipulationVelocities velocities && Equals(velocities);

		public bool Equals(ManipulationVelocities other)
		{
			return EqualityComparer<Point>.Default.Equals(Linear, other.Linear) &&
				Angular == other.Angular &&
				Expansion == other.Expansion;
		}

		public override int GetHashCode()
		{
			var hashCode = 560214819;
			hashCode = hashCode * -1521134295 + Linear.GetHashCode();
			hashCode = hashCode * -1521134295 + Angular.GetHashCode();
			hashCode = hashCode * -1521134295 + Expansion.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(ManipulationVelocities left, ManipulationVelocities right) => left.Equals(right);
		public static bool operator !=(ManipulationVelocities left, ManipulationVelocities right) => !left.Equals(right);
		#endregion
	}
}
#endif
