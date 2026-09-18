#nullable enable

using System;

namespace Windows.ApplicationModel;

/// <summary>
/// Provides information about an app suspending operation.
/// </summary>
public partial interface ISuspendingOperation
{
	/// <summary>
	/// Gets the time remaining before a delayed app suspending operation continues.
	/// </summary>
	DateTimeOffset Deadline { get; }

	/// <summary>
	/// Requests that the app suspending operation be delayed.
	/// </summary>
	/// <returns>The suspension deferral.</returns>
	SuspendingDeferral GetDeferral();
}
