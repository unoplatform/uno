#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAsyncOperationWithProgress<TResult, TProgress> : global::Windows.Foundation.IAsyncInfo
	{
		#if false
		global::Windows.Foundation.AsyncOperationProgressHandler<TResult, TProgress> Progress
		{
			get;
			set;
		}
		#endif
		#if false
		global::Windows.Foundation.AsyncOperationWithProgressCompletedHandler<TResult, TProgress> Completed
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress>.Progress.set
		// Forced skipping of method Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress>.Progress.get
		// Forced skipping of method Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress>.Completed.set
		// Forced skipping of method Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress>.Completed.get
		#if false
		TResult GetResults();
		#endif
	}
}
