using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_Storage_Streams
{
	[TestClass]
	public class Given_StorageStreamReference
	{
		[TestMethod]
		public void When_Create_Then_DelegateDeferredToOpen()
		{
			var invokeCount = 0;
			var sut = new RandomAccessStreamReference(ct =>
			{
				invokeCount++;
				return Task.FromResult(default(IRandomAccessStreamWithContentType));
			});

			Assert.AreEqual(0, invokeCount);

			sut.OpenReadAsync();

			Assert.AreEqual(1, invokeCount);
		}
	}
}
