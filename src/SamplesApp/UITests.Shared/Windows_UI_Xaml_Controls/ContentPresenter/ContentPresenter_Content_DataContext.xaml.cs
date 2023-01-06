using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[SampleControlInfo("ContentPresenter", "ContentPresenter_Content_DataContext")]
	public sealed partial class ContentPresenter_Content_DataContext : UserControl
    {
        public ContentPresenter_Content_DataContext()
        {
#if !WINDOWS_UWP
			FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent = false;
#endif
            this.InitializeComponent();
        }
    }
}
