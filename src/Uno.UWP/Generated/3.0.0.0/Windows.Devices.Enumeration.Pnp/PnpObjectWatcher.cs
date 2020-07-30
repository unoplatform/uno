#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration.Pnp
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PnpObjectWatcher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceWatcherStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceWatcherStatus PnpObjectWatcher.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Added.add
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Added.remove
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Updated.add
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Updated.remove
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Removed.add
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Removed.remove
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.EnumerationCompleted.add
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.EnumerationCompleted.remove
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Stopped.add
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Stopped.remove
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectWatcher.Status.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "void PnpObjectWatcher.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "void PnpObjectWatcher.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.Pnp.PnpObjectWatcher, global::Windows.Devices.Enumeration.Pnp.PnpObject> Added
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, PnpObject> PnpObjectWatcher.Added");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, PnpObject> PnpObjectWatcher.Added");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.Pnp.PnpObjectWatcher, object> EnumerationCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, object> PnpObjectWatcher.EnumerationCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, object> PnpObjectWatcher.EnumerationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.Pnp.PnpObjectWatcher, global::Windows.Devices.Enumeration.Pnp.PnpObjectUpdate> Removed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, PnpObjectUpdate> PnpObjectWatcher.Removed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, PnpObjectUpdate> PnpObjectWatcher.Removed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.Pnp.PnpObjectWatcher, object> Stopped
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, object> PnpObjectWatcher.Stopped");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, object> PnpObjectWatcher.Stopped");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.Pnp.PnpObjectWatcher, global::Windows.Devices.Enumeration.Pnp.PnpObjectUpdate> Updated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, PnpObjectUpdate> PnpObjectWatcher.Updated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.Pnp.PnpObjectWatcher", "event TypedEventHandler<PnpObjectWatcher, PnpObjectUpdate> PnpObjectWatcher.Updated");
			}
		}
		#endif
	}
}
