using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml.VisualStateTests
{
	[SampleControlInfo("Visual states", "VisualState_ComplexSetters_Automated")]
	public sealed partial class VisualState_ComplexSetters_Automated : UserControl
	{
		public VisualState_ComplexSetters_Automated()
		{
			this.InitializeComponent();
		}

		private void OnClick(object sender, object args)
		{
			VisualStateManager.GoToState(this, "State01", true);
		}
	}
}
