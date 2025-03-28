using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[SampleControlInfo("Buttons", "Simple_Button", typeof(ButtonTestsViewModel), ignoreInSnapshotTests: true)]

	public sealed partial class Simple_Button : UserControl
	{
		public Simple_Button()
		{
			this.InitializeComponent();
		}
	}
}
