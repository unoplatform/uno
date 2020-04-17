#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaTransportControlsHelper 
	{
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DropoutOrderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"DropoutOrder", typeof(int?), 
			typeof(global::Windows.UI.Xaml.Controls.MediaTransportControlsHelper), 
			new FrameworkPropertyMetadata(default(int?)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaTransportControlsHelper.DropoutOrderProperty.get
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static int? GetDropoutOrder( global::Windows.UI.Xaml.UIElement element)
		{
			return (int?)element.GetValue(DropoutOrderProperty);
		}
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static void SetDropoutOrder( global::Windows.UI.Xaml.UIElement element,  int? value)
		{
			element.SetValue(DropoutOrderProperty, value);
		}
		#endif
	}
}
