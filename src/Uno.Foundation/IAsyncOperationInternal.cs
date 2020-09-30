#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Windows.Foundation
{
	internal interface IAsyncOperationInternal<TResult> : IAsyncOperation<TResult>
	{
		Task<TResult> Task { get; }
	}
}
