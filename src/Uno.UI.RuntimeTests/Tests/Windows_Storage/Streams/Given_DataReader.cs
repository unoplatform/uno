using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class Given_DataReader
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
			using var reader = CreateReaderFromBytes(bytes);
			Assert.AreEqual(bytes[0], reader.ReadByte());
			Assert.AreEqual(bytes[1], reader.ReadByte());
			Assert.AreEqual(bytes[2], reader.ReadByte());
		}

		private DataReader CreateReaderFromBytes(byte[] bytes)
		{			
			return DataReader.FromBuffer(bytes.ToBuffer());
		}
	}
}
