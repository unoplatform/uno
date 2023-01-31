#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CrossSlidingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.CrossSlidingState CrossSlidingState
		{
			get
			{
				throw new global::System.NotImplementedException("The member CrossSlidingState CrossSlidingEventArgs.CrossSlidingState is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CrossSlidingState%20CrossSlidingEventArgs.CrossSlidingState");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Input.PointerDeviceType PointerDeviceType
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerDeviceType CrossSlidingEventArgs.PointerDeviceType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PointerDeviceType%20CrossSlidingEventArgs.PointerDeviceType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point CrossSlidingEventArgs.Position is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Point%20CrossSlidingEventArgs.Position");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ContactCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CrossSlidingEventArgs.ContactCount is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20CrossSlidingEventArgs.ContactCount");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.CrossSlidingEventArgs.PointerDeviceType.get
		// Forced skipping of method Windows.UI.Input.CrossSlidingEventArgs.Position.get
		// Forced skipping of method Windows.UI.Input.CrossSlidingEventArgs.CrossSlidingState.get
		// Forced skipping of method Windows.UI.Input.CrossSlidingEventArgs.ContactCount.get
	}
}
