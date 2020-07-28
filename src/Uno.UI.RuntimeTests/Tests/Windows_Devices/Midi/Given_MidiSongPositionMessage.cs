using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiSongPositionPointerMessage
	{
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiSongPositionPointerMessage(15130);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 242, 26, 118 }, data);
		}

		[TestMethod]
		public void When_Beats_Out_Of_Bounds()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new MidiPitchBendChangeMessage(12, 16384));
		}
	}
}
