using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiControlChangeMessage
    {
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiControlChangeMessage(9, 114, 64);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 185, 114, 64 }, data);
		}

		[TestMethod]
		public void When_Channel_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiControlChangeMessage(16, 10, 10));
		}

		[TestMethod]
		public void When_Controller_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiControlChangeMessage(12, 128, 10));
		}

		[TestMethod]
		public void When_ControlChange_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiControlChangeMessage(12, 10, 128));
		}
	}
}
