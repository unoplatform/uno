using Windows.UI.Core;
using Windows.UI.Xaml;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[Sample("ContentPresenter", Name = "ContentPresenter_NativeEmbedding")]
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
					this_.Content = CoreWindow.Main.CreateSampleComponent(text);
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
