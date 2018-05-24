#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDStreamParser 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Media.Protection.PlayReady.NDStreamParserNotifier Notifier
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void ParseData( byte[] dataBytes);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint GetStreamInformation( global::Windows.Media.Core.IMediaStreamDescriptor descriptor, out global::Windows.Media.Protection.PlayReady.NDMediaStreamType streamType);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void BeginOfStream();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void EndOfStream();
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDStreamParser.Notifier.get
	}
}
