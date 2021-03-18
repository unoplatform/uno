#nullable enable

using System;
using System.IO;
using Foundation;
using Uno.Storage.Internal;

namespace Uno.Storage.Streams.Internal
{
	internal class SecurityScopeStreamWrapper : Stream
	{
		private readonly Stream _innerStream;
		private readonly NSUrl _url;

		private IDisposable? _securityScope;

		internal SecurityScopeStreamWrapper(NSUrl url, Func<Stream> streamBuilder)
		{
			_url = url;
			_securityScope = _url.BeginSecurityScopedAccess();
			_innerStream = streamBuilder();
		}

		public override bool CanRead => _innerStream.CanRead;

		public override bool CanSeek => _innerStream.CanSeek;

		public override bool CanWrite => _innerStream.CanWrite;

		public override long Length => _innerStream.Length;

		public override long Position
		{
			get => _innerStream.Position;
			set => _innerStream.Position = value;
		}

		public override void Flush() => _innerStream.Flush();

		public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

		public override void SetLength(long value) => _innerStream.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

		public override void Close()
		{
			_innerStream.Close();
		}

		protected override void Dispose(bool disposing)
		{
			_securityScope?.Dispose();
			_securityScope = null;
		}
	}
}
