using Windows.Storage;

namespace Uno.Storage.Internal
{
	internal static class StorageProviders
	{
		public static StorageProvider Local { get; } = new StorageProvider("computer", "StorageProviderLocalDisplayName");

#if __WASM__
		public static StorageProvider WasmDownloadPicker { get; } = new StorageProvider("wasmdownloadpicker", "StorageProviderWasmDownloadPickerName");

		public static StorageProvider WasmNative { get; } = new StorageProvider("jsfileaccessapi", "StorageProviderWasmNativeDisplayName");
#endif

#if __ANDROID__
		public static StorageProvider AndroidSaf { get; } = new StorageProvider("androidsaf", "StorageProviderAndroidSafDisplayName");
#endif

#if __IOS__
		public static StorageProvider IosSecurityScoped { get; } = new StorageProvider("iossecurityscoped", "StorageProviderIosSecurityScopedDisplayName");
#endif
	}
}
