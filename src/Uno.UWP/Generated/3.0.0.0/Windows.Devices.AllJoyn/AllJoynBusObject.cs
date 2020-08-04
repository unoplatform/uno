#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynBusObject 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.AllJoyn.AllJoynBusAttachment BusAttachment
		{
			get
			{
				throw new global::System.NotImplementedException("The member AllJoynBusAttachment AllJoynBusObject.BusAttachment is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.AllJoyn.AllJoynSession Session
		{
			get
			{
				throw new global::System.NotImplementedException("The member AllJoynSession AllJoynBusObject.Session is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynBusObject( string objectPath) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "AllJoynBusObject.AllJoynBusObject(string objectPath)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynBusObject.AllJoynBusObject(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynBusObject( string objectPath,  global::Windows.Devices.AllJoyn.AllJoynBusAttachment busAttachment) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "AllJoynBusObject.AllJoynBusObject(string objectPath, AllJoynBusAttachment busAttachment)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynBusObject.AllJoynBusObject(string, Windows.Devices.AllJoyn.AllJoynBusAttachment)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynBusObject() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "AllJoynBusObject.AllJoynBusObject()");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynBusObject.AllJoynBusObject()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "void AllJoynBusObject.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "void AllJoynBusObject.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddProducer( global::Windows.Devices.AllJoyn.IAllJoynProducer producer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "void AllJoynBusObject.AddProducer(IAllJoynProducer producer)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynBusObject.BusAttachment.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynBusObject.Session.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynBusObject.Stopped.add
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynBusObject.Stopped.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.AllJoyn.AllJoynBusObject, global::Windows.Devices.AllJoyn.AllJoynBusObjectStoppedEventArgs> Stopped
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "event TypedEventHandler<AllJoynBusObject, AllJoynBusObjectStoppedEventArgs> AllJoynBusObject.Stopped");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynBusObject", "event TypedEventHandler<AllJoynBusObject, AllJoynBusObjectStoppedEventArgs> AllJoynBusObject.Stopped");
			}
		}
		#endif
	}
}
