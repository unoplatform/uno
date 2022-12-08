using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Xaml.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_ApplicationStorage
	{
		[TestMethod]
		public async Task When_FileExistsInPackage_Nested()
		{
			var fileExists = await StorageFileHelper.Exists("Assets/Fonts/uno-fluentui-assets.ttf");

			Assert.IsTrue(fileExists);
		}

		[TestMethod]
		public async Task When_FileExistsInPackage_RootPath()
		{
			var fileExists = await StorageFileHelper.Exists("Asset_GetFileFromApplicationUriAsync.xml");

			Assert.IsTrue(fileExists);
		}

		[TestMethod]
		public async Task When_ResourceFileExistsInPackage_Nested()
		{
			var fileExists = await StorageFileHelper.Exists("Assets/Icons/menu.png");

			Assert.IsTrue(fileExists);
		}

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

		[TestMethod]
		public async Task When_GetFileFromApplicationUriAsync_Image()
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Button.png"));

			Assert.IsTrue((await FileIO.ReadBufferAsync(file)).Length > 0);
		}

		[TestMethod]
		public async Task When_GetFileFromApplicationUriAsync_Image_Nested()
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Icons/menu.png"));

			Assert.IsTrue((await FileIO.ReadBufferAsync(file)).Length > 0);
		}
	}
}
