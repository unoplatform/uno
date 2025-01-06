#nullable enable

using System;

namespace Uno.Helpers;

/// <summary>
/// Start stop wrapper for EventHandler`1 events.
/// </summary>
/// <typeparam name="TEventArgs">Event args type.</typeparam>
internal class StartStopEventWrapper<TEventArgs> : StartStopDelegateWrapper<EventHandler<TEventArgs>>
{
	/// <summary>
	/// Creates a new instance of start-stop event wrapper.
	/// </summary>
	/// <param name="onFirst">Action to run when first subscriber is added.
	/// This will run within a synchronization lock so it should not involve blocking operations.</param>
	/// <param name="onLast">Action to run when last subscriber is removed.
	/// This will run within a synchronization lock so it should not involve blocking operations.</param>
	/// <param name="sharedLock">Optional shared object to lock on (when multiple events
	/// rely on the same native platform operation.</param>
	public StartStopEventWrapper(Action onFirst, Action onLast, object? sharedLock = null) :
		base(onFirst, onLast, sharedLock)
	{
	}

	/// <summary>
	/// Invokes the event.
	/// </summary>
	/// <param name="sender">Sender.</param>
	/// <param name="args">Args.</param>
	public void Invoke(object sender, TEventArgs args) => Event?.Invoke(sender, args);
}
