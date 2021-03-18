using Uno.Diagnostics.Eventing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using Uno;

namespace Windows.Foundation
{
	internal static class AsyncOperation
	{
		private static long _nextId;
		internal static uint CreateId()
			=> (uint)Interlocked.Increment(ref _nextId);

		public static AsyncOperation<TResult> FromTask<TResult>(Func<CancellationToken, Task<TResult>> builder)
			=> new AsyncOperation<TResult>((ct, _) => builder(ct));
	}
}
