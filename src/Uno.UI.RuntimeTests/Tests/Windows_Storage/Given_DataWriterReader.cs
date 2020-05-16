using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_DataWriterReader
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

			if(error)
			{
				Assert.Fail("FAIL while writing (DataWriter)");
			}

			var buffer = writer.DetachBuffer();

			var reader = DataReader.FromBuffer(buffer);

			// variables below has to be initialized, because if not, we get CS0165 Use of unassigned local variable
			// Codacy shows false errors here, it should be ignored.
			bool loadBool =false;
			byte loadByte=0;
			int loadInt=0;
			double loadDouble=0;
			DateTimeOffset loadDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromSeconds(10));
			TimeSpan loadTimeSpan= TimeSpan.FromSeconds(10);	// and saved value was 10 minutes
			Guid loadGuid=Guid.Empty;

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
	}
}
