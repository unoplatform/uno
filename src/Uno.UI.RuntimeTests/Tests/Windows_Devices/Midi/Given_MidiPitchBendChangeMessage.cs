using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiPitchBendChangeMessage
    {
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiPitchBendChangeMessage(12, 16323);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 236, 67, 127 }, data);
		}

		[TestMethod]
		public void When_Channel_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiPitchBendChangeMessage(16, 10));
		}

		[TestMethod]
		public void When_Bend_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiPitchBendChangeMessage(12, 16384));
		}
	}
}
