namespace Windows.Networking.Connectivity
{
	/// <summary>
	/// Represents the method that handles network status change notifications.
	/// This method is called when any properties exposed by the NetworkInformation object changes while the app is active.
	/// </summary>
	/// <param name="sender">A Object that raised the event.</param>
	public delegate void NetworkStatusChangedEventHandler(object? @sender);
}
