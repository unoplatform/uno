#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMemoryBufferReference : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint Capacity
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Foundation.IMemoryBufferReference.Capacity.get
		// Forced skipping of method Windows.Foundation.IMemoryBufferReference.Closed.add
		// Forced skipping of method Windows.Foundation.IMemoryBufferReference.Closed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Foundation.IMemoryBufferReference, object> Closed;
		#endif
	}
}
