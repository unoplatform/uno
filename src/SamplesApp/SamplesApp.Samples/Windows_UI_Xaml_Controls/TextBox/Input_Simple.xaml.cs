using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", Name = "Input_Simple", ViewModelType = typeof(TextBoxViewModel))]
	public sealed partial class Input_Simple : UserControl
	{
		public Input_Simple()
		{
			this.InitializeComponent();
		}
	}
}
