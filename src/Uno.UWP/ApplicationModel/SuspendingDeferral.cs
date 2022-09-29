using System;
using System.Diagnostics;
using Windows.Foundation;

namespace Windows.ApplicationModel;

/// <summary>
/// Manages a delayed app suspending operation.
/// </summary>
public sealed partial class SuspendingDeferral : ISuspendingDeferral
{
	private readonly Action? _deferralDone;
	private readonly DeferralCompletedHandler? _handler;

	/// <summary>
	/// This can be removed with other breaking changes
	/// </summary>
	/// <param name="deferralDone"></param>
	[DebuggerHidden]
	public SuspendingDeferral(Action? deferralDone) =>
		_deferralDone = deferralDone;

	internal SuspendingDeferral(DeferralCompletedHandler handler) =>
		_handler = handler;

	/// <summary>
	/// Notifies the operating system that the app has saved its data and is ready to be suspended.
	/// </summary>
	public void Complete()
	{
		_deferralDone?.Invoke();
		_handler?.Invoke();
	}
}
