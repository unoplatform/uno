using System;
using System.IO;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
    {
		internal static FileRandomAccessStream CreateLocal(string path, FileAccess access, FileShare share) =>
			new FileRandomAccessStream(Local.Create(path, access, share));

		private class Local : ImplementationBase
		{
			private readonly string _path;
			private readonly FileAccess _access;
			private readonly FileShare _share;

			private Local(Stream stream, string path, FileAccess access, FileShare share) : base(stream)
			{
				if (stream is null)
				{
					throw new ArgumentNullException(nameof(stream));
				}

				_path = path ?? throw new ArgumentNullException(nameof(path));
				_access = access;
				_share = share;
			}

			public static Local Create(string path, FileAccess access, FileShare share)
			{
				// In order to be able to CloneStream() and Get<Input|Output>Stream(),
				// no matter the provided share, we enforce to 'share' the 'access'.
				var readWriteAccess = (FileShare)(access & FileAccess.ReadWrite);
				share &= ~readWriteAccess;
				share |= readWriteAccess;

				return new Local(File.Open(path, FileMode.OpenOrCreate, access, share), path, access, share);
			}
		
			public override IInputStream GetInputStreamAt(ulong position)
			{
				if (!CanRead)
				{
					throw new NotSupportedException("The file has not been opened for read.");
				}

				var stream = File.Open(_path, FileMode.OpenOrCreate, FileAccess.Read, _share);
				stream.Seek((long)position, SeekOrigin.Begin);

				return new FileInputStream(stream);
			}

			public override IOutputStream GetOutputStreamAt(ulong position)
			{
				if (!CanWrite)
				{
					throw new NotSupportedException("The file has not been opened for write.");
				}

				var stream = File.Open(_path, FileMode.OpenOrCreate, FileAccess.Write, _share);
				stream.Seek((long)position, SeekOrigin.Begin);

				return new FileOutputStream(stream);
			}

			public override IRandomAccessStream CloneStream()
				=> CreateLocal(_path, _access, _share);
		}
    }
}
