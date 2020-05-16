using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiNoteOffMessage
	{
		[TestMethod]
		public void When_RawData()
		{
			var message = new MidiNoteOffMessage(12, 36, 17);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 140, 36, 17 }, data);
		}

		[TestMethod]
		public void When_Channel_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiNoteOffMessage(16, 10, 10));
		}

		[TestMethod]
		public void When_Note_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiNoteOffMessage(12, 128, 10));
		}

		[TestMethod]
		public void When_Velocity_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiNoteOffMessage(12, 10, 128));
		}
	}
}
