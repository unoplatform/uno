using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", "Simple_Button", typeof(ButtonTestsViewModel), ignoreInSnapshotTests: true)]

	public sealed partial class Simple_Button : UserControl
	{
		public Simple_Button()
		{
			this.InitializeComponent();
		}
	}
}
