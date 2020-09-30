using System;

namespace Windows.Storage.Streams
{
	public  partial interface IRandomAccessStreamWithContentType : IRandomAccessStream, IDisposable, IInputStream, IOutputStream, IContentTypeProvider
	{
	}
}
