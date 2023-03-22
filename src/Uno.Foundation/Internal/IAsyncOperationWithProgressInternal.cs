#nullable enable


namespace Windows.Foundation;

internal interface IAsyncOperationWithProgressInternal<TResult, TProgress> : IAsyncOperationWithProgress<TResult, TProgress>
{
	Task<TResult> Task { get; }
}
