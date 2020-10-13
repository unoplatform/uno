#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Windows.Foundation
{
	internal interface IAsyncOperationWithProgressInternal<TResult, TProgress> : IAsyncOperationWithProgress<TResult, TProgress>
	{
		Task<TResult> Task { get; }
	}
}
