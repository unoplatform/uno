using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{

	[ContentProperty(Name = "Content")]
	public partial class LocalControlWithContent : Control
	{
		public LocalControlWithContent()
		{
		}


		public object Content
		{
			get { return (object)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register("Content", typeof(object), typeof(LocalControlWithContent), new FrameworkPropertyMetadata(0));
	}
}
