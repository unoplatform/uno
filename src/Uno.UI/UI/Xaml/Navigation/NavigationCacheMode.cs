namespace Windows.UI.Xaml.Navigation;

/// <summary>
/// Specifies caching characteristics for a page involved in a navigation.
/// </summary>
public enum NavigationCacheMode
{
	/// <summary>
	/// The page is never cached and a new instance of the page is created on each visit.
	/// </summary>
	Disabled = 0,

	/// <summary>
	/// The page is cached and the cached instance is reused for every visit regardless of the cache size for the frame.
	/// </summary>
	Required = 1,

	/// <summary>
	/// The page is cached, but the cached instance is discarded when the size of the cache for the frame is exceeded.
	/// </summary>
	Enabled = 2,
}
