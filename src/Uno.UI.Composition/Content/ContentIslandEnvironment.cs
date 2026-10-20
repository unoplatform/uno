namespace Microsoft.UI.Content;

/// <summary>
/// Provides information about the environment a ContentIsland is hosted in.
/// </summary>
public partial class ContentIslandEnvironment
{
	internal ContentIslandEnvironment(WindowId appWindowId) => AppWindowId = appWindowId;

	/// <summary>
	/// Gets the identifier of the top-level AppWindow hosting the island.
	/// </summary>
	public WindowId AppWindowId { get; }
}
