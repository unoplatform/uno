#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAsyncInfo 
	{
		#if false || false || false || false
		global::System.Exception ErrorCode
		{
			get;
		}
		#endif
		#if false || false || false || false
		uint Id
		{
			get;
		}
		#endif
		#if false || false || false || false
		global::Windows.Foundation.AsyncStatus Status
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Foundation.IAsyncInfo.Id.get
		// Forced skipping of method Windows.Foundation.IAsyncInfo.Status.get
		// Forced skipping of method Windows.Foundation.IAsyncInfo.ErrorCode.get
		#if false || false || false || false
		void Cancel();
		#endif
		#if false || false || false || false
		void Close();
		#endif
	}
}
