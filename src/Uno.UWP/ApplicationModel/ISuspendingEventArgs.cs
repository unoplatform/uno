#nullable enable

namespace Windows.ApplicationModel;

/// <summary>
/// Provides data for an app suspending event.
/// </summary>
public partial interface ISuspendingEventArgs
{
	/// <summary>
	/// Gets the app suspending operation.
	/// </summary>
	SuspendingOperation SuspendingOperation { get; }
}
