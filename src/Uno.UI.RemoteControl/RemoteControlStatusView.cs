using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Uno.UI.RemoteControl.RemoteControlClient;

namespace Uno.UI.RemoteControl;

internal sealed partial class RemoteControlStatusView : Ellipse
{
	public RemoteControlStatusView()
	{
		Fill = new SolidColorBrush(Colors.Gray);
		Width = 20;
		Height = 20;
	}

	public bool IsAutoHideEnabled { get; set; }

	public void Update(Status status)
	{
		var (kind, message) = status.GetSummary();
		((SolidColorBrush)Fill).Color = kind switch
		{
			StatusClassification.Ok => Colors.Green,
			StatusClassification.Info => Colors.Yellow,
			StatusClassification.Warning => Colors.Orange,
			StatusClassification.Error => Colors.Red,
			_ => Colors.Gray
		};
		ToolTipService.SetToolTip(this, message);

		if (IsAutoHideEnabled)
		{
			Visibility = kind is StatusClassification.Ok
				? Microsoft.UI.Xaml.Visibility.Collapsed
				: Microsoft.UI.Xaml.Visibility.Visible;
		}
	}
}
