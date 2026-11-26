using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.FlyoutPages;

public sealed partial class NamedMenuFlyoutPage : Page
{
	public NamedMenuFlyoutPage()
	{
		InitializeComponent();
	}

	internal Button TargetButton => targetButton;

	internal Button SecondTargetButton => secondTargetButton;

	internal MenuFlyout NamedMenuFlyout => namedMenuFlyout;
}
