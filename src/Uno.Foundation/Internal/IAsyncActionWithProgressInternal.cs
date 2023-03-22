#nullable enable


namespace Windows.Foundation;

internal interface IAsyncActionWithProgressInternal<TProgress> : IAsyncActionWithProgress<TProgress>
{
	Task Task { get; }
}
