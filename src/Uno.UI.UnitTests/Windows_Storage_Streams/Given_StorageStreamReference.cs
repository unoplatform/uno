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
		public async Task When_CreateFromStream_Then_EachOpenIsIndependent()
		{
			using var stream = new MemoryStream(new byte[] { 1, 2, 3 }).AsRandomAccessStream();
			stream.Seek(1);
			var sut = RandomAccessStreamReference.CreateFromStream(stream);

			var first = await sut.OpenReadAsync();
			Assert.AreEqual(0ul, first.Position);
			first.Seek(2);
			var second = await sut.OpenReadAsync();
			Assert.AreEqual(0ul, second.Position);
			Assert.AreEqual(1ul, stream.Position);

			first.Dispose();
			Assert.AreEqual(3ul, stream.Size, "Disposing an opened stream must leave the caller's stream open.");
		}

		[TestMethod]
		public async Task When_OpenedStreamCloned_Then_ContentTypePreserved()
		{
			using var stream = new MemoryStream(new byte[] { 1, 2, 3 }).AsRandomAccessStream();
			var sut = RandomAccessStreamReference.CreateFromStream(stream);

			using var opened = await sut.OpenReadAsync();
			using var clone = opened.CloneStream();

			var typed = clone as IRandomAccessStreamWithContentType;
			Assert.IsNotNull(typed);
			Assert.AreEqual(opened.ContentType, typed.ContentType);
		}
	}
}
