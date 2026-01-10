using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[Sample("ContentPresenter", "ContentPresenter_TextProperties")]
	public sealed partial class ContentPresenter_TextProperties : UserControl
	{
		public ContentPresenter_TextProperties()
		{
#if HAS_UNO
			FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent = false;
#endif
			this.InitializeComponent();
		}
	}
}
