#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Uno.Storage.Streams.Internal;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
	{
		internal static async Task<FileRandomAccessStream> CreateNativeAsync(Guid fileId, FileAccessMode access) =>
			new FileRandomAccessStream(await Native.CreateAsync(fileId, access));

		private class Native : ImplementationBase
		{
			private readonly Guid _fileId;
			private readonly FileAccessMode _access;
			private readonly IRentableStream _rentableStream;

			private Native(Guid fileId, Stream stream, IRentableStream rentableStream, FileAccessMode access) : base(stream)
			{
				_fileId = fileId;
				_rentableStream = rentableStream;
				_access = access;
			}

			public static async Task<Native> CreateAsync(Guid fileId, FileAccessMode access)
			{
				IRentableStream rentableStream;
				if (access == FileAccessMode.Read)
				{
					rentableStream = await NativeReadStream.CreateAsync(fileId);
				}
				else
				{
					rentableStream = await NativeWriteStream.CreateAsync(fileId);
				}
				var rentedStream = rentableStream.Rent();
				return new Native(fileId, rentedStream, rentableStream, access);
			}

			public override IRandomAccessStream CloneStream()
			{
				var rentedStream = _rentableStream.Rent();
				return new FileRandomAccessStream(new Native(_fileId, rentedStream, _rentableStream, _access));
			}

			public override IInputStream GetInputStreamAt(ulong position)
			{
				if (!CanRead)
				{
					throw new NotSupportedException("The file has not been opened for read.");
				}

				var rentedStream = _rentableStream.Rent();
				rentedStream.Seek((long)position, SeekOrigin.Begin);
				return new FileInputStream(rentedStream);
			}

			public override IOutputStream GetOutputStreamAt(ulong position)
			{
				if (!CanWrite)
				{
					throw new NotSupportedException("The file has not been opened for write.");
				}

				var rentedStream = _rentableStream.Rent();
				rentedStream.Seek((long)position, SeekOrigin.Begin);
				return new FileOutputStream(rentedStream);
			}
		}
	}
}
