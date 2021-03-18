using Windows.Storage;

namespace Uno.Storage.Internal
{
	internal static class StorageProviders
	{
		public static StorageProvider Local { get; } = new StorageProvider("computer", "StorageProviderLocalDisplayName");

#if __WASM__
		public static StorageProvider NativeWasm { get; } = new StorageProvider("jsfileaccessapi", "StorageProviderNativeWasmDisplayName");
#endif

#if __ANDROID__
		public static StorageProvider AndroidSaf { get; } = new StorageProvider("androidsaf", "StorageProviderAndroidSafDisplayName");
#endif

#if __IOS__
		public static StorageProvider IosSecurityScoped { get; } = new StorageProvider("iossecurityscoped", "StorageProviderIosSecurityScopedDisplayName");
#endif
	}
}
