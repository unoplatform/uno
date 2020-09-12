#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatePickerFlyoutItem : global::Windows.UI.Xaml.DependencyObject,global::Windows.UI.Xaml.Data.ICustomPropertyProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SecondaryText
		{
			get
			{
				return (string)this.GetValue(SecondaryTextProperty);
			}
			set
			{
				this.SetValue(SecondaryTextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PrimaryText
		{
			get
			{
				return (string)this.GetValue(PrimaryTextProperty);
			}
			set
			{
				this.SetValue(PrimaryTextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Type Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member Type DatePickerFlyoutItem.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PrimaryTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PrimaryText), typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyoutItem), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SecondaryTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SecondaryText), typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyoutItem), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutItem.PrimaryText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutItem.PrimaryText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutItem.SecondaryText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutItem.SecondaryText.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Data.ICustomProperty GetCustomProperty( string name)
		{
			throw new global::System.NotImplementedException("The member ICustomProperty DatePickerFlyoutItem.GetCustomProperty(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Data.ICustomProperty GetIndexedProperty( string name,  global::System.Type type)
		{
			throw new global::System.NotImplementedException("The member ICustomProperty DatePickerFlyoutItem.GetIndexedProperty(string name, Type type) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetStringRepresentation()
		{
			throw new global::System.NotImplementedException("The member string DatePickerFlyoutItem.GetStringRepresentation() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutItem.Type.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutItem.PrimaryTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutItem.SecondaryTextProperty.get
		// Processing: Windows.UI.Xaml.Data.ICustomPropertyProvider
	}
}
