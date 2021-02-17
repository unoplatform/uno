using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
	{
		private abstract class ImplementationBase
		{
			protected ImplementationBase()
			{
			}

			public abstract ulong Size { get; set; }
			
			public abstract bool CanRead { get; }

			public abstract bool CanWrite { get; }

			public abstract ulong Position { get; }

			public abstract IInputStream GetInputStreamAt(ulong position);

			public abstract IOutputStream GetOutputStreamAt(ulong position);

			public abstract void Seek(ulong position);

			public abstract IRandomAccessStream CloneStream();

			public abstract void Dispose();

			public abstract IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options);

			public abstract IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer);

			public abstract IAsyncOperation<bool> FlushAsync();
		}
	}
}
