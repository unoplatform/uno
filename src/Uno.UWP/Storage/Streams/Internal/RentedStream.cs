using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Uno.Storage.Streams.Internal
{
	internal class RentedStream : Stream
	{
		private readonly IRentableStream _baseStream;
		private readonly IDisposable _rent;
		private long _position;
		private RefCountDisposable? _currentScope;

		internal RentedStream(IRentableStream baseStream, IDisposable rent)
		{
			_baseStream = baseStream;
			_rent = rent;
		}

		public override bool CanRead => _baseStream.CanRead;

		public override bool CanSeek => _baseStream.CanSeek;

		public override bool CanWrite => _baseStream.CanWrite;

		public override long Length => _baseStream.Length;

		public override long Position
		{
			get => _position;
			set => _position = value;
		}

		public override void Flush()
		{
			using var scope = BeginScope();
			_baseStream.Flush();
		}

		public override async Task FlushAsync(CancellationToken cancellationToken)
		{
			using var scope = BeginScopeAsync();
			await _baseStream.FlushAsync();
		}

		public override long Seek(long offset, SeekOrigin origin) =>
			origin switch
			{
				SeekOrigin.Begin => Position = offset,
				SeekOrigin.Current => Position += offset,
				SeekOrigin.End => Position = Length + offset,
				_ => throw new ArgumentException("Invalid origin", nameof(origin))
			};

		public override void SetLength(long value)
		{
			using var scope = BeginScope();

			_baseStream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			using var scope = BeginScope();

			if (CanSeek)
			{
				_baseStream.Seek(Position, SeekOrigin.Begin);
			}

			var read = _baseStream.Read(buffer, offset, count);
			Position += read;
			return read;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			using var scope = BeginScopeAsync();

			if (CanSeek)
			{
				_baseStream.Seek(Position, SeekOrigin.Begin);
			}

			var read = await _baseStream.ReadAsync(buffer, offset, count);
			Position += read;
			return read;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			using var scope = BeginScope();

			if (CanSeek)
			{
				_baseStream.Seek(Position, SeekOrigin.Begin);
			}

			_baseStream.Write(buffer, offset, count);
			Position += count;
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			using var scope = BeginScopeAsync();

			if (CanSeek)
			{
				_baseStream.Seek(Position, SeekOrigin.Begin);
			}

			await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
			Position += count;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_rent.Dispose();
		}

		private async Task<IDisposable> BeginScopeAsync()
		{
			if (_currentScope != null)
			{
				// This rent already owns a scope
				return _currentScope.GetDisposable();
			}

			var scope = await _baseStream.AccessScope.BeginAsync();
			_currentScope = new RefCountDisposable(Disposable.Create(() =>
			{
				scope.Dispose();
				_currentScope = null;
			}));
			return _currentScope;
		}

		private IDisposable BeginScope()
		{
			if (_currentScope != null)
			{
				// This rent already owns a scope
				return _currentScope.GetDisposable();
			}

			var scope = _baseStream.AccessScope.Begin();
			_currentScope = new RefCountDisposable(Disposable.Create(() =>
			{
				scope.Dispose();
				_currentScope = null;
			}));
			return _currentScope;
		}
	}
}
