using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[SampleControlInfo("Buttons", "CheckBox_Button_With_CanExecute_Changing", typeof(ButtonTestsViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class CheckBox_Button_With_CanExecute_Changing : UserControl
	{
		public CheckBox_Button_With_CanExecute_Changing()
		{
			this.InitializeComponent();
		}
	}
}
