#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RepeatButton : global::Windows.UI.Xaml.Controls.Primitives.ButtonBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int Interval
		{
			get
			{
				return (int)this.GetValue(IntervalProperty);
			}
			set
			{
				this.SetValue(IntervalProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int Delay
		{
			get
			{
				return (int)this.GetValue(DelayProperty);
			}
			set
			{
				this.SetValue(DelayProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DelayProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Delay", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.RepeatButton), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IntervalProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Interval", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.RepeatButton), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public RepeatButton() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.RepeatButton", "RepeatButton.RepeatButton()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.RepeatButton.RepeatButton()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.RepeatButton.Delay.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.RepeatButton.Delay.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.RepeatButton.Interval.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.RepeatButton.Interval.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.RepeatButton.DelayProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.RepeatButton.IntervalProperty.get
	}
}
