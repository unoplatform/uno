#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISuspendingOperation 
	{
		#if false || false || false || false || false
		global::System.DateTimeOffset Deadline
		{
			get;
		}
		#endif
		#if false || false || false || false || false
		global::Windows.ApplicationModel.SuspendingDeferral GetDeferral();
		#endif
		// Forced skipping of method Windows.ApplicationModel.ISuspendingOperation.Deadline.get
	}
}
