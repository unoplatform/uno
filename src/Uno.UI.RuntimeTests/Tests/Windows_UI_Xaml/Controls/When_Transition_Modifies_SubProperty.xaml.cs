using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
namespace Uno.UI.RuntimeTests;

public sealed partial class When_Transition_Modifies_SubProperty : UserControl
{
	public When_Transition_Modifies_SubProperty()
	{
		this.InitializeComponent();
		VisualStateManager.GoToState(control, "Green", true);
	}

	private void RedButton_Click(object sender, RoutedEventArgs e)
	{
		VisualStateManager.GoToState(control, "Red", true);
	}

	private void GreenButton_Click(object sender, RoutedEventArgs e)
	{
		VisualStateManager.GoToState(control, "Green", true);
	}
}
