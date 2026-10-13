using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_Storage_Streams
{
	[UITestClass]
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

			_ = sut.OpenReadAsync();

			Assert.AreEqual(1, invokeCount);
		}

		[TestMethod]
		public async Task When_CreateFromStream_Then_OpenedStreamIsBorrowed()
		{
			using var stream = new MemoryStream(new byte[] { 1, 2, 3 }).AsRandomAccessStream();
			var sut = RandomAccessStreamReference.CreateFromStream(stream);

			Assert.IsFalse(sut.OwnsOpenedStream);
			// Every open hands out the caller's stream (possibly wrapped for its content type), position included.
			var first = await sut.OpenReadAsync();
			first.Seek(2);
			Assert.AreEqual(2ul, stream.Position);
			var second = await sut.OpenReadAsync();
			Assert.AreEqual(2ul, second.Position);
		}

		[TestMethod]
		public void When_CreateFromOpen_Then_OpenedStreamIsOwned()
		{
			var sut = new RandomAccessStreamReference(ct => Task.FromResult(default(IRandomAccessStreamWithContentType)));

			Assert.IsTrue(sut.OwnsOpenedStream);
		}
	}
}
