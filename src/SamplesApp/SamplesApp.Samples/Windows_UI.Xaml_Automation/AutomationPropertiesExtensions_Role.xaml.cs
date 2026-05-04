using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation;

[Sample(
	"Automation",
	Description = "Demonstrates AutomationPropertiesExtensions.Role, an Uno-specific attached property that overrides the platform accessibility role. On native WASM it sets the HTML 'role' attribute directly. On Skia-WASM it is consumed by the accessibility overlay via AutomationProperties.FindHtmlRole.")]
public sealed partial class AutomationPropertiesExtensions_Role : UserControl
{
	public AutomationPropertiesExtensions_Role()
	{
		this.InitializeComponent();
	}
}
