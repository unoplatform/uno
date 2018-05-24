#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Navigation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationFailedEventArgs 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool NavigationFailedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Navigation.NavigationFailedEventArgs", "bool NavigationFailedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Exception Exception
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception NavigationFailedEventArgs.Exception is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Type SourcePageType
		{
			get
			{
				throw new global::System.NotImplementedException("The member Type NavigationFailedEventArgs.SourcePageType is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Navigation.NavigationFailedEventArgs.Exception.get
		// Forced skipping of method Windows.UI.Xaml.Navigation.NavigationFailedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Navigation.NavigationFailedEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Xaml.Navigation.NavigationFailedEventArgs.SourcePageType.get
	}
}
