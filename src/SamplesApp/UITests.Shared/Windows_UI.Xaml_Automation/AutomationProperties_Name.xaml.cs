using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation
{
	[Sample("Automation", Description = "Accessibility voice over should read aloud the values for AutomationProperties.Name")]
	public sealed partial class AutomationProperties_Name : UserControl
	{
		public AutomationProperties_Name()
		{
			this.InitializeComponent();
		}
	}
}
