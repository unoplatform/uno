#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WindowingEnvironmentAddedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.WindowingEnvironment WindowingEnvironment
		{
			get
			{
				throw new global::System.NotImplementedException("The member WindowingEnvironment WindowingEnvironmentAddedEventArgs.WindowingEnvironment is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.WindowingEnvironmentAddedEventArgs.WindowingEnvironment.get
	}
}
