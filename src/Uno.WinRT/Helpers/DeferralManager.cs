#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.Helpers;

/// <summary>
/// Handles completion of deferrals. The deferred action is completed when all deferral objects that were taken have called Complete().
/// </summary>
/// <typeparam name="T"></typeparam>
internal class DeferralManager<T>
{
	private readonly Func<DeferralCompletedHandler, T> _deferralFactory;
	private readonly bool _requiresUIThread;

	// Continuations run asynchronously so that awaiting the completion does not resume on — and block —
	// the thread that completed the last deferral.
	private readonly TaskCompletionSource<object?> _allDeferralsCompletedCompletionSource =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	/// <summary>
	/// Start the count at 1, this ensures the deferral won't be completed until all subscribers to the corresponding event have had a
	/// chance to take out a deferral object.
	/// </summary>
	private int _deferralsCount = 1;

	public DeferralManager(Func<DeferralCompletedHandler, T> deferralFactory, bool requiresUIThread = true)
	{
		_deferralFactory = deferralFactory ?? throw new ArgumentNullException(nameof(deferralFactory));
		_requiresUIThread = requiresUIThread;
	}

	internal event EventHandler? Completed;

	internal bool CompletedSynchronously { get; set; }

	public T GetDeferral()
	{
		Interlocked.Increment(ref _deferralsCount);
		var isCompleted = 0;
		return _deferralFactory(OnDeferralCompleted);

		void OnDeferralCompleted()
		{
			if (_requiresUIThread)
			{
				CoreDispatcher.CheckThreadAccess();
			}

			if (Interlocked.Exchange(ref isCompleted, 1) == 1)
			{
				throw new InvalidOperationException("Deferral already completed.");
			}

			DeferralCompleted(false);
		}
	}

	/// <summary>
	/// This marks the deferral as ready for completion.
	/// Must be called after the related event finished invoking.
	/// In case the operation is not deferred, it will also synchronously raise
	/// the Completed event.
	/// </summary>
	/// <returns>A value indicating whether the deferral completed synchronously.</returns>
	internal bool EventRaiseCompleted()
	{
		DeferralCompleted(true);
		return CompletedSynchronously;
	}

	/// <summary>
	/// Waits for every deferral that was taken to complete.
	/// </summary>
	/// <param name="cancellationToken">
	/// Cancels the wait. A deferral that is never completed would otherwise wait forever.
	/// </param>
	internal Task WhenAllCompletedAsync(CancellationToken cancellationToken = default) =>
		cancellationToken.CanBeCanceled
			? _allDeferralsCompletedCompletionSource.Task.WaitAsync(cancellationToken)
			: _allDeferralsCompletedCompletionSource.Task;

	private void DeferralCompleted(bool eventRaiseCompletion)
	{
		if (Interlocked.Decrement(ref _deferralsCount) <= 0)
		{
			if (eventRaiseCompletion)
			{
				CompletedSynchronously = true;
			}

			Completed?.Invoke(this, EventArgs.Empty);
			_allDeferralsCompletedCompletionSource.TrySetResult(null);
		}
	}
}
