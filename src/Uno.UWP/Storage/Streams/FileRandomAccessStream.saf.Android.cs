using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
	{
		internal static async Task<FileRandomAccessStream> CreateFromSafUriAsync(Android.Net.Uri fileUri, FileAccessMode access) =>
			new FileRandomAccessStream(await SafStream.CreateAsync(fileUri, access));

		private class SafStream : ImplementationBase
		{
			private SafStream(Stream stream) : base(stream)
			{
			}

			public static async Task<SafStream> CreateAsync(Android.Net.Uri fileUri, FileAccessMode access)
			{
				if (fileUri is null)
				{
					throw new ArgumentNullException(nameof(fileUri));
				}

				Stream backingStream;
				if (access == FileAccessMode.Read)
				{
					backingStream = Application.Context.ContentResolver.OpenInputStream(fileUri);
				}
				else
				{
					backingStream = Application.Context.ContentResolver.OpenOutputStream(fileUri);
				}
				return new SafStream(backingStream);
			}

			public override IRandomAccessStream CloneStream()
			{
				return new FileRandomAccessStream(new SafStream(_stream));
			}

			public override IInputStream GetInputStreamAt(ulong position)
			{
				return new FileInputStream(_stream);
			}

			public override IOutputStream GetOutputStreamAt(ulong position)
			{
				return new FileOutputStream(_stream);
			}
		}
	}
}
