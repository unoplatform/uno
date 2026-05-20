using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[Sample("ContentPresenter", Name = "ContentPresenter_Content_DataContext")]
	public sealed partial class ContentPresenter_Content_DataContext : UserControl
	{
		public ContentPresenter_Content_DataContext()
		{
#if HAS_UNO
			FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent = false;
#endif
			this.InitializeComponent();
		}
	}
}
