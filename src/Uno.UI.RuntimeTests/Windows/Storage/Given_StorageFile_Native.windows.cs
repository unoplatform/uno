#if WINAPPSDK
#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_StorageFile_Native : Given_StorageFile_Native_Base
	{
		private StorageFolder? _rootFolder;

		protected override async Task<StorageFolder> GetRootFolderAsync()
		{
			var testFolderName = Guid.NewGuid().ToString();
			_rootFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testFolderName);
			return _rootFolder;
		}

		protected override async Task CleanupRootFolderAsync()
		{
			if (_rootFolder != null)
			{
				await _rootFolder.DeleteAsync();
			}
		}
	}
}
#endif
