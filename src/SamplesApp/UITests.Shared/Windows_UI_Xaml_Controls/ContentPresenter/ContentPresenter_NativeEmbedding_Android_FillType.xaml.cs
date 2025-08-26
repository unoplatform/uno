using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.UI.Core;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[Sample("ContentPresenter", IsManualTest = true, Description = "You should see an orange ellipse on top of a WebView showing Google's homepage. Specifically, the issue reported in https://github.com/unoplatform/uno/issues/21312 should not reproduce.")]
	public sealed partial class ContentPresenter_NativeEmbedding_Android_FillType : UserControl
	{
		public ContentPresenter_NativeEmbedding_Android_FillType()
		{
			this.InitializeComponent();
		}
	}
}
