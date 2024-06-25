using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Uno.UI.RemoteControl.RemoteControlStatus;

namespace Uno.UI.RemoteControl;

internal sealed partial class RemoteControlStatusView : Ellipse
{
	private readonly RemoteControlClient? _devServer;

#if __IOS__
	public new const string Id = nameof(RemoteControlStatusView);
#else
	public const string Id = nameof(RemoteControlStatusView);
#endif

	#region Status (DP)
	public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
		nameof(Status),
		typeof(RemoteControlStatus),
		typeof(RemoteControlStatusView),
		new PropertyMetadata(default(RemoteControlStatus), (snd, args) => ((RemoteControlStatusView)snd).OnStatusChanged((RemoteControlStatus)args.NewValue)));

	public RemoteControlStatus Status
	{
		get => (RemoteControlStatus)GetValue(StatusProperty);
		private set => SetValue(StatusProperty, value);
	}
	#endregion

	#region HeadLine (DP)
	public static readonly DependencyProperty HeadLineProperty = DependencyProperty.Register(
		nameof(HeadLine),
		typeof(string),
		typeof(RemoteControlStatusView),
		new PropertyMetadata(default(string)));

	public string HeadLine
	{
		get => (string)GetValue(HeadLineProperty);
		private set => SetValue(HeadLineProperty, value);
	}
	#endregion

	internal bool HasServer => _devServer is not null;

	public RemoteControlStatusView()
		: this(RemoteControlClient.Instance)
	{
	}

	public RemoteControlStatusView(RemoteControlClient? devServer)
	{
		_devServer = devServer;

		Fill = new SolidColorBrush(Colors.Gray);
		Width = 20;
		Height = 20;

		if (_devServer is null)
		{
			return;
		}

		Loading += static (snd, _) =>
		{
			if (snd is RemoteControlStatusView that)
			{
				that._devServer!.StatusChanged += that.OnDevServerStatusChanged;
				that.Status = that._devServer.Status;
			}
		};
		Unloaded += static (snd, _) =>
		{
			if (snd is RemoteControlStatusView that)
			{
				that._devServer!.StatusChanged -= that.OnDevServerStatusChanged;
			}
		};
	}

	private void OnDevServerStatusChanged(object? sender, RemoteControlStatus status)
		=> DispatcherQueue.TryEnqueue(() => Status = status);

	private void OnStatusChanged(RemoteControlStatus status)
	{
		var (kind, message) = status.GetSummary();
		((SolidColorBrush)Fill).Color = kind switch
		{
			Classification.Ok => Colors.Green,
			Classification.Info => Colors.Yellow,
			Classification.Warning => Colors.Orange,
			Classification.Error => Colors.Red,
			_ => Colors.Gray
		};
		HeadLine = message;
		ToolTipService.SetToolTip(this, message);
	}
}
