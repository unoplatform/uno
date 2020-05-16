using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiStopMessage
	{
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiStopMessage();
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 252 }, data);
		}
	}
}
