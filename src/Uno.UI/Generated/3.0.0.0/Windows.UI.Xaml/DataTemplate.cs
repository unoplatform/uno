#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataTemplate : global::Windows.UI.Xaml.FrameworkTemplate
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ExtensionInstanceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"ExtensionInstance", typeof(global::Windows.UI.Xaml.IDataTemplateExtension), 
			typeof(global::Windows.UI.Xaml.DataTemplate), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.IDataTemplateExtension)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.DataTemplate.DataTemplate()
		// Forced skipping of method Windows.UI.Xaml.DataTemplate.DataTemplate()
		// Skipping already declared method Windows.UI.Xaml.DataTemplate.LoadContent()
		// Forced skipping of method Windows.UI.Xaml.DataTemplate.ExtensionInstanceProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.IDataTemplateExtension GetExtensionInstance( global::Windows.UI.Xaml.FrameworkElement element)
		{
			return (global::Windows.UI.Xaml.IDataTemplateExtension)element.GetValue(ExtensionInstanceProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetExtensionInstance( global::Windows.UI.Xaml.FrameworkElement element,  global::Windows.UI.Xaml.IDataTemplateExtension value)
		{
			element.SetValue(ExtensionInstanceProperty, value);
		}
		#endif
	}
}
