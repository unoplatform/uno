#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDProximityDetectionCompletedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint ProximityDetectionRetryCount
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDProximityDetectionCompletedEventArgs.ProximityDetectionRetryCount.get
	}
}
