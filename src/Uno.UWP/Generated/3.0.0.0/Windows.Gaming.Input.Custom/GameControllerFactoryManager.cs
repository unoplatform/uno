#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.Custom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GameControllerFactoryManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Gaming.Input.IGameController TryGetFactoryControllerFromGameController( global::Windows.Gaming.Input.Custom.ICustomGameControllerFactory factory,  global::Windows.Gaming.Input.IGameController gameController)
		{
			throw new global::System.NotImplementedException("The member IGameController GameControllerFactoryManager.TryGetFactoryControllerFromGameController(ICustomGameControllerFactory factory, IGameController gameController) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void RegisterCustomFactoryForGipInterface( global::Windows.Gaming.Input.Custom.ICustomGameControllerFactory factory,  global::System.Guid interfaceId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.GameControllerFactoryManager", "void GameControllerFactoryManager.RegisterCustomFactoryForGipInterface(ICustomGameControllerFactory factory, Guid interfaceId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void RegisterCustomFactoryForHardwareId( global::Windows.Gaming.Input.Custom.ICustomGameControllerFactory factory,  ushort hardwareVendorId,  ushort hardwareProductId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.GameControllerFactoryManager", "void GameControllerFactoryManager.RegisterCustomFactoryForHardwareId(ICustomGameControllerFactory factory, ushort hardwareVendorId, ushort hardwareProductId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void RegisterCustomFactoryForXusbType( global::Windows.Gaming.Input.Custom.ICustomGameControllerFactory factory,  global::Windows.Gaming.Input.Custom.XusbDeviceType xusbType,  global::Windows.Gaming.Input.Custom.XusbDeviceSubtype xusbSubtype)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.GameControllerFactoryManager", "void GameControllerFactoryManager.RegisterCustomFactoryForXusbType(ICustomGameControllerFactory factory, XusbDeviceType xusbType, XusbDeviceSubtype xusbSubtype)");
		}
		#endif
	}
}
