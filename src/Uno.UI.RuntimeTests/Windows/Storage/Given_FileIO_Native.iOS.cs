#nullable enable

using System.Threading.Tasks;
using Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
#pragma warning disable MSTEST0016 // Test class should have at least one test method - https://github.com/microsoft/testfx/issues/4543
	[TestClass]
	public class Given_FileIO_Native : Given_FileIO_Native_Base
	{
		protected override Task<StorageFolder> GetRootFolderAsync()
		{
			var path = ApplicationData.Current.LocalCacheFolder.Path;
			var url = new NSUrl(path, true);
			return Task.FromResult(StorageFolder.GetFromSecurityScopedUrl(url, null));
		}
	}
}
