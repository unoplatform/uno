using System;
using System.IO;
using System.Threading.Tasks;
using Uno.Storage.Streams;
using Uno.Storage.Streams.Internal;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
	{
		internal static async Task<FileRandomAccessStream> CreateNativeAsync(Guid fileId, FileAccess access) =>
			new FileRandomAccessStream(await Native.CreateAsync(fileId, access));

		private class Native : ImplementationBase
		{
			private readonly Guid _fileId;
			private readonly FileAccess _access;
			private readonly INativeStreamAdapter _backingStream;

			private Native(Stream stream, Guid fileId, FileAccess access) : base(stream)
			{
				_fileId = fileId;
				_access = access;
				_backingStream = (INativeStreamAdapter)stream;
				_backingStream.Rent();
			}

			public static async Task<Native> CreateAsync(Guid fileId, FileAccess access)
			{
				Stream backingStream;
				if (access == FileAccess.Read)
				{
					backingStream = await NativeReadStreamAdapter.CreateAsync(fileId);
				}
				else
				{
					backingStream = await NativeWriteStreamAdapter.CreateAsync(fileId);
				}
				return new Native(backingStream, fileId, access);
			}

			public override IRandomAccessStream CloneStream()
			{
				return new FileRandomAccessStream(new Native(_stream, _fileId, _access));
			}

			public override IInputStream GetInputStreamAt(ulong position)
			{
				_backingStream.Rent();
				return new FileInputStream(_stream);
			}

			public override IOutputStream GetOutputStreamAt(ulong position)
			{
				_backingStream.Rent();
				return new FileOutputStream(_stream);
			}
		}
	}
}
