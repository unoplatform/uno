#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if __ANDROID__ || __IOS__
	public delegate void AsyncOperationCompletedHandler<TResult>(global::Windows.Foundation.IAsyncOperation<TResult> @asyncInfo, global::Windows.Foundation.AsyncStatus @asyncStatus);
	#endif
}
