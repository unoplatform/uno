using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[SampleControlInfo(
		"ContentPresenter",
		"ContentPresenter_Changing_ContentTemplate",
		description: "ContentPresenter where ContentTemplate can be toggled between non-null and null. Content view should be visible when null.",
		ignoreInSnapshotTests: true)]
	public sealed partial class ContentPresenter_Changing_ContentTemplate : UserControl
	{
		public ContentPresenter_Changing_ContentTemplate()
		{
#if HAS_UNO
			FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent = false;
#endif
			this.InitializeComponent();
		}

		private DataTemplate _stashedTemplate;
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			var oldTemplate = TargetContentPresenter.ContentTemplate;
			TargetContentPresenter.ContentTemplate = _stashedTemplate;
			_stashedTemplate = oldTemplate;
		}
	}
}
