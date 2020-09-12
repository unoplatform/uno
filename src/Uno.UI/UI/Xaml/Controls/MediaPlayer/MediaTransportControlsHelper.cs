#if __ANDROID__ || __IOS__ || __MACOS__
namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControlsHelper 
	{
		public static DependencyProperty DropoutOrderProperty { get; } = 
			DependencyProperty.RegisterAttached(
				"DropoutOrder",
				typeof(int?), 
				typeof(MediaTransportControlsHelper), 
				new FrameworkPropertyMetadata(default(int?)));

		public static int? GetDropoutOrder(UIElement element)
		{
			return (int?)element.GetValue(DropoutOrderProperty);
		}

		public static void SetDropoutOrder(UIElement element, int? value)
		{
			element.SetValue(DropoutOrderProperty, value);
		}
	}
}
#endif
