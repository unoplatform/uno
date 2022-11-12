using System;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.Foundation;

internal static class AsyncOperation
{
	private static long _nextId;
	internal static uint CreateId()
		=> (uint)Interlocked.Increment(ref _nextId);

	public static AsyncOperation<TResult> FromTask<TResult>(Func<CancellationToken, Task<TResult>> builder)
		=> new AsyncOperation<TResult>((ct, _) => builder(ct));
}
