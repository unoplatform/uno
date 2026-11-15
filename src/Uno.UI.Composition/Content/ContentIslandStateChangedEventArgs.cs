namespace Microsoft.UI.Content;

/// <summary>
/// Contains event data for the ContentIsland.StateChanged event.
/// </summary>
public partial class ContentIslandStateChangedEventArgs
{
	private ContentIslandStateChangedEventArgs()
	{
	}

	internal static ContentIslandStateChangedEventArgs RasterizationScaleChange { get; } = new() { DidRasterizationScaleChange = true };

	internal static ContentIslandStateChangedEventArgs ActualSizeChange { get; } = new() { DidActualSizeChange = true };

	internal static ContentIslandStateChangedEventArgs SiteVisibleChange { get; } = new() { DidSiteVisibleChange = true };

	/// <summary>
	/// Gets whether the ContentIsland size changed.
	/// </summary>
	public bool DidActualSizeChange { get; internal set; }

	/// <summary>
	/// Gets whether the ContentIsland layout direction changed.
	/// </summary>
	public bool DidLayoutDirectionChange { get; internal set; }

	/// <summary>
	/// Gets whether the ContentIsland rasterization scale changed.
	/// </summary>
	public bool DidRasterizationScaleChange { get; internal set; }

	/// <summary>
	/// Gets whether enabled state of the ContentIsland changed.
	/// </summary>
	public bool DidSiteEnabledChange { get; internal set; }

	/// <summary>
	/// Gets whether the ContentIsland visibility changed.
	/// </summary>
	public bool DidSiteVisibleChange { get; internal set; }
}
