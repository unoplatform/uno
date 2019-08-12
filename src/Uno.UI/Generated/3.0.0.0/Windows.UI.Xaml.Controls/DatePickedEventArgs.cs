#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatePickedEventArgs : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset NewDate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset DatePickedEventArgs.NewDate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset OldDate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset DatePickedEventArgs.OldDate is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.DatePickedEventArgs.DatePickedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickedEventArgs.DatePickedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickedEventArgs.OldDate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickedEventArgs.NewDate.get
	}
}
