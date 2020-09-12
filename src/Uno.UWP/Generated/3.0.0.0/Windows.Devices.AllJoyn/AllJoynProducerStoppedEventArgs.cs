#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynProducerStoppedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member int AllJoynProducerStoppedEventArgs.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynProducerStoppedEventArgs( int status) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynProducerStoppedEventArgs", "AllJoynProducerStoppedEventArgs.AllJoynProducerStoppedEventArgs(int status)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynProducerStoppedEventArgs.AllJoynProducerStoppedEventArgs(int)
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynProducerStoppedEventArgs.Status.get
	}
}
