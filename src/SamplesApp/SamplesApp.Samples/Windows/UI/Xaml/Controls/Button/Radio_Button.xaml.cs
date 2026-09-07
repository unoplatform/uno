using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", Name = "Radio_Button", ViewModelType = typeof(ButtonTestsViewModel), IgnoreInSnapshotTests = true)]

	public sealed partial class Radio_Button : UserControl
	{
		public Radio_Button()
		{
			this.InitializeComponent();
		}
	}
}
