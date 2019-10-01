using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Clip
{
	[SampleControlInfo(category: "Clip", viewModelType: typeof(ButtonTestsViewModel))]
	public sealed partial class XamlButtonWithClipping : UserControl
	{
		public XamlButtonWithClipping()
		{
			this.InitializeComponent();
		}
	}
}
