#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SuspendingEventArgs : global::Windows.ApplicationModel.ISuspendingEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.SuspendingOperation SuspendingOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member SuspendingOperation SuspendingEventArgs.SuspendingOperation is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.SuspendingEventArgs.SuspendingOperation.get
		// Processing: Windows.ApplicationModel.ISuspendingEventArgs
	}
}
