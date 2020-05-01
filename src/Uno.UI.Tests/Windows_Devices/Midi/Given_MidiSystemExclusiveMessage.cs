using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;
using Windows.Storage.Streams;

namespace Uno.UI.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiSystemExclusiveMessage
	{
        [TestMethod]
		public void When_RawData_Empty()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiSystemExclusiveMessage(new InMemoryBuffer(Array.Empty<byte>())));
		}

		[TestMethod]
		public void When_RawData()
		{
			var inputBytes = new byte[] { 135, 147, 65, 30, 22 };
			var message = new MidiSystemExclusiveMessage(new InMemoryBuffer(inputBytes));
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(inputBytes, data);
		}
	}
}
