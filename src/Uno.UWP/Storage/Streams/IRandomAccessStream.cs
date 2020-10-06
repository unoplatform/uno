using System;

namespace Windows.Storage.Streams
{
	public partial interface IRandomAccessStream : IDisposable, IInputStream, IOutputStream
	{
		bool CanRead { get; }

		bool CanWrite { get; }

		ulong Position { get; }

		ulong Size { get; }

		IInputStream GetInputStreamAt( ulong position);

		IOutputStream GetOutputStreamAt( ulong position);

		void Seek( ulong position);

		IRandomAccessStream CloneStream();
	}
}
