using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", Name = "Custom_Button_With_ContentTemplate", ViewModelType = typeof(ButtonTestsViewModel))]

	public sealed partial class Custom_Button_With_ContentTemplate : UserControl
	{
		public Custom_Button_With_ContentTemplate()
		{
			this.InitializeComponent();
		}
	}
}
