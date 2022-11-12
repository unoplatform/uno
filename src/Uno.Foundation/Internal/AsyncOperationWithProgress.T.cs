#nullable enable

using Uno;

namespace Windows.Foundation;

internal class AsyncOperationWithProgress<TResult, TProgress> : AsyncOperation<TResult>, IAsyncOperationWithProgress<TResult, TProgress>, IAsyncOperationWithProgressInternal<TResult, TProgress>
{
	private AsyncOperationWithProgressCompletedHandler<TResult, TProgress>? _completed;

	/// <inheritdoc />
	public AsyncOperationWithProgress(FuncAsync<AsyncOperationWithProgress<TResult, TProgress>, TResult> taskBuilder)
		: base(Wrap(taskBuilder))
	{
	}

	/// <inheritdoc />
	public AsyncOperationProgressHandler<TResult, TProgress>? Progress { get; set; }

	/// <inheritdoc />
	public new AsyncOperationWithProgressCompletedHandler<TResult, TProgress>? Completed
	{
		get => _completed;
		set
		{
			_completed = value;
			base.Completed = Wrap(value);
		}
	}

	public void NotifyProgress(TProgress progressInfo)
		=> Progress?.Invoke(this, progressInfo);

	private static FuncAsync<AsyncOperation<TResult>, TResult> Wrap(FuncAsync<AsyncOperationWithProgress<TResult, TProgress>, TResult> taskBuilder)
		=> (ct, that) => taskBuilder(ct, (AsyncOperationWithProgress<TResult, TProgress>)that);

	private static AsyncOperationCompletedHandler<TResult>? Wrap(AsyncOperationWithProgressCompletedHandler<TResult, TProgress>? handler)
		=> handler is null
			? default(AsyncOperationCompletedHandler<TResult>)
			: (that, status) => handler!((IAsyncOperationWithProgress<TResult, TProgress>)that, status);
}
