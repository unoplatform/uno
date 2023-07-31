#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
<<<<<<< HEAD
	#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	#endif
	public  partial class MediaTransportControlsHelper 
	{
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Windows.UI.Xaml.DependencyProperty DropoutOrderProperty { get; } = 
=======
#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
	public partial class MediaTransportControlsHelper
	{
#if false || false || false || false || false || false || false
		internal MediaTransportControlsHelper()
		{
		}
#endif
#if false || false || false || false || false || false || false
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Windows.UI.Xaml.DependencyProperty DropoutOrderProperty { get; } =
>>>>>>> 113f95dd8e (feat(MediaTransportControl): import Dropout and Expand)
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"DropoutOrder", typeof(int?), 
			typeof(global::Windows.UI.Xaml.Controls.MediaTransportControlsHelper), 
			new Windows.UI.Xaml.FrameworkPropertyMetadata(default(int?)));
<<<<<<< HEAD
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaTransportControlsHelper.DropoutOrderProperty.get
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static int? GetDropoutOrder( global::Windows.UI.Xaml.UIElement element)
		{
			return (int?)element.GetValue(DropoutOrderProperty);
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static void SetDropoutOrder( global::Windows.UI.Xaml.UIElement element,  int? value)
		{
			element.SetValue(DropoutOrderProperty, value);
		}
		#endif
	}
=======
#endif
        // Forced skipping of method Windows.UI.Xaml.Controls.MediaTransportControlsHelper.DropoutOrderProperty.get
#if false || false || false || false || false || false || false
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static int? GetDropoutOrder(global::Windows.UI.Xaml.UIElement element)
		{
			return (int?)element.GetValue(DropoutOrderProperty);
		}
#endif
#if false || false || false || false || false || false || false
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static void SetDropoutOrder(global::Windows.UI.Xaml.UIElement element, int? value)
		{
			element.SetValue(DropoutOrderProperty, value);
		}
#endif
    }
>>>>>>> 113f95dd8e (feat(MediaTransportControl): import Dropout and Expand)
}
