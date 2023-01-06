using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[SampleControlInfo("ContentPresenter", "ContentPresenter_Background")]
	public sealed partial class ContentPresenter_Background : UserControl
    {
        public ContentPresenter_Background()
        {
#if !WINDOWS_UWP
			FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent = false;
#endif
            this.InitializeComponent();
        }
    }
}
