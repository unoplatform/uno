using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.BitmapImageTests
{
	[TestClass]
	public class Given_BitmapImage
	{
		[TestMethod]
		[DataRow("http://example.com/image.png", "http://example.com/image.png")]
		[DataRow("ms-appx:///image.png", "ms-appx:///image.png")]
		[DataRow("ms-appx:///image.png", "image.png")]
		[DataRow("ms-appx:///folder/image.png", "folder/image.png")]
		[DataRow("ms-appx:///folder/image.png", "folder\\image.png")]
		[DataRow("ms-appx:///folder/folder2/image.png", "folder/folder2/image.png")]
		[DataRow("ms-appx:///folder/folder2/image.png", "folder\\folder2\\image.png")]
		[DataRow("ms-appx:///image.png", "/image.png")]
		[DataRow("ms-appx:///folder/image.png", "/folder/image.png")]
		public void When_Uri(string expected, string uri)
		{
			var SUT = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));

			Assert.AreEqual(new Uri(expected), SUT.AbsoluteUri);
		}

		[TestMethod]
		[DataRow("http://example.com/image.png", "http://example.com/image.png")]
		[DataRow("ms-appx:///image.png", "ms-appx:///image.png")]
		[DataRow("ms-appx:///image.png", "image.png")]
		[DataRow("ms-appx:///folder/image.png", "folder/image.png")]
		[DataRow("ms-appx:///folder/image.png", "folder\\image.png")]
		[DataRow("ms-appx:///folder/folder2/image.png", "folder/folder2/image.png")]
		[DataRow("ms-appx:///folder/folder2/image.png", "folder\\folder2\\image.png")]
		[DataRow("ms-appx:///image.png", "/image.png")]
		[DataRow("ms-appx:///folder/image.png", "/folder/image.png")]
		public void When_UriString(string expected, string uri)
		{
			var SUT = new BitmapImage(ImageSource.TryCreateUriFromString(uri));

			Assert.AreEqual(new Uri(expected), SUT.AbsoluteUri);
		}
	}
}
