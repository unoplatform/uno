using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Storage
{
	internal static partial class StorageFolderNative
	{
		[JSImport("globalThis.Windows.Storage.StorageFolder.makePersistent")]
		internal static partial void NativeMakePersistent(string[] paths);
	}
}
