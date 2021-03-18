using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class Given_WindowsRuntimeStreamExtensions
	{
		[TestMethod]
		public async Task When_StreamAsRandomAccessStream()
		{
			var src = new MemoryStream();
			var sut = src.AsRandomAccessStream();

			await StreamTestHelper.Test(writeTo: src, readOn: sut, directWrapper: true);
			await StreamTestHelper.Test(writeTo: sut, readOn: src, directWrapper: true);
		}

		[TestMethod]
		public async Task When_StreamAsInputStream()
		{
			var src = new MemoryStream();
			var sut = src.AsInputStream();

			await StreamTestHelper.Test(writeTo: src, readOn: sut, directWrapper: true);
		}

		[TestMethod]
		public async Task When_StreamAsOutputStream()
		{
			var src = new MemoryStream();
			var sut = src.AsOutputStream();

			await StreamTestHelper.Test(writeTo: sut, readOn: src, directWrapper: true);
		}
	}
}
