#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAsyncActionWithProgress<TProgress> : global::Windows.Foundation.IAsyncInfo
	{
		#if false || false || false || false
		global::Windows.Foundation.AsyncActionProgressHandler<TProgress> Progress
		{
			get;
			set;
		}
		#endif
		#if false || false || false || false
		global::Windows.Foundation.AsyncActionWithProgressCompletedHandler<TProgress> Completed
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Foundation.IAsyncActionWithProgress<TProgress>.Progress.set
		// Forced skipping of method Windows.Foundation.IAsyncActionWithProgress<TProgress>.Progress.get
		// Forced skipping of method Windows.Foundation.IAsyncActionWithProgress<TProgress>.Completed.set
		// Forced skipping of method Windows.Foundation.IAsyncActionWithProgress<TProgress>.Completed.get
		#if false || false || false || false
		void GetResults();
		#endif
	}
}
