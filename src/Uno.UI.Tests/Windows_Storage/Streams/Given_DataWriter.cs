using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage.Streams;

namespace Uno.UI.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class Given_DataWriter
	{
		[TestMethod]
		public void Given_DateTime_MinValue()
		{
			var dataWriter = new DataWriter();
			dataWriter.WriteDateTime(DateTimeOffset.MinValue);
			var bytes = dataWriter.DetachBuffer().ToArray();
			CollectionAssert.AreEqual(new byte[] { 248, 254, 49, 232, 221, 137, 0, 0 }, bytes);
		}
	}
}
