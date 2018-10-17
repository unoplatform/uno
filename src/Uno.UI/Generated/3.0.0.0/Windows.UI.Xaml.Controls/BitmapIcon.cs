#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET46 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BitmapIcon : global::Windows.UI.Xaml.Controls.IconElement
	{
		#if false || false || NET46 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  global::System.Uri UriSource
		{
			get
			{
				return (global::System.Uri)this.GetValue(UriSourceProperty);
			}
			set
			{
				this.SetValue(UriSourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool ShowAsMonochrome
		{
			get
			{
				return (bool)this.GetValue(ShowAsMonochromeProperty);
			}
			set
			{
				this.SetValue(ShowAsMonochromeProperty, value);
			}
		}
		#endif
		#if false || false || NET46 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty UriSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"UriSource", typeof(global::System.Uri), 
			typeof(global::Windows.UI.Xaml.Controls.BitmapIcon), 
			new FrameworkPropertyMetadata(default(global::System.Uri)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ShowAsMonochromeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ShowAsMonochrome", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.BitmapIcon), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET46 || __WASM__ || false
		[global::Uno.NotImplemented]
		public BitmapIcon() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.BitmapIcon", "BitmapIcon.BitmapIcon()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.BitmapIcon.BitmapIcon()
		// Forced skipping of method Windows.UI.Xaml.Controls.BitmapIcon.UriSource.get
		// Forced skipping of method Windows.UI.Xaml.Controls.BitmapIcon.UriSource.set
		// Forced skipping of method Windows.UI.Xaml.Controls.BitmapIcon.ShowAsMonochrome.get
		// Forced skipping of method Windows.UI.Xaml.Controls.BitmapIcon.ShowAsMonochrome.set
		// Forced skipping of method Windows.UI.Xaml.Controls.BitmapIcon.ShowAsMonochromeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.BitmapIcon.UriSourceProperty.get
	}
}
