using System;
using System.Threading.Tasks;
using Uno.Extensions.Specialized;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_ApplicationStorage
	{

		[TestMethod]
		public async Task When_ExistFilesInPackage()
		{
			var fileExists = await Uno.UI.Toolkit.StorageFileHelper.GetFilesInDirectoryAsync(null);

			Assert.IsTrue(fileExists.Any());
		}

		[TestMethod]
		public async Task When_ExtensionsFilterCountDifferentFromAllInPackage()
		{
			var filteredFileExists = await Uno.UI.Toolkit.StorageFileHelper.GetFilesInDirectoryAsync([".png"]);
			var allFileExists = await Uno.UI.Toolkit.StorageFileHelper.GetFilesInDirectoryAsync(null);

			Assert.IsFalse(allFileExists.Count() == filteredFileExists.Count());
		}

		[TestMethod]
		public async Task When_ExtensionsFilterOnlyPathsForPngFiles()
		{
			var filteredFileExists = await Uno.UI.Toolkit.StorageFileHelper.GetFilesInDirectoryAsync([".png"]);
			var allFileExists = await Uno.UI.Toolkit.StorageFileHelper.GetFilesInDirectoryAsync(null);

			Assert.IsTrue(filteredFileExists.Count() == filteredFileExists.Where(e => e.ToString().EndsWith(".png")).Count());
		}

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
