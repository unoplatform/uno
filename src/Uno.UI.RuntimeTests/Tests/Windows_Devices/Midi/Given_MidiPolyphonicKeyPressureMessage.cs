using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiPolyphonicKeyPressureMessage
    {
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiPolyphonicKeyPressureMessage(6, 120, 103);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 166, 120, 103 }, data);
		}

		[TestMethod]
		public void When_Channel_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiNoteOnMessage(16, 10, 10));
		}

		[TestMethod]
		public void When_Note_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiNoteOnMessage(12, 128, 10));
		}

		[TestMethod]
		public void When_Pressure_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiNoteOnMessage(12, 10, 128));
		}
	}
}
