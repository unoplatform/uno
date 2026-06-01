#nullable enable

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_StorageFile_Native : Given_StorageFile_Native_Base
	{
		protected override async Task<StorageFolder> GetRootFolderAsync()
		{
			var folder = await StorageFolder.GetPrivateRootAsync()!;
			if (folder == null)
			{
				Assert.Inconclusive("File System Access API not available on this browser.");
			}
			return folder!;
		}
	}
}
