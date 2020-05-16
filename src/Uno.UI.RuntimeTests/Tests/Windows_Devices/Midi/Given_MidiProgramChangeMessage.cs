using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiProgramChangeMessage
    {
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiProgramChangeMessage(2, 98);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 194, 98 }, data);
		}

		[TestMethod]
		public void When_Channel_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiProgramChangeMessage(16, 10));
		}

		[TestMethod]
		public void When_Pressure_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiProgramChangeMessage(12, 128));
		}
	}
}
