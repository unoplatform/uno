#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimePickerFlyout 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan Time
		{
			get
			{
				return (global::System.TimeSpan)this.GetValue(TimeProperty);
			}
			set
			{
				this.SetValue(TimeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int MinuteIncrement
		{
			get
			{
				return (int)this.GetValue(MinuteIncrementProperty);
			}
			set
			{
				this.SetValue(MinuteIncrementProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string ClockIdentifier
		{
			get
			{
				return (string)this.GetValue(ClockIdentifierProperty);
			}
			set
			{
				this.SetValue(ClockIdentifierProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ClockIdentifierProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ClockIdentifier", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.TimePickerFlyout), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinuteIncrementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinuteIncrement", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.TimePickerFlyout), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Time", typeof(global::System.TimeSpan), 
			typeof(global::Windows.UI.Xaml.Controls.TimePickerFlyout), 
			new FrameworkPropertyMetadata(default(global::System.TimeSpan)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public TimePickerFlyout() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePickerFlyout", "TimePickerFlyout.TimePickerFlyout()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimePickerFlyout()
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.ClockIdentifier.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.ClockIdentifier.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.Time.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.Time.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.MinuteIncrement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.MinuteIncrement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimePicked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimePicked.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.TimeSpan?> ShowAtAsync( global::Windows.UI.Xaml.FrameworkElement target)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<TimeSpan?> TimePickerFlyout.ShowAtAsync(FrameworkElement target) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.ClockIdentifierProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.MinuteIncrementProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TimePickerFlyout, global::Windows.UI.Xaml.Controls.TimePickedEventArgs> TimePicked
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePickerFlyout", "event TypedEventHandler<TimePickerFlyout, TimePickedEventArgs> TimePickerFlyout.TimePicked");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePickerFlyout", "event TypedEventHandler<TimePickerFlyout, TimePickedEventArgs> TimePickerFlyout.TimePicked");
			}
		}
		#endif
	}
}
