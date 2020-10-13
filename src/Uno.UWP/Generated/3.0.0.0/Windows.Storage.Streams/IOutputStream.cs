#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IOutputStream : global::System.IDisposable
	{
		#if false
		global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync( global::Windows.Storage.Streams.IBuffer buffer);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<bool> FlushAsync();
		#endif
	}
}
