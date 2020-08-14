using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiStartMessage
	{
        [TestMethod]
		public void When_RawData()
		{
			var message = new MidiStartMessage();
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 250 }, data);
		}
	}
}
