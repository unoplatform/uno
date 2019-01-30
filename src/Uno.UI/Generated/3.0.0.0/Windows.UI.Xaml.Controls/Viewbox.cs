#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Viewbox : global::Windows.UI.Xaml.FrameworkElement
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.StretchDirection StretchDirection
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.StretchDirection)this.GetValue(StretchDirectionProperty);
			}
			set
			{
				this.SetValue(StretchDirectionProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Stretch Stretch
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Stretch)this.GetValue(StretchProperty);
			}
			set
			{
				this.SetValue(StretchProperty, value);
			}
		}
		#endif
		// Skipping already declared property Child
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StretchDirectionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"StretchDirection", typeof(global::Windows.UI.Xaml.Controls.StretchDirection), 
			typeof(global::Windows.UI.Xaml.Controls.Viewbox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.StretchDirection)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StretchProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Stretch", typeof(global::Windows.UI.Xaml.Media.Stretch), 
			typeof(global::Windows.UI.Xaml.Controls.Viewbox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Stretch)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Viewbox.Viewbox()
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.Viewbox()
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.Child.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.Child.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.Stretch.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.Stretch.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.StretchDirection.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.StretchDirection.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.StretchProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Viewbox.StretchDirectionProperty.get
	}
}
