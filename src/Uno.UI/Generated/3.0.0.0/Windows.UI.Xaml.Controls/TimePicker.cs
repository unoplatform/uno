#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimePicker : global::Windows.UI.Xaml.Controls.Control
	{
		#if false || false || NET461 || __WASM__ || false
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
		#if false || false || NET461 || __WASM__ || false
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
			}
		}
		#endif
		// Skipping already declared property ClockIdentifier
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan? SelectedTime
		{
			get
			{
				return (global::System.TimeSpan?)this.GetValue(SelectedTimeProperty);
			}
			set
			{
				this.SetValue(SelectedTimeProperty, value);
			}
		}
		#endif
		// Skipping already declared property ClockIdentifierProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.TimePicker), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.TimePicker), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinuteIncrementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinuteIncrement", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.TimePicker), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Time", typeof(global::System.TimeSpan), 
			typeof(global::Windows.UI.Xaml.Controls.TimePicker), 
			new FrameworkPropertyMetadata(default(global::System.TimeSpan)));
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LightDismissOverlayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode), 
			typeof(global::Windows.UI.Xaml.Controls.TimePicker), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedTimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedTime", typeof(global::System.TimeSpan?), 
			typeof(global::Windows.UI.Xaml.Controls.TimePicker), 
			new FrameworkPropertyMetadata(default(global::System.TimeSpan?)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.TimePicker.TimePicker()
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.TimePicker()
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.ClockIdentifier.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.ClockIdentifier.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.MinuteIncrement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.MinuteIncrement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.Time.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.Time.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.TimeChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.TimeChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.SelectedTime.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.SelectedTime.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.SelectedTimeChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.SelectedTimeChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.SelectedTimeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.ClockIdentifierProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.MinuteIncrementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePicker.TimeProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<global::Windows.UI.Xaml.Controls.TimePickerValueChangedEventArgs> TimeChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePicker", "event EventHandler<TimePickerValueChangedEventArgs> TimePicker.TimeChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePicker", "event EventHandler<TimePickerValueChangedEventArgs> TimePicker.TimeChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TimePicker, global::Windows.UI.Xaml.Controls.TimePickerSelectedValueChangedEventArgs> SelectedTimeChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePicker", "event TypedEventHandler<TimePicker, TimePickerSelectedValueChangedEventArgs> TimePicker.SelectedTimeChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePicker", "event TypedEventHandler<TimePicker, TimePickerSelectedValueChangedEventArgs> TimePicker.SelectedTimeChanged");
			}
		}
		#endif
	}
}
