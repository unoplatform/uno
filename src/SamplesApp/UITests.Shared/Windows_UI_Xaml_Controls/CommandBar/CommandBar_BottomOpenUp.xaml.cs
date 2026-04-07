using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[Sample("CommandBar", Name = "CommandBar_BottomOpenUp",
		Description = "Repro for https://github.com/unoplatform/uno/issues/1612: " +
		              "CommandBar at the bottom should use OpenUp visual states (expand upward). " +
		              "Tap the expand chevron — the secondary commands should appear above the bar.")]
	public sealed partial class CommandBar_BottomOpenUp : UserControl
	{
		public CommandBar_BottomOpenUp()
		{
			InitializeComponent();
		}
	}
}
