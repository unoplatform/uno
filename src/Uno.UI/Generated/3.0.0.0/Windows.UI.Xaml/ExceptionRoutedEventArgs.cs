#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ExceptionRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string ErrorMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ExceptionRoutedEventArgs.ErrorMessage is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.ExceptionRoutedEventArgs.ErrorMessage.get
	}
}
