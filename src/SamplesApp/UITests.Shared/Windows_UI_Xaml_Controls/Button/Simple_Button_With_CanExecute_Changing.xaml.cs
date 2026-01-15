using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", Name = "Simple_Button_With_CanExecute_Changing", ViewModelType = typeof(ButtonTestsViewModel), IgnoreInSnapshotTests = true)]

	public sealed partial class Simple_Button_With_CanExecute_Changing : UserControl
	{
		public Simple_Button_With_CanExecute_Changing()
		{
			this.InitializeComponent();
		}
	}
}
