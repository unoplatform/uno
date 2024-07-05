#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Diagnostics.UI;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;
using static Uno.UI.RemoteControl.RemoteControlStatus;

namespace Uno.UI.RemoteControl.HotReload;

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

	public string? HeadLine
	{
		get => (string?)GetValue(HeadLineProperty);
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
	private RemoteControlStatus? _devServerStatus;

	private readonly Dictionary<long, ServerEntry> _serverHrEntries = new();
	private readonly Dictionary<long, ApplicationEntry> _appHrEntries = new();

	public HotReloadStatusView(IDiagnosticViewContext ctx)
	{
		_ctx = ctx;
		DefaultStyleKey = typeof(HotReloadStatusView);
		History = [];

		UpdateNotificationStates();

		Loaded += static (snd, _) =>
		{
			// Make sure to hide the diagnostics overlay when the view is loaded (in case the template was applied while out of the visual tree).
			if (snd is HotReloadStatusView { XamlRoot: { } root } that)
			{
				DiagnosticsOverlay.Get(root).Hide(RemoteControlStatusView.Id);
				if (RemoteControlClient.Instance is { } devServer)
				{
					devServer.StatusChanged += that.OnDevServerStatusChanged;
					that.OnDevServerStatusChanged(null, devServer.Status);
				}
			}
		};
		Unloaded += static (snd, _) =>
		{
			if (snd is HotReloadStatusView that
				&& RemoteControlClient.Instance is { } devServer)
			{
				devServer.StatusChanged -= that.OnDevServerStatusChanged;
			}
		};
	}

	private void OnDevServerStatusChanged(object? sender, RemoteControlStatus devServerStatus)
	{
		var oldStatus = _devServerStatus;
		_devServerStatus = devServerStatus;

		UpdateLog(oldStatus, devServerStatus);

		UpdateNotificationStates();
		UpdateStatusVisualState();
	}

	public void OnHotReloadStatusChanged(Status status)
	{
		var oldStatus = _hotReloadStatus;
		_hotReloadStatus = status;

		UpdateLog(oldStatus, status);

		UpdateNotificationStates();
		UpdateStatusVisualState();
	}

	private void UpdateLog(RemoteControlStatus? oldStatus, RemoteControlStatus newStatus)
	{
		if (oldStatus is not null && oldStatus.State == newStatus.State)
		{
			return;
		}

		var notif = (oldStatus, newStatus) switch
		{
			(_, { State: ConnectionState.NoServer }) => "No endpoint found.",
			(_, { State: ConnectionState.Connecting }) => "Connecting...",
			({ State: not ConnectionState.ConnectionTimeout }, { State: ConnectionState.ConnectionTimeout }) => "Timeout.",
			({ State: not ConnectionState.ConnectionFailed }, { State: ConnectionState.ConnectionFailed }) => "Connection error.",

			({ IsVersionValid: not false }, { IsVersionValid: false }) => "Version mismatch",
			({ InvalidFrames.Count: 0 }, { InvalidFrames.Count: > 0 }) => "Unknown messages.",
			({ MissingRequiredProcessors.IsEmpty: true }, { MissingRequiredProcessors.IsEmpty: false }) => "Processors missing.",

			({ KeepAlive.State: KeepAliveState.Idle or KeepAliveState.Ok }, { KeepAlive.State: KeepAliveState.Late }) => "Connection lost (> 1000ms).",
			({ KeepAlive.State: KeepAliveState.Idle or KeepAliveState.Ok }, { KeepAlive.State: KeepAliveState.Lost }) => "Connection lost (> 1s).",
			({ KeepAlive.State: KeepAliveState.Idle or KeepAliveState.Ok }, { KeepAlive.State: KeepAliveState.Aborted }) => "Connection lost (keep-alive).",
			({ State: ConnectionState.Connected }, { State: ConnectionState.Disconnected }) => "Connection lost.",

			({ State: ConnectionState.Connected }, { State: ConnectionState.Reconnecting }) => "Connection lost (reconnecting).",

			_ => null
		};
		if (notif is not null)
		{
			Insert(History, new DevServerEntry { Description = notif });
		}
	}

	private void UpdateLog(Status? oldStatus, Status status)
	{
		// Add or update the entries for the **operations** (server and the application).
		foreach (var srvOp in status.Server.Operations)
		{
			ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_serverHrEntries, srvOp.Id, out bool exists);
			if (exists)
			{
				entry!.Update(srvOp);
			}
			else
			{
				entry = new ServerEntry(srvOp);
			}
		}

		foreach (var localOp in status.Local.Operations)
		{
			ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_appHrEntries, localOp.Id, out bool exists);
			if (exists)
			{
				entry!.Update(localOp);
			}
			else
			{
				entry = new ApplicationEntry(localOp);
			}
		}

		var log = History;
		SyncLog(log, _serverHrEntries.Values);
		SyncLog(log, _appHrEntries.Values);

		// Add a log entry for the **status** change.
		switch (oldStatus?.State ?? HotReloadState.Initializing, status.State)
		{
			case (< HotReloadState.Ready, HotReloadState.Ready):
				Insert(log, new EngineEntry { Description = "Connected." });
				break;

			case (not HotReloadState.Disabled, HotReloadState.Disabled):
				Insert(log, new EngineEntry { Description = "Cannot initialize." });
				break;
		}
	}

	public void UpdateNotificationStates()
	{
		var log = History;

		HeadLine = log
			.LastOrDefault(e => e.Source is EntrySource.Engine or EntrySource.DevServer)
			?.Description;

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
				if (notif.Content is null or HotReloadLogEntry)
				{
					notif.Content = history[0];
				}

				_ctx.Notify(notif);
			}
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

	#region Misc helpers
	private static void SyncLog<TEntry>(ObservableCollection<HotReloadLogEntry> history, ICollection<TEntry> entries)
		where TEntry : HotReloadLogEntry
	{
		foreach (var entry in entries)
		{
			if (entry.Title is null)
			{
				history.Remove(entry);
			}
			else if (!history.Contains(entry))
			{
				Insert(history, entry);
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
	#endregion
}
