#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	public delegate void AsyncOperationCompletedHandler<TResult>(global::Windows.Foundation.IAsyncOperation<TResult> @asyncInfo, global::Windows.Foundation.AsyncStatus @asyncStatus);
	#endif
}
