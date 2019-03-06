#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimePickedEventArgs : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan NewTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan TimePickedEventArgs.NewTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan OldTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan TimePickedEventArgs.OldTime is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.TimePickedEventArgs.TimePickedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickedEventArgs.TimePickedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickedEventArgs.OldTime.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickedEventArgs.NewTime.get
	}
}
