#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatePickerFlyout 
	{
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
		public static global::Windows.UI.Xaml.DependencyProperty CalendarIdentifierProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CalendarIdentifier", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Date", typeof(global::System.DateTimeOffset), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(global::System.DateTimeOffset)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DayVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DayVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaxYearProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxYear", typeof(global::System.DateTimeOffset), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(global::System.DateTimeOffset)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinYearProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinYear", typeof(global::System.DateTimeOffset), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(global::System.DateTimeOffset)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MonthVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MonthVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty YearVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"YearVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DayFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DayFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MonthFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MonthFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty YearFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"YearFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyout), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public DatePickerFlyout() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.DatePickerFlyout", "DatePickerFlyout.DatePickerFlyout()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DatePickerFlyout()
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.CalendarIdentifier.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.CalendarIdentifier.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.Date.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.Date.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DayVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DayVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MonthVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MonthVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.YearVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.YearVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MinYear.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MinYear.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MaxYear.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MaxYear.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DatePicked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DatePicked.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.DateTimeOffset?> ShowAtAsync( global::Windows.UI.Xaml.FrameworkElement target)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DateTimeOffset?> DatePickerFlyout.ShowAtAsync(FrameworkElement target) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DayFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DayFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MonthFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MonthFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.YearFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.YearFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DayFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MonthFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.YearFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.CalendarIdentifierProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.DayVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MonthVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.YearVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MinYearProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyout.MaxYearProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.DatePickerFlyout, global::Windows.UI.Xaml.Controls.DatePickedEventArgs> DatePicked
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.DatePickerFlyout", "event TypedEventHandler<DatePickerFlyout, DatePickedEventArgs> DatePickerFlyout.DatePicked");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.DatePickerFlyout", "event TypedEventHandler<DatePickerFlyout, DatePickedEventArgs> DatePickerFlyout.DatePicked");
			}
		}
		#endif
	}
}
