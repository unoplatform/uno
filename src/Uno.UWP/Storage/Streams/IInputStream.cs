
using System;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public  partial interface IInputStream : IDisposable
	{
		IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options);
	}
}
