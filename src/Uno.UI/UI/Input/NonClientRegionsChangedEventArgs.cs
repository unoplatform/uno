namespace Microsoft.UI.Input;

/// <summary>
/// Provides data for the event that occurs when the set of non-client regions for a window changes.
/// Contains the list of non-client regions that were added, removed, or otherwise changed.
/// </summary>
public partial class NonClientRegionsChangedEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NonClientRegionsChangedEventArgs"/> class.
	/// This constructor is internal because instances are provided by the framework.
	/// </summary>
	internal NonClientRegionsChangedEventArgs(NonClientRegionKind[] changedRegions) =>
		ChangedRegions = changedRegions;

	/// <summary>
	/// Gets an array of <see cref="NonClientRegionKind"/> values that indicate which non-client
	/// regions have changed for the window. Examples include caption, borders, and caption buttons.
	/// </summary>
	public NonClientRegionKind[] ChangedRegions { get; }
}
