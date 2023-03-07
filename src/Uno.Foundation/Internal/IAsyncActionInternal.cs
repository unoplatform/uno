#nullable enable


namespace Windows.Foundation;

internal interface IAsyncActionInternal : IAsyncAction
{
	Task Task { get; }
}
