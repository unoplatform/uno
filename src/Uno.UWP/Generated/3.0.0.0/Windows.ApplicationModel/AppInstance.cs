#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppInstance 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCurrentInstance
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppInstance.IsCurrentInstance is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20AppInstance.IsCurrentInstance");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Key
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppInstance.Key is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppInstance.Key");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.AppInstance RecommendedInstance
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppInstance AppInstance.RecommendedInstance is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInstance%20AppInstance.RecommendedInstance");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppInstance.Key.get
		// Forced skipping of method Windows.ApplicationModel.AppInstance.IsCurrentInstance.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RedirectActivationTo()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppInstance", "void AppInstance.RedirectActivationTo()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppInstance.RecommendedInstance.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Activation.IActivatedEventArgs GetActivatedEventArgs()
		{
			throw new global::System.NotImplementedException("The member IActivatedEventArgs AppInstance.GetActivatedEventArgs() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IActivatedEventArgs%20AppInstance.GetActivatedEventArgs%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.AppInstance FindOrRegisterInstanceForKey( string key)
		{
			throw new global::System.NotImplementedException("The member AppInstance AppInstance.FindOrRegisterInstanceForKey(string key) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInstance%20AppInstance.FindOrRegisterInstanceForKey%28string%20key%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void Unregister()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppInstance", "void AppInstance.Unregister()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IList<global::Windows.ApplicationModel.AppInstance> GetInstances()
		{
			throw new global::System.NotImplementedException("The member IList<AppInstance> AppInstance.GetInstances() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IList%3CAppInstance%3E%20AppInstance.GetInstances%28%29");
		}
		#endif
	}
}
