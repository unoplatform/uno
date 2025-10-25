namespace Microsoft.UI.Content;

/// <summary>
/// Provides general environment information to a ContentIsland.
/// </summary>
public partial class ContentIslandEnvironment
{
	internal ContentIslandEnvironment(WindowId appWindowId)
	{
		AppWindowId = appWindowId;
	}

	/// <summary>
	/// Gets the ID of the top-level window.
	/// </summary>
	public WindowId AppWindowId { get; }
}
