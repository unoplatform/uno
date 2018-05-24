#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataContextChangedEventArgs 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DataContextChangedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DataContextChangedEventArgs", "bool DataContextChangedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object NewValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member object DataContextChangedEventArgs.NewValue is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DataContextChangedEventArgs.NewValue.get
		// Forced skipping of method Windows.UI.Xaml.DataContextChangedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.DataContextChangedEventArgs.Handled.set
	}
}
