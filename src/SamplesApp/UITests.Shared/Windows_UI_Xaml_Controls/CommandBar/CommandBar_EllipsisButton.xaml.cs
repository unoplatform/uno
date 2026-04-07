using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[Sample("CommandBar", Name = "CommandBar_EllipsisButton",
		Description = "Repro for https://github.com/unoplatform/uno/issues/4395: " +
		              "CommandBar ellipsis button (⋯) should be (1) vertically centered and " +
		              "(2) colored Red via AppBarEllipsisButtonForeground resource.")]
	public sealed partial class CommandBar_EllipsisButton : UserControl
	{
		public CommandBar_EllipsisButton()
		{
			InitializeComponent();
		}
	}
}
