#nullable enable

namespace Uno.UI;

public static partial class FeatureConfiguration
{
	/// <summary>
	/// Opt-in switches for the 2026 WinUI performance parity optimizations.
	/// </summary>
	/// <remarks>
	/// These optimizations change internal visual tree shapes or allocation patterns, so they are opt-in
	/// and default to <see cref="EnableAll"/>. Individual switches live either in this group or in their
	/// existing feature group, such as <see cref="FeatureConfiguration.Style"/>.
	/// </remarks>
	public static partial class Perf2026
	{
		/// <summary>
		/// Enables every <see cref="Perf2026"/> optimization that has not been configured individually,
		/// including optimized default styles and deferred overridden style values.
		/// Defaults to false.
		/// </summary>
		/// <remarks>Set this during application startup, before controls or theme resources are created.</remarks>
		public static bool EnableAll { get; set; }
	}
}
