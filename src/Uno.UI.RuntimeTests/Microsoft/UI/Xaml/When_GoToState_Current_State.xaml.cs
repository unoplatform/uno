using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests;

public sealed partial class When_GoToState_Current_State : UserControl
{
	public When_GoToState_Current_State()
	{
		this.InitializeComponent();
	}

	internal ContentControl Host => StatesHost;
}
