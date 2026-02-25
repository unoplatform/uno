using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class Given_Buffer
	{
		private const byte D = 0b1010_1010; // D as Data ... not the D ANSI code ;)
		private const byte E = 0b0101_0101;

		[TestMethod]
		public void When_Create_Then_LengthIsZero()
		{
			var sut = new Buffer(42);

			Assert.AreEqual(0U, sut.Length);
			Assert.AreEqual(42U, sut.Capacity);
		}

		[TestMethod]
		public void When_SetLengthGreaterThanCapacity()
		{
			var sut = new Buffer(42);

			Assert.Throws<ArgumentException>(() => sut.Length = 43);
		}

		[TestMethod]
		public void When_Write_Then_LengthUpdated()
		{
			var sut = new Buffer(42);

			Assert.AreEqual(0U, sut.Length);

			sut.Write(0, new byte[42], 0, 21);

			Assert.AreEqual(21U, sut.Length);
		}

		[TestMethod]
		public void When_CopyToArray_Then_DataCopied()
		{
			var sut = GetFilledBuffer();

			Assert.AreEqual(D, sut.GetByte(10));

			var copy = new byte[sut.Length];
			sut.CopyTo(0, copy, 0, copy.Length);

			// Then if we change the content of the source, it does not change the copy
			sut.Fill(E);

			Assert.AreEqual(E, sut.GetByte(10));
			Assert.AreEqual(D, copy[10]);
		}

		[TestMethod]
		public void When_CopyToBuffer_Then_DstLengthUpdated()
		{
			var src = GetFilledBuffer();
			var dst = new Buffer(42);

			Assert.AreEqual(42U, src.Length);
			Assert.AreEqual(0U, dst.Length);

			src.CopyTo(0, dst, 10, 10);

			Assert.AreEqual(10U + 10U, dst.Length);
		}

		[TestMethod]
		public void When_CopyToBuffer_Then_DataCopied()
		{
			var sut = GetFilledBuffer();

			Assert.AreEqual(D, sut.GetByte(10));

			var copy = GetFilledBuffer(data: 0);
			sut.CopyTo(0, copy, 0, copy.Capacity);

			// Then if we change the content of the source, it does not change the copy
			sut.Fill(E);

			Assert.AreEqual(E, sut.GetByte(10));
			Assert.AreEqual(D, copy.GetByte(10));
		}

		[TestMethod]
		public void When_GetByte()
		{
			var sut = GetFilledBuffer();

			Assert.AreEqual(D, sut.GetByte(10));
		}

		[TestMethod]
		public void When_GetByteGreaterThanLength()
		{
			var sut = new Buffer(42) { Length = 21 };

			sut.GetByte(41); // We only assert this won't fail
		}

		[TestMethod]
		public void When_GetByteGreaterThanCapacity()
		{
			var sut = new Buffer(42);

			Assert.Throws<ArgumentException>(() => sut.GetByte(42));
		}

		[TestMethod]
		public void When_ToArray()
		{
			var sut = GetFilledBuffer();
			var expected = GetFilledBytes();

			var actual = sut.ToArray();

			Assert.IsTrue(expected.SequenceEqual(actual));
		}

		[TestMethod]
		public void When_ToArray_Then_DataCopied()
		{
			var sut = GetFilledBuffer();

			Assert.AreEqual(D, sut.GetByte(10));

			var copy = sut.ToArray();

			// Then if we change the content of the source, it does not change the copy
			sut.Fill(E);

			Assert.AreEqual(E, sut.GetByte(10));
			Assert.AreEqual(D, copy[10]);
		}

		[TestMethod]
		public void When_ToArray_Then_ResultLengthIsBufferLength()
		{
			// Buffer's length equals
			Assert.HasCount(42, new Buffer(42) { Length = 42 }.ToArray());

			// Buffer's length lower
			Assert.HasCount(21, new Buffer(42) { Length = 21 }.ToArray());

			// Empty buffer
			Assert.IsEmpty(new Buffer(42) { Length = 0 }.ToArray());
		}

		[TestMethod]
		public void When_ToArrayWithCount_Then_ResultLengthIsCount()
		{
			// Buffer's length equals
			Assert.HasCount(42, new Buffer(42) { Length = 42 }.ToArray(0, 42));

			// Buffer's length lower
			Assert.HasCount(42, new Buffer(42) { Length = 21 }.ToArray(0, 42));

			// Empty buffer
			Assert.HasCount(42, new Buffer(42) { Length = 0 }.ToArray(0, 42));
		}

		[TestMethod]
		public void When_ToStream_Then_DataNotCopied()
		{
			var sut = GetFilledBuffer();
			var stream = sut.AsStream();

			Assert.AreEqual(D, stream.ReadByte());

			sut.Fill(E);

			Assert.AreEqual(E, stream.ReadByte());
		}

		[TestMethod]
		public void When_ToStreamAndWriteTo_Then_LengthUpdated()
		{
			var sut = GetFilledBuffer();
			sut.Length = 0;

			var stream = sut.AsStream();

			var data = GetFilledBytes(21);
			stream.Write(data, 0, data.Length);

			Assert.AreEqual(21U, sut.Length);
			Assert.AreEqual(21L, stream.Position);
		}

		private static Buffer GetFilledBuffer(uint capacity = 42, uint length = 42, byte data = D)
		{
			var buffer = new Buffer(capacity) { Length = length };
			buffer.Fill(0, length, data);
			return buffer;
		}

		private static byte[] GetFilledBytes(uint length = 42, byte data = D)
		{
			var buffer = new byte[length];
			for (var i = 0; i < length; i++)
			{
				buffer[i] = data;
			}

			return buffer;
		}
	}

	internal static class BufferExtensions
	{
#if WINAPPSDK
		public static void Write(this IBuffer buffer, uint index, byte[] source, int sourceIndex, int count)
			=> source.CopyTo(sourceIndex, buffer, index, count);
#endif

		public static void Fill(this Buffer buffer, byte data)
			=> Fill(buffer, 0, buffer.Capacity, data);

		public static void Fill(this Buffer buffer, uint index, uint count, byte data)
		{
			var d = new byte[count];
			for (var i = 0; i < d.Length; i++)
			{
				d[i] = data;
			}

			buffer.Write(index, d, 0, d.Length);
		}
	}
}
