using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation;

[Sample(
	"Automation",
	Description = "Demonstrates AutomationPropertiesExtensions.Role, an Uno-specific attached property that overrides the platform accessibility role. On WASM it sets the HTML 'role' attribute directly.")]
public sealed partial class AutomationPropertiesExtensions_Role : UserControl
{
	public AutomationPropertiesExtensions_Role()
	{
		this.InitializeComponent();
	}
}
