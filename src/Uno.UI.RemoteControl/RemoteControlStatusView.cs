using System;
using System.Linq;
using System.Threading;
using Windows.UI;
using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static Uno.UI.RemoteControl.RemoteControlStatus;

namespace Uno.UI.RemoteControl;

internal sealed partial class RemoteControlStatusView : Ellipse
{
#if __ANDROID__
	public new const string Id = nameof(RemoteControlStatusView);
#else
	public const string Id = nameof(RemoteControlStatusView);
#endif

	private static readonly Color _gray = Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A);
	private static readonly Color _green = Color.FromArgb(0xFF, 0x09, 0xB5, 0x09);
	private static readonly Color _yellow = Color.FromArgb(0xFF, 0xFC, 0xDF, 0x49);
	private static readonly Color _orange = Color.FromArgb(0xFF, 0xFD, 0x9E, 0x0F);
	private static readonly Color _red = Color.FromArgb(0xFF, 0xF3, 0x00, 0x00);

	private readonly RemoteControlClient? _devServer;
	private CancellationTokenSource? _details;

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
			Classification.Ok => _green,
			Classification.Info => _yellow,
			Classification.Warning => _orange,
			Classification.Error => _red,
			_ => _gray
		};
		HeadLine = message;
		ToolTipService.SetToolTip(this, message);
	}

	internal void ShowDetails()
	{
		if (_devServer is null)
		{
			return;
		}

		_details?.Cancel();
		_details = new CancellationTokenSource();

		var dialog = new ContentDialog
		{
			XamlRoot = XamlRoot,
			Title = "Hot reload",
			Content = _devServer.Status.GetDescription(),
			CloseButtonText = "Close",
		};
		_ = dialog.ShowAsync().AsTask(_details.Token);
	}
}
