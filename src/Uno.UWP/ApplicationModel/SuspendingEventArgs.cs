#nullable enable

using System;

namespace Windows.ApplicationModel;

/// <summary>
/// Provides data for an app suspending event.
/// </summary>
public sealed partial class SuspendingEventArgs : ISuspendingEventArgs
{
	internal SuspendingEventArgs(SuspendingOperation operation)
	{
		SuspendingOperation = operation ?? throw new ArgumentNullException(nameof(operation));
	}

	/// <summary>
	/// Gets the app suspending operation.
	/// </summary>
	public SuspendingOperation SuspendingOperation { get; }
}
