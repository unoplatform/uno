#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatePicker : global::Windows.UI.Xaml.Controls.Control
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset MaxYear
		{
			get
			{
				return (global::System.DateTimeOffset)this.GetValue(MaxYearProperty);
			}
			set
			{
				this.SetValue(MaxYearProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string MonthFormat
		{
			get
			{
				return (string)this.GetValue(MonthFormatProperty);
			}
			set
			{
				this.SetValue(MonthFormatProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool DayVisible
		{
			get
			{
				return (bool)this.GetValue(DayVisibleProperty);
			}
			set
			{
				this.SetValue(DayVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string DayFormat
		{
			get
			{
				return (string)this.GetValue(DayFormatProperty);
			}
			set
			{
				this.SetValue(DayFormatProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string CalendarIdentifier
		{
			get
			{
				return (string)this.GetValue(CalendarIdentifierProperty);
			}
			set
			{
				this.SetValue(CalendarIdentifierProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool YearVisible
		{
			get
			{
				return (bool)this.GetValue(YearVisibleProperty);
			}
			set
			{
				this.SetValue(YearVisibleProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset Date
		{
			get
			{
				return (global::System.DateTimeOffset)this.GetValue(DateProperty);
			}
			set
			{
				this.SetValue(DateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string YearFormat
		{
			get
			{
				return (string)this.GetValue(YearFormatProperty);
			}
			set
			{
				this.SetValue(YearFormatProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Orientation Orientation
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Orientation)this.GetValue(OrientationProperty);
			}
			set
			{
				this.SetValue(OrientationProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool MonthVisible
		{
			get
			{
				return (bool)this.GetValue(MonthVisibleProperty);
			}
			set
			{
				this.SetValue(MonthVisibleProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset MinYear
		{
			get
			{
				return (global::System.DateTimeOffset)this.GetValue(MinYearProperty);
			}
			set
			{
				this.SetValue(MinYearProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Orientation", typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty YearFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"YearFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty YearVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"YearVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CalendarIdentifierProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CalendarIdentifier", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Date", typeof(global::System.DateTimeOffset), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::System.DateTimeOffset)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DayFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DayFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DayVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DayVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaxYearProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxYear", typeof(global::System.DateTimeOffset), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::System.DateTimeOffset)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinYearProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinYear", typeof(global::System.DateTimeOffset), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::System.DateTimeOffset)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MonthFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MonthFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MonthVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MonthVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LightDismissOverlayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public DatePicker() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.DatePicker", "DatePicker.DatePicker()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DatePicker()
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.CalendarIdentifier.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.CalendarIdentifier.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Date.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Date.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MinYear.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MinYear.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MaxYear.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MaxYear.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DateChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DateChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.CalendarIdentifierProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MinYearProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MaxYearProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.OrientationProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<global::Windows.UI.Xaml.Controls.DatePickerValueChangedEventArgs> DateChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.DatePicker", "event EventHandler<DatePickerValueChangedEventArgs> DatePicker.DateChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.DatePicker", "event EventHandler<DatePickerValueChangedEventArgs> DatePicker.DateChanged");
			}
		}
		#endif
	}
}
