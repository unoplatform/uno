namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify how an AnimatedVisualPlayer caches animations when the player is idle.
/// </summary>
public enum PlayerAnimationOptimization
{
	/// <summary>
	/// The player optimizes animation caching for lower latency.
	/// </summary>
	Latency = 0,

	/// <summary>
	/// The player optimizes animation caching for lower resource usage.
	/// </summary>
	Resources = 1,
}
