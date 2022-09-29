namespace Windows.ApplicationModel;

/// <summary>
/// Manages a delayed app suspending operation.
/// </summary>
public partial interface ISuspendingDeferral
{
	/// <summary>
	/// Notifies the system that the app has saved its data and is ready to be suspended.
	/// </summary>
	void Complete();
}
