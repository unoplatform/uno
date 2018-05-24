#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ImageBrush 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.ImageSource ImageSource
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.ImageSource)this.GetValue(ImageSourceProperty);
			}
			set
			{
				this.SetValue(ImageSourceProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ImageSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ImageSource", typeof(global::Windows.UI.Xaml.Media.ImageSource), 
			typeof(global::Windows.UI.Xaml.Media.ImageBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.ImageSource)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ImageBrush() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.ImageBrush", "ImageBrush.ImageBrush()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageBrush()
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageSource.get
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageSource.set
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageFailed.add
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageFailed.remove
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageOpened.add
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageOpened.remove
		// Forced skipping of method Windows.UI.Xaml.Media.ImageBrush.ImageSourceProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.ExceptionRoutedEventHandler ImageFailed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.ImageBrush", "event ExceptionRoutedEventHandler ImageBrush.ImageFailed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.ImageBrush", "event ExceptionRoutedEventHandler ImageBrush.ImageFailed");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler ImageOpened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.ImageBrush", "event RoutedEventHandler ImageBrush.ImageOpened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.ImageBrush", "event RoutedEventHandler ImageBrush.ImageOpened");
			}
		}
		#endif
	}
}
