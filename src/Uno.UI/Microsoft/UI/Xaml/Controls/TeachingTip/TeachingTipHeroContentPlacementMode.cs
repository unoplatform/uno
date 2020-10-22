// MUX Reference TeachingTip.idl, commit de78834

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Defines constants that indicate the preferred location of the HeroContent within a teaching tip.
	/// </summary>
	public enum TeachingTipHeroContentPlacementMode
	{
		/// <summary>
		/// The header of the teaching tip.
		/// The hero content might be moved to the footer to avoid intersecting with the tail of the targeted teaching tip.
		/// </summary>
		Auto,

		/// <summary>
		/// The header of the teaching tip.
		/// </summary>
		Top,

		/// <summary>
		/// The footer of the teaching tip.
		/// </summary>
		Bottom,
	}
}
