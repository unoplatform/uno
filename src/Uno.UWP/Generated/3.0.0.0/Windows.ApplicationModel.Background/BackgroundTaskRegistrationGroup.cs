#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackgroundTaskRegistrationGroup 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, global::Windows.ApplicationModel.Background.BackgroundTaskRegistration> AllTasks
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, BackgroundTaskRegistration> BackgroundTaskRegistrationGroup.AllTasks is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BackgroundTaskRegistrationGroup.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BackgroundTaskRegistrationGroup.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BackgroundTaskRegistrationGroup( string id) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup", "BackgroundTaskRegistrationGroup.BackgroundTaskRegistrationGroup(string id)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup.BackgroundTaskRegistrationGroup(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BackgroundTaskRegistrationGroup( string id,  string name) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup", "BackgroundTaskRegistrationGroup.BackgroundTaskRegistrationGroup(string id, string name)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup.BackgroundTaskRegistrationGroup(string, string)
		// Forced skipping of method Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup.Id.get
		// Forced skipping of method Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup.Name.get
		// Forced skipping of method Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup.BackgroundActivated.add
		// Forced skipping of method Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup.BackgroundActivated.remove
		// Forced skipping of method Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup.AllTasks.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup, global::Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs> BackgroundActivated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup", "event TypedEventHandler<BackgroundTaskRegistrationGroup, BackgroundActivatedEventArgs> BackgroundTaskRegistrationGroup.BackgroundActivated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup", "event TypedEventHandler<BackgroundTaskRegistrationGroup, BackgroundActivatedEventArgs> BackgroundTaskRegistrationGroup.BackgroundActivated");
			}
		}
		#endif
	}
}
