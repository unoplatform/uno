using System.IO;

namespace Windows.Storage.Streams
{
	public partial class InMemoryRandomAccessStream: IStreamWrapper
	{
		private MemoryStream _stream;
		public InMemoryRandomAccessStream() =>
			_stream = new();

		private InMemoryRandomAccessStream(MemoryStream stream) =>
			_stream = stream;

		public ulong Size
		{
			get => (ulong)_stream.Length;
			set => _stream.SetLength((long)value);
		}

		public bool CanRead => _stream.CanRead;

		public bool CanWrite => _stream.CanWrite;

		public ulong Position => (ulong)_stream.Position;

		public void Seek(ulong position) => _stream.Position = (long)position;

		public IRandomAccessStream CloneStream()
		{
			var destination = new MemoryStream();
			_stream.Position = 0;
			_stream.CopyTo(destination);
			return new InMemoryRandomAccessStream(destination);
		}

		public void Dispose() => _stream.Dispose();

		Stream IStreamWrapper.FindStream() => _stream;
	}
}
