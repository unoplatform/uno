using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class Given_WindowsRuntimeStreamExtensions
	{
		// Inheriting Stream rather than MemoryStream is intentional.
		// This is intended to test a code path that does this:
		// if (_stream is MemoryStream) ... else ...
		// and we want to test the "else" branch.
		private sealed class MemoryStreamWrapper : Stream
		{
			private readonly MemoryStream _stream;

			public MemoryStreamWrapper(MemoryStream stream)
			{
				_stream = stream;
			}

			public override bool CanRead => _stream.CanRead;

			public override bool CanSeek => _stream.CanSeek;

			public override bool CanWrite => _stream.CanWrite;

			public override long Length => _stream.Length;

			public override long Position
			{
				get => _stream.Position;
				set => _stream.Position = value;
			}

			public override void Flush() => _stream.Flush();
			public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
			public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
			public override void SetLength(long value) => _stream.SetLength(value);
			public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
		}

		[TestMethod]
		public async Task When_StreamAsRandomAccessStream()
		{
			var src = new MemoryStream();
			var sut = src.AsRandomAccessStream();

			await StreamTestHelper.Test(writeTo: src, readOn: sut, directWrapper: true);
			await StreamTestHelper.Test(writeTo: sut, readOn: src, directWrapper: true);
		}

		[TestMethod]
		public void When_MemoryStreamAsRandomAccessStream_CloneStream()
		{
			var memoryStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
			AssertCloneStream(memoryStream);
		}

		[TestMethod]
		public void When_MemoryStreamWrapperAsRandomAccessStream_CloneStream()
		{
			var memoryStream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
			AssertCloneStream(new MemoryStreamWrapper(memoryStream));
		}

		[TestMethod]
		public async Task When_StreamAsInputStream()
		{
			var src = new MemoryStream();
			var sut = src.AsInputStream();

			await StreamTestHelper.Test(writeTo: src, readOn: sut, directWrapper: true);
		}

		[TestMethod]
		public async Task When_StreamAsOutputStream()
		{
			var src = new MemoryStream();
			var sut = src.AsOutputStream();

			await StreamTestHelper.Test(writeTo: sut, readOn: src, directWrapper: true);
		}

		private static void AssertCloneStream(Stream stream)
		{
			stream.Position = 5;
			var randomAccessStream = stream.AsRandomAccessStream();

			Assert.AreEqual(5, stream.Position);
			Assert.HasCount(10, stream);
			Assert.AreEqual((ulong)5, randomAccessStream.Position);
			Assert.AreEqual((ulong)10, randomAccessStream.Size);

			var cloned = randomAccessStream.CloneStream();
			// CloneStream should return a stream with Position equals 0
			Assert.AreEqual((ulong)0, cloned.Position);
			Assert.AreEqual((ulong)10, cloned.Size);
		}
	}
}
