#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class UnhandledExceptionEventArgs 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UnhandledExceptionEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.UnhandledExceptionEventArgs", "bool UnhandledExceptionEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Exception Exception
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception UnhandledExceptionEventArgs.Exception is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UnhandledExceptionEventArgs.Message is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.UnhandledExceptionEventArgs.Exception.get
		// Forced skipping of method Windows.UI.Xaml.UnhandledExceptionEventArgs.Message.get
		// Forced skipping of method Windows.UI.Xaml.UnhandledExceptionEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.UnhandledExceptionEventArgs.Handled.set
	}
}
