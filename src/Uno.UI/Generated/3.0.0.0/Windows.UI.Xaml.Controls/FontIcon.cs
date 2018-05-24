#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FontIcon 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FontFamilyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FontFamily", typeof(global::Windows.UI.Xaml.Media.FontFamily), 
			typeof(global::Windows.UI.Xaml.Controls.FontIcon), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.FontFamily)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FontSizeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FontSize", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.FontIcon), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FontStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FontStyle", typeof(global::Windows.UI.Text.FontStyle), 
			typeof(global::Windows.UI.Xaml.Controls.FontIcon), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Text.FontStyle)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FontWeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FontWeight", typeof(global::Windows.UI.Text.FontWeight), 
			typeof(global::Windows.UI.Xaml.Controls.FontIcon), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Text.FontWeight)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty GlyphProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Glyph", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.FontIcon), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsTextScaleFactorEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsTextScaleFactorEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.FontIcon), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MirroredWhenRightToLeftProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MirroredWhenRightToLeft", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.FontIcon), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public FontIcon() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.FontIcon", "FontIcon.FontIcon()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontIcon()
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.Glyph.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.Glyph.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontSize.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontSize.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontFamily.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontFamily.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontWeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontWeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.IsTextScaleFactorEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.IsTextScaleFactorEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.MirroredWhenRightToLeft.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.MirroredWhenRightToLeft.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.MirroredWhenRightToLeftProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.IsTextScaleFactorEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.GlyphProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontSizeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontFamilyProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontWeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FontIcon.FontStyleProperty.get
	}
}
