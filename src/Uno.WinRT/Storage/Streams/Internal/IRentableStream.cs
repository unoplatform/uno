using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Storage.Streams.Internal
{
	internal interface IRentableStream
	{
		StreamAccessScope AccessScope { get; }

		RentedStream Rent();

		bool CanRead { get; }

		bool CanWrite { get; }

		bool CanSeek { get; }

		long Length { get; }

		long Position { get; set; }

		int Read(byte[] buffer, int offset, int count);

		Task<int> ReadAsync(byte[] buffer, int offset, int count);

		void Write(byte[] buffer, int offset, int count);

		Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

		long Seek(long offset, SeekOrigin origin);

		void Flush();

		Task FlushAsync();

		void SetLength(long length);
	}
}
