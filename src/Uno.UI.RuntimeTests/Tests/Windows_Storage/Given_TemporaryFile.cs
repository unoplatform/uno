#if !WINAPPSDK
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_TemporaryFile
	{
		[TestMethod]
		public void When_CloseLast_Then_FileDeleted()
		{
			var sut = new TemporaryFile();
			var tempFilePath = sut.ToString().Substring("[TEMP]".Length); // I'm not even ashamed of this hack

			var weak = sut.OpenWeak(FileAccess.Write);
			var strong = sut.Open(FileAccess.Read);

			Assert.IsTrue(File.Exists(tempFilePath));

			var expected = Encoding.UTF8.GetBytes("Hello world");

			weak.Write(expected, 0, expected.Length);
			weak.Flush();

			var actualBytes = new byte[512];
			var actualRead = strong.Read(actualBytes, 0, actualBytes.Length);
			var actual = Encoding.UTF8.GetString(actualBytes, 0, actualRead);

			Assert.AreEqual("Hello world", actual);

			strong.Dispose();

			try
			{
				weak.Write(expected, 0, expected.Length);
				weak.Flush();
				Assert.Fail("Write should have failed");
			}
			catch (IOException)
			{
			}

			Assert.IsFalse(File.Exists(tempFilePath));
		}
	}
}
#endif
