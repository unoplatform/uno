using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Uno.Storage.Streams.Internal;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
	{
		internal static async Task<FileRandomAccessStream> CreateFromSafUriAsync(Android.Net.Uri fileUri, FileAccessMode access) =>
			new FileRandomAccessStream(await SafStream.CreateAsync(fileUri, access));

		private class SafStream : ImplementationBase
		{
			private readonly Android.Net.Uri _fileUri;
			private readonly IRentableStream? _rentableStream;
			private readonly FileAccessMode _accessMode;

			private SafStream(Android.Net.Uri fileUri, Stream stream) : base(stream)
			{
				_fileUri = fileUri;
				_accessMode = FileAccessMode.Read;
			}

			private SafStream(Android.Net.Uri fileUri, Stream stream, IRentableStream rentableStream) : base(stream)
			{
				_fileUri = fileUri;
				_rentableStream = rentableStream;
				_accessMode = FileAccessMode.ReadWrite;
			}

			public static async Task<SafStream> CreateAsync(Android.Net.Uri fileUri, FileAccessMode access)
			{
				if (fileUri is null)
				{
					throw new ArgumentNullException(nameof(fileUri));
				}

				if (access == FileAccessMode.Read)
				{
					var backingStream = Application.Context.ContentResolver!.OpenInputStream(fileUri)!;
					return new SafStream(fileUri, backingStream);
				}
				else
				{
					var backingStream = await SafOutputStream.CreateAsync(fileUri);
					var rentedStream = backingStream.Rent();
					return new SafStream(fileUri, rentedStream, backingStream);
				}
			}

			public override IRandomAccessStream CloneStream()
			{
				if (_accessMode == FileAccessMode.Read)
				{
					var newReadStream = Application.Context.ContentResolver!.OpenInputStream(_fileUri)!;
					return new FileRandomAccessStream(new SafStream(_fileUri, newReadStream));
				}
				else
				{
					var rentedStream = _rentableStream!.Rent();
					return new FileRandomAccessStream(new SafStream(_fileUri, rentedStream, _rentableStream));
				}
			}

			public override IInputStream GetInputStreamAt(ulong position)
			{
				if (!CanRead)
				{
					throw new NotSupportedException("The file has not been opened for read.");
				}

				var newReadStream = Application.Context.ContentResolver!.OpenInputStream(_fileUri)!;

				newReadStream.Seek((long)position, SeekOrigin.Begin);

				return new FileInputStream(newReadStream);
			}

			public override IOutputStream GetOutputStreamAt(ulong position)
			{
				if (!CanWrite)
				{
					throw new NotSupportedException("The file has not been opened for write.");
				}

				var rentedStream = _rentableStream!.Rent();

				rentedStream.Seek((long)position, SeekOrigin.Begin);

				return new FileOutputStream(rentedStream);
			}
		}
	}
}
