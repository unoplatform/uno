namespace Microsoft.UI.Content;

/// <summary>
/// Contains event data for the AutomationProviderRequested event.
/// </summary>
public partial class ContentIslandAutomationProviderRequestedEventArgs
{
	internal ContentIslandAutomationProviderRequestedEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets whether the AutomationProviderRequested event was handled.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets or sets an automation provider object for the associated ContentIsland.
	/// </summary>
	public object AutomationProvider { get; set; }
}
