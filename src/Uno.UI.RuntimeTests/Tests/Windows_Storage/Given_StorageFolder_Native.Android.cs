#nullable enable

using System;
using System.Threading.Tasks;
using AndroidX.DocumentFile.Provider;
using Java.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_StorageFolder_Native : Given_StorageFolder_Native_Base
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
