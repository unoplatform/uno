using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
		[ExpectedException(typeof(ArgumentException))]
		public void When_SetLengthGreaterThanCapacity()
		{
			var sut = new Buffer(42);

			sut.Length = 43;
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
			sut.Span.Fill(E);

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
			sut.Span.Fill(E);

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
			var sut = new Buffer(42) {Length = 21};

			sut.GetByte(41); // We only assert this won't fail
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void When_GetByteGreaterThanCapacity()
		{
			var sut = new Buffer(42);

			sut.GetByte(42);
		}

		[TestMethod]
		public void When_ToArray()
		{
			var sut = GetFilledBuffer();
			var expected = new byte[42];
			Array.Fill(expected, D);

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
			sut.Span.Fill(E);

			Assert.AreEqual(E, sut.GetByte(10));
			Assert.AreEqual(D, copy[10]);
		}

		[TestMethod]
		public void When_ToArray_Then_ResultLengthIsBufferLength()
		{
			// Buffer's length equals
			Assert.AreEqual(42, new Buffer(42) { Length = 42 }.ToArray().Length);

			// Buffer's length lower
			Assert.AreEqual(21, new Buffer(42) { Length = 21 }.ToArray().Length);

			// Empty buffer
			Assert.AreEqual(0, new Buffer(42) { Length = 0 }.ToArray().Length);
		}

		[TestMethod]
		public void When_ToArrayWithCount_Then_ResultLengthIsCount()
		{
			// Buffer's length equals
			Assert.AreEqual(42, new Buffer(42){Length = 42}.ToArray(0, 42).Length);

			// Buffer's length lower
			Assert.AreEqual(42, new Buffer(42) { Length = 21 }.ToArray(0, 42).Length);

			// Empty buffer
			Assert.AreEqual(42, new Buffer(42) { Length = 0 }.ToArray(0, 42).Length);
		}

		[TestMethod]
		public void When_ToStream_Then_DataNotCopied()
		{
			var sut = GetFilledBuffer();
			var stream = sut.AsStream();

			Assert.AreEqual(D, stream.ReadByte());

			sut.Span.Fill(E);

			Assert.AreEqual(E, stream.ReadByte());
		}

		[TestMethod]
		public void When_ToStreamAndWriteTo_Then_LengthUpdated()
		{
			var sut = GetFilledBuffer();
			sut.Length = 0;

			var stream = sut.AsStream();

			var data = new byte[21];
			Array.Fill(data, E);
			stream.Write(data);

			Assert.AreEqual(21U, sut.Length);
			Assert.AreEqual(21L, stream.Position);
		}

		private static Buffer GetFilledBuffer(uint capacity = 42, uint length = 42, byte data = D)
		{
			var buffer = new Buffer(capacity) { Length = length };
			buffer.Span.Slice(0, (int)length).Fill(data);
			return buffer;
		}
	}
}
