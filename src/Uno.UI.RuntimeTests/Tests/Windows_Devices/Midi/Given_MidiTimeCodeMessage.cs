using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiTimeCodeMessage
	{
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiTimeCodeMessage(1, 2);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 241, 18 }, data);
		}

		[TestMethod]
		public void When_FrameType_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiTimeCodeMessage(8, 10));
		}

		[TestMethod]
		public void When_Values_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiTimeCodeMessage(5, 33));
		}
	}
}
