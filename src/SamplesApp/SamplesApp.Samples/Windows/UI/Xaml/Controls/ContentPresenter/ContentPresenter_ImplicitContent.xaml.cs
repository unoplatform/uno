using Microsoft.UI.Xaml.Controls;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ContentPresenter
{
	[Sample("ContentPresenter", Name = "ContentPresenter_ImplicitContent")]
	public sealed partial class ContentPresenter_ImplicitContent : Page
	{
		public ContentPresenter_ImplicitContent()
		{
#if HAS_UNO
			FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent = true;
#endif
			this.InitializeComponent();
		}
	}
}
