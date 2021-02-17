using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
    public partial class FileRandomAccessStream
    {
		internal static FileRandomAccessStream CreateNativeWasm(StorageFile file, FileAccess access, FileShare share)
		{
			var localImplementation = new NativeFileSystem(file, access, share);
			return new FileRandomAccessStream(localImplementation);
		}

		private class NativeFileSystem : ImplementationBase
		{
			public NativeFileSystem(StorageFile file, FileAccess access, FileShare share)
			{

			}

			public override ulong Size { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public override bool CanRead => throw new NotImplementedException();

			public override bool CanWrite => throw new NotImplementedException();

			public override ulong Position => throw new NotImplementedException();

			public override IRandomAccessStream CloneStream() => throw new NotImplementedException();
			public override void Dispose() => throw new NotImplementedException();
			public override IAsyncOperation<bool> FlushAsync() => throw new NotImplementedException();
			public override IInputStream GetInputStreamAt(ulong position) => throw new NotImplementedException();
			public override IOutputStream GetOutputStreamAt(ulong position) => throw new NotImplementedException();
			public override IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) => throw new NotImplementedException();
			public override void Seek(ulong position) => throw new NotImplementedException();
			public override IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => throw new NotImplementedException();
		}
	}
}
