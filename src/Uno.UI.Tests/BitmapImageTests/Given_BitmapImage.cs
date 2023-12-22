using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

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
			var SUT = new BitmapImage(uri);

			Assert.AreEqual(new Uri(expected), SUT.AbsoluteUri);
		}
	}
}
