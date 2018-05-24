#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Deferral : global::System.IDisposable
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Deferral( global::Windows.Foundation.DeferralCompletedHandler handler) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Deferral", "Deferral.Deferral(DeferralCompletedHandler handler)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Deferral.Deferral(Windows.Foundation.DeferralCompletedHandler)
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Complete()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Deferral", "void Deferral.Complete()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Deferral", "void Deferral.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
