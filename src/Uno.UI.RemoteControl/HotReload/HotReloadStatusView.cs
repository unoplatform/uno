#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Diagnostics.UI;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;
using static Uno.UI.RemoteControl.RemoteControlStatus;

namespace Uno.UI.RemoteControl.HotReload;

[TemplatePart(Name = DevServerStatusPartName, Type = typeof(RemoteControlStatusView))]
[TemplateVisualState(GroupName = "Status", Name = StatusUnknownVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusErrorVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusInitializingVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusReadyVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusProcessingVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultNoneVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultSuccessVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultFailedVisualStateName)]
internal sealed partial class HotReloadStatusView : Control
{
	private const string DevServerStatusPartName = "PART_DevServerStatus";

	private const string StatusUnknownVisualStateName = "Unknown";
	private const string StatusErrorVisualStateName = "Error";
	private const string StatusInitializingVisualStateName = "Initializing";
	private const string StatusReadyVisualStateName = "Ready";
	private const string StatusProcessingVisualStateName = "Processing";

	private const string ResultNoneVisualStateName = "None";
	private const string ResultSuccessVisualStateName = "Success";
	private const string ResultFailedVisualStateName = "Failed";

	#region HeadLine (DP)
	public static DependencyProperty HeadLineProperty { get; } = DependencyProperty.Register(
		nameof(HeadLine),
		typeof(string),
		typeof(HotReloadStatusView),
		new PropertyMetadata(default(string), (snd, args) => ToolTipService.SetToolTip(snd, args.NewValue?.ToString())));

	public string HeadLine
	{
		get => (string)GetValue(HeadLineProperty);
		private set => SetValue(HeadLineProperty, value);
	}
	#endregion

	#region History (DP)
	public static DependencyProperty HistoryProperty { get; } = DependencyProperty.Register(
		nameof(History),
		typeof(ObservableCollection<HotReloadLogEntry>),
		typeof(HotReloadStatusView),
		new PropertyMetadata(default(ObservableCollection<HotReloadLogEntry>)));

	public ObservableCollection<HotReloadLogEntry> History
	{
		get => (ObservableCollection<HotReloadLogEntry>)GetValue(HistoryProperty);
		private init => SetValue(HistoryProperty, value);
	}
	#endregion

	#region ProcessingNotification (DP)
	public static readonly DependencyProperty ProcessingNotificationProperty = DependencyProperty.Register(
		nameof(ProcessingNotification),
		typeof(DiagnosticViewNotification),
		typeof(HotReloadStatusView),
		new PropertyMetadata(default(DiagnosticViewNotification?)));

	public DiagnosticViewNotification? ProcessingNotification
	{
		get => (DiagnosticViewNotification?)GetValue(ProcessingNotificationProperty);
		set => SetValue(ProcessingNotificationProperty, value);
	}
	#endregion

	#region SuccessNotification (DP)
	public static readonly DependencyProperty SuccessNotificationProperty = DependencyProperty.Register(
		nameof(SuccessNotification),
		typeof(DiagnosticViewNotification),
		typeof(HotReloadStatusView),
		new PropertyMetadata(default(DiagnosticViewNotification?)));

	public DiagnosticViewNotification? SuccessNotification
	{
		get => (DiagnosticViewNotification?)GetValue(SuccessNotificationProperty);
		set => SetValue(SuccessNotificationProperty, value);
	}
	#endregion

	#region FailureNotification (DP)
	public static readonly DependencyProperty FailureNotificationProperty = DependencyProperty.Register(
		nameof(FailureNotification),
		typeof(DiagnosticViewNotification),
		typeof(HotReloadStatusView),
		new PropertyMetadata(default(DiagnosticViewNotification?)));

	public DiagnosticViewNotification? FailureNotification
	{
		get => (DiagnosticViewNotification?)GetValue(FailureNotificationProperty);
		set => SetValue(FailureNotificationProperty, value);
	}
	#endregion

	private readonly IDiagnosticViewContext _ctx;
	private string _resultState = ResultNoneVisualStateName;

	private Status? _hotReloadStatus;
	private (RemoteControlStatusView view, long token)? _devServer;
	private RemoteControlStatus? _devServerStatus;

	public HotReloadStatusView(IDiagnosticViewContext ctx)
	{
		_ctx = ctx;
		DefaultStyleKey = typeof(HotReloadStatusView);
		History = [];

		UpdateHeadline(null);

		Loaded += static (snd, _) =>
		{
			// Make sure to hide the diagnostics overlay when the view is loaded (in case the template was applied while out of the visual tree).
			if (snd is HotReloadStatusView { _devServer: not null, XamlRoot: { } root })
			{
				DiagnosticsOverlay.Get(root).Hide(RemoteControlStatusView.Id);
			}
		};
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		if (_devServer is { } devServer)
		{
			devServer.view.UnregisterPropertyChangedCallback(RemoteControlStatusView.StatusProperty, devServer.token);
		}

		base.OnApplyTemplate();

		if (GetTemplateChild(DevServerStatusPartName) is RemoteControlStatusView { HasServer: true } devServerView)
		{
			var token = devServerView.RegisterPropertyChangedCallback(
				RemoteControlStatusView.StatusProperty,
				(snd, _) => OnDevServerStatusChanged(((RemoteControlStatusView)snd).Status));
			_devServer = (devServerView, token);

			//if (devServerView.Parent is Panel devServerTouchTarget) // TODO: The RemoteControlStatusView should be a templatable control and do that by it own...
			//{
			//	devServerTouchTarget.Tapped += (snd, _) => devServerView.ShowDetails();
			//}

			if (XamlRoot is { } root)
			{
				DiagnosticsOverlay.Get(root).Hide(RemoteControlStatusView.Id);
			}
		}
	}

