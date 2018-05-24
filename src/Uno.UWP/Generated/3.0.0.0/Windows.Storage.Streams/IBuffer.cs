#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBuffer 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint Capacity
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint Length
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.IBuffer.Capacity.get
		// Forced skipping of method Windows.Storage.Streams.IBuffer.Length.get
		// Forced skipping of method Windows.Storage.Streams.IBuffer.Length.set
	}
}
