using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml.Clipping
{
	[SampleControlInfo(category: "Clipping", viewModelType: typeof(ButtonTestsViewModel))]
	public sealed partial class XamlButtonWithClipping : UserControl
	{
		public XamlButtonWithClipping()
		{
			this.InitializeComponent();
		}
	}
}
