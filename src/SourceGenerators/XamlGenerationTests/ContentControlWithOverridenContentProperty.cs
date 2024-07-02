
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{

	[ContentProperty(Name = "ContentOverride")]
	public partial class ContentControlWithOverridenContentProperty : ContentControl
	{
		public ContentControlWithOverridenContentProperty()
		{
		}


		public object ContentOverride
		{
			get { return (object)this.GetValue(ContentOverrideProperty); }
			set { this.SetValue(ContentOverrideProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentOverrideProperty =
			DependencyProperty.Register("ContentOverride", typeof(object), typeof(LocalControlWithContent), new FrameworkPropertyMetadata(0));
	}
}
