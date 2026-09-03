#nullable enable

using System;
using Uno.Helpers;

namespace Windows.ApplicationModel;

/// <summary>
/// Provides info about an app suspending operation.
/// </summary>
public sealed partial class SuspendingOperation : ISuspendingOperation
{
	internal SuspendingOperation(DateTimeOffset offset, Action? onComplete)
	{
		Deadline = offset;
		DeferralManager = new DeferralManager<SuspendingDeferral>(h => new SuspendingDeferral(h));
		DeferralManager.Completed += (s, e) => onComplete?.Invoke();
	}

	/// <summary>
	/// Gets the time when the delayed app suspending operation continues.
	/// </summary>
	public DateTimeOffset Deadline { get; }

	internal DeferralManager<SuspendingDeferral> DeferralManager { get; }

	/// <summary>
	/// Requests that the app suspending operation be delayed.
	/// </summary>
	/// <returns>The suspension deferral.</returns>
	public SuspendingDeferral GetDeferral() => DeferralManager.GetDeferral();
}
