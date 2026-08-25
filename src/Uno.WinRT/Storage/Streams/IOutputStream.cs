using System;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial interface IOutputStream : IDisposable
	{
		IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer);

		IAsyncOperation<bool> FlushAsync();
	}
}
