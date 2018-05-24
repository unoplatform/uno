#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class RoutedEventArgs 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object OriginalSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member object RoutedEventArgs.OriginalSource is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public RoutedEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.RoutedEventArgs", "RoutedEventArgs.RoutedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.RoutedEventArgs.RoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.RoutedEventArgs.OriginalSource.get
	}
}
