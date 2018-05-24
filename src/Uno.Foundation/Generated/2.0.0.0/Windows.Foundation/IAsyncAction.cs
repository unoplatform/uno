#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAsyncAction : global::Windows.Foundation.IAsyncInfo
	{
		#if false || false || false || false
		global::Windows.Foundation.AsyncActionCompletedHandler Completed
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Foundation.IAsyncAction.Completed.set
		// Forced skipping of method Windows.Foundation.IAsyncAction.Completed.get
		#if false || false || false || false
		void GetResults();
		#endif
	}
}
