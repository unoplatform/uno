using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[SampleControlInfo("Buttons", "Custom_Button_With_ContentTemplate", typeof(ButtonTestsViewModel))]

	public sealed partial class Custom_Button_With_ContentTemplate : UserControl
	{
		public Custom_Button_With_ContentTemplate()
		{
			this.InitializeComponent();
		}
	}
}
