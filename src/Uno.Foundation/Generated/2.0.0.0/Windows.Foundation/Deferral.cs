#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Deferral : global::System.IDisposable
	{
		// Skipping already declared method Windows.Foundation.Deferral.Deferral(Windows.Foundation.DeferralCompletedHandler)
		// Forced skipping of method Windows.Foundation.Deferral.Deferral(Windows.Foundation.DeferralCompletedHandler)
		// Skipping already declared method Windows.Foundation.Deferral.Complete()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Deferral", "void Deferral.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
