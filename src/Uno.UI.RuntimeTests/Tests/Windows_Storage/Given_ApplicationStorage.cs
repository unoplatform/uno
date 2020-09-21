using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;
using Windows.UI.Xaml.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_ApplicationStorage
	{
		[TestMethod]
		public async Task When_GetFileFromApplicationUriAsync_RootPath()
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Asset_GetFileFromApplicationUriAsync.xml"));

			var content = await FileIO.ReadTextAsync(file);

			Assert.AreEqual("<SomeContent/>", content);
		}

		[TestMethod]
		public async Task When_GetFileFromApplicationUriAsync_Nested()
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Asset_GetFileFromApplicationUriAsync_Nested.xml"));

			var content = await FileIO.ReadTextAsync(file);

			Assert.AreEqual("<SomeContent/>", content);
		}
	}
}
