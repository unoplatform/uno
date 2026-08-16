#nullable enable

namespace Uno.UI;

public static partial class FeatureConfiguration
{
	/// <summary>
	/// Opt-in switches for the 2026 WinUI performance parity optimizations.
	/// </summary>
	/// <remarks>
	/// These optimizations change internal visual tree shapes or allocation patterns, so they are opt-in
	/// and default to <see cref="EnableAll"/>. Each optimization declares its own property in a dedicated
	/// <c>FeatureConfiguration.Perf2026.&lt;Feature&gt;.cs</c> partial file.
	/// </remarks>
	public static partial class Perf2026
	{
		/// <summary>
		/// Enables every <see cref="Perf2026"/> optimization that has not been configured individually.
		/// Defaults to false.
		/// </summary>
		public static bool EnableAll { get; set; }
	}
}
