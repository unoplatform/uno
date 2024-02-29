using System;
using System.IO;
using Foundation;
using Uno.Storage.Streams.Internal;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
	{
		internal static FileRandomAccessStream CreateSecurityScoped(NSUrl url, FileAccess access, FileShare share) =>
			new FileRandomAccessStream(SecurityScoped.Create(url, access, share));

		private class SecurityScoped : ImplementationBase
		{
			private readonly NSUrl _url;
			private readonly FileAccess _access;
			private readonly FileShare _share;

			private SecurityScoped(Stream stream, NSUrl url, FileAccess access, FileShare share) : base(stream)
			{
				_url = url;
				_access = access;
				_share = share;
			}

			public static SecurityScoped Create(NSUrl url, FileAccess access, FileShare share)
			{
				// In order to be able to CloneStream() and Get<Input|Output>Stream(),
				// no matter the provided share, we enforce to 'share' the 'access'.
				var readWriteAccess = (FileShare)(access & FileAccess.ReadWrite);
				share &= ~readWriteAccess;
				share |= readWriteAccess;

				Func<Stream> streamBuilder = () => File.Open(url.Path!, FileMode.OpenOrCreate, access, share);
				var securityScopedStream = new SecurityScopeStreamWrapper(url, streamBuilder);
				return new SecurityScoped(securityScopedStream, url, access, share);
			}

			public override IInputStream GetInputStreamAt(ulong position)
			{
				if (!CanRead)
				{
					throw new NotSupportedException("The file has not been opened for read.");
				}

				Func<Stream> streamBuilder = () => File.Open(_url.Path!, FileMode.OpenOrCreate, FileAccess.Read, _share);
				var securityScopedStream = new SecurityScopeStreamWrapper(_url, streamBuilder);
				securityScopedStream.Seek((long)position, SeekOrigin.Begin);

				return new FileInputStream(securityScopedStream);
			}

			public override IOutputStream GetOutputStreamAt(ulong position)
			{
				if (!CanWrite)
				{
					throw new NotSupportedException("The file has not been opened for write.");
				}

				Func<Stream> streamBuilder = () => File.Open(_url.Path!, FileMode.OpenOrCreate, FileAccess.Write, _share);
				var securityScopedStream = new SecurityScopeStreamWrapper(_url, streamBuilder);
				securityScopedStream.Seek((long)position, SeekOrigin.Begin);

				return new FileOutputStream(securityScopedStream);
			}

			public override IRandomAccessStream CloneStream()
				=> CreateSecurityScoped(_url, _access, _share);
		}
	}
}
