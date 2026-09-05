using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation
{
	[Sample("Automation", Name = "AutomationProperties_Landmark", Description = "Landmark regions (Main/Navigation) plus a LocalizedLandmarkType-only region promoted as a Custom landmark.")]
	public sealed partial class AutomationProperties_Landmark : UserControl
	{
		public AutomationProperties_Landmark()
		{
			this.InitializeComponent();
		}
	}
}
