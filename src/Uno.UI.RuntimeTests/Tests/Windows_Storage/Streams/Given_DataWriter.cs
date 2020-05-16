using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Storage.Streams;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace Uno.UI.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class Given_DataWriter
	{
		[TestMethod]
		public async Task When_WriteThenRead()
		{
			bool saveBool = true;
			byte saveByte = 10;
			int saveInt = 12345;
			double saveDouble = 12345.678;
			DateTimeOffset saveDate = DateTimeOffset.Now;
			TimeSpan saveTimeSpan = TimeSpan.FromMinutes(10);
			Guid saveGuid = Guid.NewGuid();

			bool error = false;
			var writer = new DataWriter();
			try
			{
				writer.WriteBoolean(saveBool);
				writer.WriteByte(saveByte);
				writer.WriteInt32(saveInt);
				writer.WriteDouble(saveDouble);
				writer.WriteDateTime(saveDate);
				writer.WriteTimeSpan(saveTimeSpan);
				writer.WriteGuid(saveGuid);
			}
			catch
			{
				error = true;
			}

			if (error)
			{
				Assert.Fail("FAIL while writing (DataWriter)");
			}

			var buffer = writer.DetachBuffer();

			var reader = DataReader.FromBuffer(buffer);

			// variables below has to be initialized, because if not, we get CS0165 Use of unassigned local variable
			// Codacy shows false errors here, it should be ignored.
			bool loadBool = false;
			byte loadByte = 0;
			int loadInt = 0;
			double loadDouble = 0;
			DateTimeOffset loadDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromMinutes(10));
			TimeSpan loadTimeSpan = TimeSpan.FromSeconds(10);   // and saved value was 10 minutes
			Guid loadGuid = Guid.Empty;

			try
			{
				loadBool = reader.ReadBoolean();
				Assert.AreEqual(saveBool, loadBool, "FAIL: read/write Bool value inconsistence");

				loadByte = reader.ReadByte();
				Assert.AreEqual(saveByte, loadByte, "FAIL: read/write Byte value inconsistence");

				loadInt = reader.ReadInt32();
				Assert.AreEqual(saveInt, loadInt, "FAIL: read/write Int32 value inconsistence");

				loadDouble = reader.ReadDouble();
				Assert.AreEqual(saveDouble, loadDouble, "FAIL: read/write Double value inconsistence");

				loadDate = reader.ReadDateTime();
				Assert.AreEqual(saveDate, loadDate, "FAIL: read/write DateTimeOffset value inconsistence");

				loadTimeSpan = reader.ReadTimeSpan();
				Assert.AreEqual(saveTimeSpan, loadTimeSpan, "FAIL: read/write TimeSpan value inconsistence");

				loadGuid = reader.ReadGuid();
				Assert.AreEqual(saveGuid, loadGuid, "FAIL: read/write Guid value inconsistence");
			}
			catch
			{
				error = true;
			}

			if (error)
			{
				Assert.Fail("FAIL while reading (DataReader)");
			}

			Assert.AreEqual(saveBool, loadBool, "FAIL: read/write Bool value inconsistence");
			Assert.AreEqual(saveByte, loadByte, "FAIL: read/write Byte value inconsistence");
			Assert.AreEqual(saveInt, loadInt, "FAIL: read/write Int32 value inconsistence");
			Assert.AreEqual(saveDouble, loadDouble, "FAIL: read/write Double value inconsistence");
			Assert.AreEqual(saveDate, loadDate, "FAIL: read/write DateTimeOffset value inconsistence");
			Assert.AreEqual(saveTimeSpan, loadTimeSpan, "FAIL: read/write TimeSpan value inconsistence");
			Assert.AreEqual(saveGuid, loadGuid, "FAIL: read/write Guid value inconsistence");
		}


		[TestMethod]
		public void Given_WriteByte_123()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteByte(123);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 123 }, bytes);
		}

		[TestMethod]
		public void Given_WriteByte_0()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteByte(0);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteByte_255()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteByte(255);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteBoolean_true()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBoolean(true);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 1 }, bytes);
		}

		[TestMethod]
		public void Given_WriteBoolean_false()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBoolean(false);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteBytes()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBytes(new byte[] { 0, 2, 135, 65, 255, 7, 9 });
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 2, 135, 65, 255, 7, 9 }, bytes);
		}

		[TestMethod]
		public void Given_WriteBytes_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBytes(new byte[] { 0, 2, 135, 65, 255, 7, 9 });
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 2, 135, 65, 255, 7, 9 }, bytes);
		}

		[TestMethod]
		public void Given_WriteBytes_Empty()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBytes(new byte[0]);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { }, bytes);
		}

		[TestMethod]
		public void Given_WriteBuffer()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBuffer((new byte[] { 0, 2, 135, 65, 255, 7, 9 }).ToBuffer());
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 2, 135, 65, 255, 7, 9 }, bytes);
		}

		[TestMethod]
		public void Given_WriteBuffer_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBuffer((new byte[] { 0, 2, 135, 65, 255, 7, 9 }).ToBuffer());
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 2, 135, 65, 255, 7, 9 }, bytes);
		}

		[TestMethod]
		public void Given_WriteBuffer_Empty()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteBuffer((new byte[0]).ToBuffer());
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { }, bytes);
		}

		[TestMethod]
		public void Given_WriteString_Empty()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteString("");
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { }, bytes);
		}

		[TestMethod]
		public void Given_WriteString_HelloWorld()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteString("Hello, World!");
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 72, 101, 108, 108, 111, 44, 32, 87, 111, 114, 108, 100, 33 }, bytes);
		}

		[TestMethod]
		public void Given_WriteString_HelloWorld_BigEndian_Utf16BE()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf16BE;
			dataWriter.WriteString("Hello, World!");
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 72, 0, 101, 0, 108, 0, 108, 0, 111, 0, 44, 0, 32, 0, 87, 0, 111, 0, 114, 0, 108, 0, 100, 0, 33 }, bytes);
		}

		[TestMethod]
		public void Given_WriteString_HelloWorld_Utf16LE()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf16LE;
			dataWriter.WriteString("Hello, World!");
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 72, 0, 101, 0, 108, 0, 108, 0, 111, 0, 44, 0, 32, 0, 87, 0, 111, 0, 114, 0, 108, 0, 100, 0, 33, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDateTime_MinValue()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDateTime(DateTimeOffset.MinValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 137, 221, 232, 49, 254, 248 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDateTime_MaxValue()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDateTime(DateTimeOffset.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 63, 192, 209, 94, 90, 200, 36 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDateTime_PiDate()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDateTime(new DateTimeOffset(2020, 3, 14, 15, 26, 35, TimeSpan.Zero));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 128, 135, 96, 242, 20, 250, 213, 1 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDateTime_PiDate_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDateTime(new DateTimeOffset(2020, 3, 14, 15, 26, 35, TimeSpan.Zero));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 1, 213, 250, 20, 242, 96, 135, 128 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDateTime_WithOffset()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDateTime(new DateTimeOffset(2020, 3, 14, 15, 26, 35, TimeSpan.FromMinutes(120)));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 128, 183, 215, 46, 4, 250, 213, 1 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDateTime_FileTimeZero()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDateTime(new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDouble_Zero()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDouble(0.0);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDouble_NaN()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDouble(double.NaN);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 248, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDouble_NegativeInfinity()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDouble(double.NegativeInfinity);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 240, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDouble_PositiveInfinity()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDouble(double.PositiveInfinity);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 240, 127 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDouble_Min()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDouble(double.MinValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 255, 255, 255, 255, 255, 239, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteDouble_Max_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteDouble(double.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 127, 239, 255, 255, 255, 255, 255, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteSingle_Zero()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteSingle(0.0f);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteSingle_NaN()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteSingle(float.NaN);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 192, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteSingle_NegativeInfinity()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteSingle(float.NegativeInfinity);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 128, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteSingle_PositiveInfinity()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteSingle(float.PositiveInfinity);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 128, 127 }, bytes);
		}

		[TestMethod]
		public void Given_WriteSingle_Min()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteSingle(float.MinValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 255, 127, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteSingle_Max_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteSingle(float.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 127, 127, 255, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt16_Zero_Utf16LE()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf16LE;
			dataWriter.WriteInt16(0);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt16_14_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt16(14);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 14 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt16_14()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt16(14);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 14, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt16_Negative14()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt16(-14);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 242, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt16_Min_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt16(short.MinValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 128, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt16_Max()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt16(short.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 127 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt32_1304_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt32(1304);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 5, 24 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt32_1304()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt32(1304);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 24, 5, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt32_Negative1304()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt32(-1304);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 232, 250, 255, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt32_Min_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt32(short.MinValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 255, 128, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt32_Max()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt32(short.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 127, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt64_894318()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt64(894318);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 110, 165, 13, 0, 0, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt64_894318_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt64(894318);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 13, 165, 110 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt64_Negative894318()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt64(-894318);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 146, 90, 242, 255, 255, 255, 255, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt64_Min()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt64(short.MinValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 128, 255, 255, 255, 255, 255, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteInt64_Max_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteInt64(short.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 127, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt16_Zero()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt16(0);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt16_1682_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt16(1682);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 6, 146 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt16_Max()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt16(ushort.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt32_Zero()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt32(0);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt32_13()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt32(13);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 13, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt32_Max_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt32(ushort.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 255, 255 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt64_Zero()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt64(0);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt64_198736080_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt64(198736080);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 11, 216, 120, 208 }, bytes);
		}

		[TestMethod]
		public void Given_WriteUInt64_Max()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteUInt64(ushort.MaxValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 255, 255, 0, 0, 0, 0, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteTimeSpan_FromMilliseconds_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteTimeSpan(TimeSpan.FromMilliseconds(9846343));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 22, 236, 224, 181, 112 }, bytes);
		}

		[TestMethod]
		public void Given_WriteTimeSpan_FromDays()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteTimeSpan(TimeSpan.FromDays(19));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 64, 217, 37, 238, 14, 0, 0 }, bytes);
		}

		[TestMethod]
		public void Given_WriteGuid_1()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteGuid(Guid.Parse("f6a9ff70-9cc5-4468-a2cb-51235355856d"));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 112, 255, 169, 246, 197, 156, 104, 68, 162, 203, 81, 35, 83, 85, 133, 109 }, bytes);
		}

		[TestMethod]
		public void Given_WriteGuid_2_BigEndian()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.BigEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteGuid(Guid.Parse("5592504a-161c-4b58-ae92-93f9b49de574"));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 85, 146, 80, 74, 22, 28, 75, 88, 116, 229, 157, 180, 249, 147, 146, 174 }, bytes);
		}

		[TestMethod]
		public void Given_WriteGuid_Zero()
		{
			var dataWriter = new DataWriter();
			dataWriter.ByteOrder = ByteOrder.LittleEndian;
			dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
			dataWriter.WriteGuid(Guid.Parse("00000000-0000-0000-0000-000000000000"));
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, bytes);
		}

	}
}
