using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

public sealed partial class FindName_NonFrameworkElement_Page : Page
{
	public FindName_NonFrameworkElement_Page()
	{
		InitializeComponent();
	}

	internal Run CompiledRunElement => CompiledRun;

	internal Hyperlink CompiledLinkElement => CompiledLink;

	internal MenuFlyout CompiledMenuElement => CompiledMenu;

	internal Flyout CompiledFlyoutElement => CompiledFlyout;

	internal Button FlyoutButtonElement => FlyoutButton;

	internal ScaleTransform CompiledTransformElement => CompiledTransform;
}
