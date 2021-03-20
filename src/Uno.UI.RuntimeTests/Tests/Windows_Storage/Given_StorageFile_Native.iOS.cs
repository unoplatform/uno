#nullable enable

using System.Threading.Tasks;
using Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_StorageFile_Native : Given_StorageFile_Native_Base
	{
		protected override Task<StorageFolder> GetRootFolderAsync()
		{
			var path = ApplicationData.Current.LocalCacheFolder.Path;
			var url = new NSUrl(path, true);
			return Task.FromResult(StorageFolder.GetFromSecurityScopedUrl(url, null));
		}
	}
}
