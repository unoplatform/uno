namespace Microsoft.UI.Content;

/// <summary>
/// Provides information about the environment that hosts a ContentIsland.
/// </summary>
public partial class ContentIslandEnvironment
{
	internal ContentIslandEnvironment(WindowId appWindowId)
	{
		AppWindowId = appWindowId;
	}

	/// <summary>
	/// Gets the identifier of the AppWindow that hosts the ContentIsland.
	/// </summary>
	public WindowId AppWindowId { get; }
}
