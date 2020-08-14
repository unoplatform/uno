#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class LaunchUriResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.ValueSet Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet LaunchUriResult.Result is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Status
		// Forced skipping of method Windows.System.LaunchUriResult.Status.get
		// Forced skipping of method Windows.System.LaunchUriResult.Result.get
	}
}
