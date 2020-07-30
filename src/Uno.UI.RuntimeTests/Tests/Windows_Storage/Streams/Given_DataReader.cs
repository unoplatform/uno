using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class When_DataReader
	{
		[TestMethod]
		public void When_FromBuffer_Argument_Null()
		{
			Assert.ThrowsException<ArgumentNullException>(() => DataReader.FromBuffer(null));
		}

		[TestMethod]
		public void When_ReadByte()
		{
			var bytes = new byte[]
			{
				123,
				192,
				31
			};
			var buffer = bytes.AsBuffer();
			var reader = DataReader.FromBuffer(buffer);
			Assert.AreEqual(bytes[0], reader.ReadByte());
			Assert.AreEqual(bytes[1], reader.ReadByte());
			Assert.AreEqual(bytes[2], reader.ReadByte());
		}

		[TestMethod]
		public void When_ReadByte_123()
		{
			var inputBytes = new byte[] { 123 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadByte();
			var expected = 123;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadByte_0()
		{
			var inputBytes = new byte[] { 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadByte();
			var expected = 0;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadByte_255()
		{
			var inputBytes = new byte[] { 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadByte();
			var expected = 255;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadBoolean_true()
		{
			var inputBytes = new byte[] { 1 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadBoolean();
			var expected = true;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadBoolean_false()
		{
			var inputBytes = new byte[] { 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadBoolean();
			var expected = false;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadBytes()
		{
			var inputBytes = new byte[] { 0, 2, 135, 65, 255, 7, 9 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var expected = new byte[] { 0, 2, 135, 65, 255, 7, 9 };
			var bytes = new byte[expected.Length];
			dataReader.ReadBytes(bytes);
			CollectionAssert.AreEqual(expected, bytes);
		}

		[TestMethod]
		public void When_ReadBytes_BigEndian()
		{
			var inputBytes = new byte[] { 0, 2, 135, 65, 255, 7, 9 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var expected = new byte[] { 0, 2, 135, 65, 255, 7, 9 };
			var bytes = new byte[expected.Length];
			dataReader.ReadBytes(bytes);
			CollectionAssert.AreEqual(expected, bytes);
		}

		[TestMethod]
		public void When_ReadBytes_Empty()
		{
			var inputBytes = new byte[] { };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var bytes = new byte[0];
			dataReader.ReadBytes(bytes);
			var expected = new byte[0];
			CollectionAssert.AreEqual(expected, bytes);
		}

		[TestMethod]
		public void When_ReadBuffer()
		{
			var inputBytes = new byte[] { 0, 2, 135, 65, 255, 7, 9 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadBuffer((uint)inputBytes.Length);
			var expected = (new byte[] { 0, 2, 135, 65, 255, 7, 9 }).ToBuffer();
			CollectionAssert.AreEqual(expected.ToArray(), result.ToArray());
		}

		[TestMethod]
		public void When_ReadBuffer_BigEndian()
		{
			var inputBytes = new byte[] { 0, 2, 135, 65, 255, 7, 9 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadBuffer((uint)inputBytes.Length);
			var expected = (new byte[] { 0, 2, 135, 65, 255, 7, 9 }).ToBuffer();
			CollectionAssert.AreEqual(expected.ToArray(), result.ToArray());
		}

		[TestMethod]
		public void When_ReadString_Empty()
		{
			var inputBytes = new byte[] { };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var expected = "";
			var result = dataReader.ReadString(0);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadString_HelloWorld()
		{
			var inputBytes = new byte[] { 72, 101, 108, 108, 111, 44, 32, 87, 111, 114, 108, 100, 33 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var expected = "Hello, World!";
			var result = dataReader.ReadString((uint)expected.Length);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadString_HelloWorld_BigEndian_Utf16BE()
		{
			var inputBytes = new byte[] { 0, 72, 0, 101, 0, 108, 0, 108, 0, 111, 0, 44, 0, 32, 0, 87, 0, 111, 0, 114, 0, 108, 0, 100, 0, 33 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf16BE;
			var expected = "Hello, World!";
			var result = dataReader.ReadString((uint)expected.Length);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadString_HelloWorld_Utf16LE()
		{
			var inputBytes = new byte[] { 72, 0, 101, 0, 108, 0, 108, 0, 111, 0, 44, 0, 32, 0, 87, 0, 111, 0, 114, 0, 108, 0, 100, 0, 33, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf16LE;
			var expected = "Hello, World!";
			var result = dataReader.ReadString((uint)expected.Length);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		[Ignore("This test fails if the current timezone offset is negative")]
		public void When_ReadDateTime_MinValue()
		{
			var inputBytes = new byte[] { 0, 0, 137, 221, 232, 49, 254, 248 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDateTime();
			var expected = DateTimeOffset.MinValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void Given_ReadDateTime_PiDate()
		{
			var inputBytes = new byte[] { 128, 135, 96, 242, 20, 250, 213, 1 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDateTime();
			var expected = new DateTimeOffset(2020, 3, 14, 15, 26, 35, TimeSpan.Zero);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void Given_ReadDateTime_PiDate_BigEndian()
		{
			var inputBytes = new byte[] { 1, 213, 250, 20, 242, 96, 135, 128 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDateTime();
			var expected = new DateTimeOffset(2020, 3, 14, 15, 26, 35, TimeSpan.Zero);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void Given_ReadDateTime_OldDate()
		{
			var inputBytes = new byte[] { 128, 191, 199, 115, 143, 82, 95, 253 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDateTime();
			var expected = new DateTimeOffset(1001, 1, 1, 8, 14, 35, TimeSpan.FromMinutes(60));
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void Given_ReadDateTime_WithNegativeOffset()
		{
			var inputBytes = new byte[] { 0, 112, 167, 193, 164, 135, 198, 1 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDateTime();
			var expected = new DateTimeOffset(2006, 6, 4, 3, 1, 52, -TimeSpan.FromMinutes(240));
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void Given_ReadDateTime_WithOffset()
		{
			var inputBytes = new byte[] { 128, 183, 215, 46, 4, 250, 213, 1 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDateTime();
			var expected = new DateTimeOffset(2020, 3, 14, 15, 26, 35, TimeSpan.FromMinutes(120));
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void Given_ReadDateTime_FileTimeZero()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDateTime();
			var expected = new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadDouble_Zero()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDouble();
			var expected = 0.0;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadDouble_NaN()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 248, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDouble();
			var expected = double.NaN;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadDouble_NegativeInfinity()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 240, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDouble();
			var expected = double.NegativeInfinity;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadDouble_PositiveInfinity()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 240, 127 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDouble();
			var expected = double.PositiveInfinity;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadDouble_Min()
		{
			var inputBytes = new byte[] { 255, 255, 255, 255, 255, 255, 239, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDouble();
			var expected = double.MinValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadDouble_Max_BigEndian()
		{
			var inputBytes = new byte[] { 127, 239, 255, 255, 255, 255, 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadDouble();
			var expected = double.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadSingle_Zero()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadSingle();
			var expected = 0.0f;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadSingle_NaN()
		{
			var inputBytes = new byte[] { 0, 0, 192, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadSingle();
			var expected = float.NaN;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadSingle_NegativeInfinity()
		{
			var inputBytes = new byte[] { 0, 0, 128, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadSingle();
			var expected = float.NegativeInfinity;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadSingle_PositiveInfinity()
		{
			var inputBytes = new byte[] { 0, 0, 128, 127 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadSingle();
			var expected = float.PositiveInfinity;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadSingle_Min()
		{
			var inputBytes = new byte[] { 255, 255, 127, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadSingle();
			var expected = float.MinValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadSingle_Max_BigEndian()
		{
			var inputBytes = new byte[] { 127, 127, 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadSingle();
			var expected = float.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt16_Zero_Utf16LE()
		{
			var inputBytes = new byte[] { 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf16LE;
			var result = dataReader.ReadInt16();
			var expected = 0;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt16_14_BigEndian()
		{
			var inputBytes = new byte[] { 0, 14 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt16();
			var expected = 14;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt16_14()
		{
			var inputBytes = new byte[] { 14, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt16();
			var expected = 14;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt16_Negative14()
		{
			var inputBytes = new byte[] { 242, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt16();
			var expected = -14;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt16_Min_BigEndian()
		{
			var inputBytes = new byte[] { 128, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt16();
			var expected = short.MinValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt16_Max()
		{
			var inputBytes = new byte[] { 255, 127 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt16();
			var expected = short.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt32_1304_BigEndian()
		{
			var inputBytes = new byte[] { 0, 0, 5, 24 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt32();
			var expected = 1304;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt32_1304()
		{
			var inputBytes = new byte[] { 24, 5, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt32();
			var expected = 1304;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt32_Negative1304()
		{
			var inputBytes = new byte[] { 232, 250, 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt32();
			var expected = -1304;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt32_Min_BigEndian()
		{
			var inputBytes = new byte[] { 128, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt32();
			var expected = int.MinValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt32_Max()
		{
			var inputBytes = new byte[] { 255, 255, 255, 127 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt32();
			var expected = int.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt64_894318()
		{
			var inputBytes = new byte[] { 110, 165, 13, 0, 0, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt64();
			var expected = 894318;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt64_894318_BigEndian()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 13, 165, 110 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt64();
			var expected = 894318;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt64_Negative894318()
		{
			var inputBytes = new byte[] { 146, 90, 242, 255, 255, 255, 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt64();
			var expected = -894318;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt64_Min()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 128 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt64();
			var expected = long.MinValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadInt64_Max_BigEndian()
		{
			var inputBytes = new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadInt64();
			var expected = long.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt16_Zero()
		{
			var inputBytes = new byte[] { 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt16();
			var expected = 0;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt16_1682_BigEndian()
		{
			var inputBytes = new byte[] { 6, 146 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt16();
			var expected = 1682;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt16_Max()
		{
			var inputBytes = new byte[] { 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt16();
			var expected = ushort.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt32_Zero()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt32();
			var expected = 0u;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt32_13()
		{
			var inputBytes = new byte[] { 13, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt32();
			var expected = 13u;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt32_Max_BigEndian()
		{
			var inputBytes = new byte[] { 255, 255, 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt32();
			var expected = uint.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt64_Zero()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt64();
			var expected = 0ul;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt64_198736080_BigEndian()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 11, 216, 120, 208 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt64();
			var expected = 198736080ul;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadUInt64_Max()
		{
			var inputBytes = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadUInt64();
			var expected = ulong.MaxValue;
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadTimeSpan_FromMilliseconds_BigEndian()
		{
			var inputBytes = new byte[] { 0, 0, 0, 22, 236, 224, 181, 112 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadTimeSpan();
			var expected = TimeSpan.FromMilliseconds(9846343);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadTimeSpan_FromDays()
		{
			var inputBytes = new byte[] { 0, 64, 217, 37, 238, 14, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadTimeSpan();
			var expected = TimeSpan.FromDays(19);
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadGuid_1()
		{
			var inputBytes = new byte[] { 34, 46, 142, 202, 240, 60, 78, 73, 144, 134, 128, 240, 12, 41, 55, 26 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadGuid();
			var expected = Guid.Parse("ca8e2e22-3cf0-494e-9086-80f00c29371a");
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadGuid_2_BigEndian()
		{
			var inputBytes = new byte[] { 220, 113, 4, 117, 57, 105, 75, 156, 43, 172, 105, 154, 64, 149, 138, 141 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.BigEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadGuid();
			var expected = Guid.Parse("dc710475-3969-4b9c-8d8a-95409a69ac2b");
			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void When_ReadGuid_Zero()
		{
			var inputBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			var buffer = inputBytes.AsBuffer();
			var dataReader = DataReader.FromBuffer(buffer);
			dataReader.ByteOrder = ByteOrder.LittleEndian;
			dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
			var result = dataReader.ReadGuid();
			var expected = Guid.Parse("00000000-0000-0000-0000-000000000000");
			Assert.AreEqual(expected, result);
		}
	}
}
