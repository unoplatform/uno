#nullable enable

using System.Threading.Tasks;
using AndroidX.DocumentFile.Provider;
using Java.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
#pragma warning disable MSTEST0016 // Test class should have at least one test method - https://github.com/microsoft/testfx/issues/4543
	[TestClass]
	public class Given_StorageFile_Native : Given_StorageFile_Native_Base
	{
		protected override Task<StorageFolder> GetRootFolderAsync()
		{
			var localCache = ApplicationData.Current.LocalCacheFolder.Path;
			var directory = new File(localCache);
			var documentFile = DocumentFile.FromFile(directory);
			var rootFolder = StorageFolder.GetFromSafDocument(documentFile);
			return Task.FromResult(rootFolder);
		}
	}
}
