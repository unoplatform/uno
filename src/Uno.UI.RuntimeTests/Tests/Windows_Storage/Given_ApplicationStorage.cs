using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_ApplicationStorage
	{

		[TestMethod]
		public async Task When_FileDoesNotExistsInPackage()
		{
			var fileExists = await Uno.UI.Toolkit.StorageFileHelper.ExistsInPackage("Asset_InvalidFile.xml");

			Assert.IsFalse(fileExists);
		}

		[TestMethod]
		public async Task When_FileExistsInPackage_Nested()
		{
			var fileExists = await Uno.UI.Toolkit.StorageFileHelper.ExistsInPackage("Assets/Fonts/RoteFlora.ttf");

			Assert.IsTrue(fileExists);
		}

		[TestMethod]
		public async Task When_FileExistsInPackage_RootPath()
		{
			var fileExists = await Uno.UI.Toolkit.StorageFileHelper.ExistsInPackage("Asset_GetFileFromApplicationUriAsync.xml");

			Assert.IsTrue(fileExists);
		}

		[TestMethod]
		public async Task When_ResourceFileExistsInPackage_Nested()
		{
			var fileExists = await Uno.UI.Toolkit.StorageFileHelper.ExistsInPackage("Assets/Icons/menu.png");

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

			Assert.IsGreaterThan<uint>(0, (await FileIO.ReadBufferAsync(file)).Length);
		}

		[TestMethod]
		public async Task When_GetFileFromApplicationUriAsync_Image_Nested()
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Icons/menu.png"));

			Assert.IsGreaterThan<uint>(0, (await FileIO.ReadBufferAsync(file)).Length);
		}
	}
}
