using System.Threading;

namespace Windows.Foundation;

/// <summary>
/// Provides static helpers for <see cref="AsyncOperation{TResult}"/>.
/// </summary>
internal static class AsyncOperation
{
	private static long _nextId;

	internal static uint CreateId() => (uint)Interlocked.Increment(ref _nextId);

	public static AsyncOperation<TResult> FromTask<TResult>(Func<CancellationToken, Task<TResult>> builder)
		=> new AsyncOperation<TResult>((ct, _) => builder(ct));
}
