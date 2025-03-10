using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[Sample("ContentPresenter", Name = "ContentPresenter_NativeEmbedding", IsManualTest = true, Description = "This sample showcases embedding native elements into an Uno window. On X11, this sample needs Xterm to be installed and will need a few seconds to load. You should test that clipping scrolling works correctly with native elements and that opening the flyout will hide elements underneath it.")]
	public sealed partial class ContentPresenter_NativeEmbedding : UserControl
	{
		public ContentPresenter_NativeEmbedding()
		{
			this.InitializeComponent();
		}
	}

	public sealed partial class NativeControlHostWithText : ContentControl
	{
		public static DependencyProperty TextProperty { get; } = DependencyProperty.Register(
			"Text",
			typeof(string),
			typeof(NativeControlHostWithText),
			new PropertyMetadata("", propertyChangedCallback: (dependencyObject, args) =>
			{
#if __SKIA__
				if (dependencyObject is NativeControlHostWithText this_ && args.NewValue is string text)
				{
					if (this_.IsLoaded)
					{
						this_.Content = this_.FindFirstChild<Windows.UI.Xaml.Controls.ContentPresenter>().CreateSampleComponent(text);
					}
					else
					{
						// we need XamlRoot to not be null, so we have to wait until IsLoaded
						RoutedEventHandler onLoaded = null;
						onLoaded = (_, _) =>
						{
							this_.Content = this_.FindFirstChild<Windows.UI.Xaml.Controls.ContentPresenter>().CreateSampleComponent(text);
							this_.Loaded -= onLoaded;
						};

						this_.Loaded += onLoaded;
					}
				}
#endif
			}));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}
	}
}
