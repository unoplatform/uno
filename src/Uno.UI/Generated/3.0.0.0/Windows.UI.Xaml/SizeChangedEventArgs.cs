#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SizeChangedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size NewSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size SizeChangedEventArgs.NewSize is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size PreviousSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size SizeChangedEventArgs.PreviousSize is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.SizeChangedEventArgs.PreviousSize.get
		// Forced skipping of method Windows.UI.Xaml.SizeChangedEventArgs.NewSize.get
	}
}