	private void OnDevServerStatusChanged(RemoteControlStatus devServerStatus)
	{
		var oldStatus = _devServerStatus;
		_devServerStatus = devServerStatus;

		UpdateLog(oldStatus, devServerStatus);

		UpdateStatusVisualState();
	}

	public void OnHotReloadStatusChanged(Status status)
	{
		var oldStatus = _hotReloadStatus;
		_hotReloadStatus = status;

		UpdateHeadline(status);
		UpdateLog(oldStatus, status);

		UpdateStatusVisualState();
	}

	public void UpdateHeadline(Status? status)
	{
		HeadLine = status?.State switch
		{
			null => """
					State of the hot-reload engine is unknown.
					This usually indicates that connection to the IDE failed, but if running within VisualStudio, updates might still be detected.
					""",
			HotReloadState.Disabled => "Hot-reload engine failed to initialize.",
			HotReloadState.Initializing => "Hot-reload engine is initializing.",
			HotReloadState.Ready => "Hot-reload engine is ready and listening for file changes.",
			HotReloadState.Processing => "Hot-reload engine is processing file changes",
			_ => "Unable to determine the state of the hot-reload engine."
		};
	}

	private void UpdateLog(RemoteControlStatus? oldStatus, RemoteControlStatus newStatus)
	{
		if (oldStatus is not null && oldStatus.State == newStatus.State)
		{
			return;
		}

		if (newStatus.GetSummary() is { kind: Classification.Warning or Classification.Error } summary)
		{
			Insert(History, new DevServerEntry { Description = summary.message });
		}
	}

	private void UpdateLog(Status? oldStatus, Status status)
	{
		var log = History;

		var serverEntries = log.OfType<ServerEntry>().ToDictionary(entry => entry.Id);
		foreach (var srvOp in status.Server.Operations)
		{
			if (serverEntries.TryGetValue(srvOp.Id, out var entry))
			{
				entry.Update(srvOp);
			}
			else
			{
				Insert(log, new ServerEntry(srvOp));
			}
		}

		var appEntries = log.OfType<ApplicationEntry>().ToDictionary(entry => entry.Id);
		foreach (var localOp in status.Local.Operations)
		{
			if (appEntries.TryGetValue(localOp.Id, out var entry))
			{
				entry.Update(localOp);
			}
			else
			{
				Insert(log, new ApplicationEntry(localOp));
			}
		}

		if (status.State is HotReloadState.Ready && (oldStatus is null || oldStatus.State != HotReloadState.Ready))
		{
			Insert(log, new EngineEntry { Description = "" });
		}

		// Finally once we synced the history, we update the "result" visual state.
		var history = log
			.Where(entry => entry.Source is EntrySource.Server or EntrySource.Application)
			.ToList();
		var resultState = history switch
		{
			{ Count: 0 } => ResultNoneVisualStateName,
			_ when history.Any(op => op.IsSuccess is null) => ResultNoneVisualStateName, // Makes sure to restore to None while processing!
			[{ IsSuccess: true }, ..] => ResultSuccessVisualStateName,
			_ => ResultFailedVisualStateName
		};
		if (resultState != _resultState)
		{
			_resultState = resultState;
			VisualStateManager.GoToState(this, resultState, true);

			var notif = resultState switch
			{
				ResultNoneVisualStateName when history is { Count: > 0 } => ProcessingNotification,
				ResultSuccessVisualStateName => SuccessNotification,
				ResultFailedVisualStateName => FailureNotification,
				_ => default
			};
			if (notif is not null)
			{
				if (notif.Content is null)
				{
					notif.Content = history[0];
				}
				_ctx.Notify(notif);
			}
		}
	}

	private static void Insert(ObservableCollection<HotReloadLogEntry> history, HotReloadLogEntry entry)
	{
		history.Insert(FindIndex(entry.Timestamp), entry);

		int FindIndex(DateTimeOffset date)
		{
			for (var i = 0; i < history.Count; i++)
			{
				if (history[i].Timestamp > date)
				{
					return i;
				}
			}

			return 0;
		}
	}

	private void UpdateStatusVisualState()
	{
		var state = (_devServerStatus?.GetSummary().kind ?? Classification.Ok, _hotReloadStatus?.State) switch
		{
			(Classification.Error, _) => StatusErrorVisualStateName,
			(_, HotReloadState.Disabled) => StatusErrorVisualStateName,

			(_, HotReloadState.Initializing) => StatusInitializingVisualStateName,
			(Classification.Info, _) => StatusInitializingVisualStateName,

			(Classification.Warning, _) when !HasSuccessfulLocalOperation(_hotReloadStatus) => StatusUnknownVisualStateName, // e.g. invalid processors version

			(_, HotReloadState.Ready) => StatusReadyVisualStateName,

			(_, HotReloadState.Processing) => StatusProcessingVisualStateName,

			_ => StatusUnknownVisualStateName
		};

		VisualStateManager.GoToState(this, state, true);

		static bool HasSuccessfulLocalOperation(Status? status)
			=> status is not null
				&& status.Local.Operations.Any(op => op.Result is HotReloadClientResult.Success);
	}
}
