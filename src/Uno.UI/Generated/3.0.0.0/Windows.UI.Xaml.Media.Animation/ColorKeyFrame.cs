#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ColorKeyFrame : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color Value
		{
			get
			{
				return (global::Windows.UI.Color)this.GetValue(ValueProperty);
			}
			set
			{
				this.SetValue(ValueProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.KeyTime KeyTime
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.KeyTime)this.GetValue(KeyTimeProperty);
			}
			set
			{
				this.SetValue(KeyTimeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty KeyTimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(KeyTime), typeof(global::Windows.UI.Xaml.Media.Animation.KeyTime), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorKeyFrame), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.KeyTime)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ValueProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Value), typeof(global::Windows.UI.Color), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorKeyFrame), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.ColorKeyFrame()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.ColorKeyFrame()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.Value.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.Value.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.KeyTime.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.KeyTime.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.ValueProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorKeyFrame.KeyTimeProperty.get
	}
}
