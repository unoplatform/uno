using System;
using Windows.Foundation;

namespace Windows.Storage;

/// <summary>
/// Manages a delayed set version operation.
/// </summary>
public partial class SetVersionDeferral
{
	private readonly DeferralCompletedHandler _handler;

	internal SetVersionDeferral(DeferralCompletedHandler handler)
	{
		_handler = handler ?? throw new ArgumentNullException(nameof(handler));
	}

	/// <summary>
	/// Notifies the system that the app has set the version of the application data in its app data store.
	/// </summary>
	public void Complete() => _handler?.Invoke();
}
