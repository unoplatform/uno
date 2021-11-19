using System;
using System.Diagnostics.Contracts;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial struct ManipulationVelocities
	{
		internal static ManipulationVelocities Empty { get; } = new ManipulationVelocities();

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

		// Note: We should apply a velocity factor to thresholds to determine if isSignificant
		internal bool IsAnyAbove(GestureRecognizer.Manipulation.Thresholds thresholds)
			=> Math.Abs(Linear.X) > thresholds.TranslateX
				|| Math.Abs(Linear.Y) > thresholds.TranslateY
				|| Math.Abs(Angular) > thresholds.Rotate
				|| Math.Abs(Expansion) > thresholds.Expansion;

		/// <inheritdoc />
		[Pure]
		public override string ToString()
			=> $"x:{Linear.X:N0};y:{Linear.Y:N0};θ:{Angular};e:{Expansion:F2}";
	}
}
