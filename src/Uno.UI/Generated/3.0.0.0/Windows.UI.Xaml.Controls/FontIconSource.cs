#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FontIconSource : global::Windows.UI.Xaml.Controls.IconSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MirroredWhenRightToLeft
		{
			get
			{
				return (bool)this.GetValue(MirroredWhenRightToLeftProperty);
			}
			set
			{
				this.SetValue(MirroredWhenRightToLeftProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsTextScaleFactorEnabled
		{
			get
			{
				return (bool)this.GetValue(IsTextScaleFactorEnabledProperty);
			}
			set
			{
				this.SetValue(IsTextScaleFactorEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Glyph
		{
			get
			{
				return (string)this.GetValue(GlyphProperty);
			}
			set
			{
				this.SetValue(GlyphProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.FontWeight FontWeight
		{
			get
			{
				return (global::Windows.UI.Text.FontWeight)this.GetValue(FontWeightProperty);
			}
			set
			{
				this.SetValue(FontWeightProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.FontStyle FontStyle
		{
			get
			{
				return (global::Windows.UI.Text.FontStyle)this.GetValue(FontStyleProperty);
			}
			set
			{
				this.SetValue(FontStyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double FontSize
		{
			get
			{
				return (double)this.GetValue(FontSizeProperty);
			}
			set
			{
				this.SetValue(FontSizeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.FontFamily FontFamily
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.FontFamily)this.GetValue(FontFamilyProperty);
			}
			set
			{
				this.SetValue(FontFamilyProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty FontFamilyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FontFamily), typeof(global::Windows.UI.Xaml.Media.FontFamily), 
			typeof(global::Windows.UI.Xaml.Controls.FontIconSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.FontFamily)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty FontSizeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FontSize), typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.FontIconSource), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty FontStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FontStyle), typeof(global::Windows.UI.Text.FontStyle), 
			typeof(global::Windows.UI.Xaml.Controls.FontIconSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Text.FontStyle)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty FontWeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FontWeight), typeof(global::Windows.UI.Text.FontWeight), 
			typeof(global::Windows.UI.Xaml.Controls.FontIconSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Text.FontWeight)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty GlyphProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Glyph), typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.FontIconSource), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsTextScaleFactorEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsTextScaleFactorEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.FontIconSource), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty MirroredWhenRightToLeftProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MirroredWhenRightToLeft), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.FontIconSource), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FontIconSource() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.FontIconSource", "FontIconSource.FontIconSource()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontIconSource()
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.Glyph.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.Glyph.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontSize.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontSize.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontFamily.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontFamily.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontWeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontWeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.IsTextScaleFactorEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.IsTextScaleFactorEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.MirroredWhenRightToLeft.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.MirroredWhenRightToLeft.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.GlyphProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontSizeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontFamilyProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontWeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.FontStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.IsTextScaleFactorEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIconSource.MirroredWhenRightToLeftProperty.get
	}
}
