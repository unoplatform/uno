using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public class Given_MidiSongSelectMessage
	{
		[TestMethod]
		public void When_RawData()
		{
			var message = new MidiSongSelectMessage(62);
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 243, 62 }, data);
		}

		[TestMethod]
		public void When_Song_Out_Of_Bounds()
		{
			Assert.ThrowsExactly<ArgumentException>(
				() => new MidiSongSelectMessage(128));
		}
	}
}
