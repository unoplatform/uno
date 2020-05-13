#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimelineMarker : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Type
		{
			get
			{
				return (string)this.GetValue(TypeProperty);
			}
			set
			{
				this.SetValue(TypeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Text
		{
			get
			{
				return (string)this.GetValue(TextProperty);
			}
			set
			{
				this.SetValue(TextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Text), typeof(string), 
			typeof(global::Windows.UI.Xaml.Media.TimelineMarker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Time), typeof(global::System.TimeSpan), 
			typeof(global::Windows.UI.Xaml.Media.TimelineMarker), 
			new FrameworkPropertyMetadata(default(global::System.TimeSpan)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TypeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Type), typeof(string), 
			typeof(global::Windows.UI.Xaml.Media.TimelineMarker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Media.TimelineMarker.TimelineMarker()
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.TimelineMarker()
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.Time.get
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.Time.set
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.Type.get
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.Type.set
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.Text.get
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.Text.set
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.TimeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.TypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.TimelineMarker.TextProperty.get
	}
}
