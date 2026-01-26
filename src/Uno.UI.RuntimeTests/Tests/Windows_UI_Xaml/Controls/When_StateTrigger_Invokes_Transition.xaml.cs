using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class When_StateTrigger_Invokes_Transition : UserControl
{
	public When_StateTrigger_Invokes_Transition()
	{
		InitializeComponent();
	}

	public Border GetTestBorder() => TestBorder;

	public VisualStateGroup GetSizeStates() => SizeStates;

	public AdaptiveTrigger GetWideTrigger() => WideTrigger;

	public AdaptiveTrigger GetNarrowTrigger() => NarrowTrigger;
}
