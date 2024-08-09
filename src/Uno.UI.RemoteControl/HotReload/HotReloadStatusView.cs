#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Diagnostics.UI;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;

namespace Uno.UI.RemoteControl.HotReload;

[TemplateVisualState(GroupName = "Status", Name = StatusUnknownVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusInitializingVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusReadyVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusWarningVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusErrorVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultNoneVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultSuccessVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultFailedVisualStateName)]
internal sealed partial class HotReloadStatusView : Control
{
	private const string StatusUnknownVisualStateName = "Unknown";
	private const string StatusInitializingVisualStateName = "Initializing";
	private const string StatusReadyVisualStateName = "Ready";
	private const string StatusErrorVisualStateName = "Error";
	private const string StatusWarningVisualStateName = "Warning";

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
	private (string state, HotReloadLogEntry? entry) _result = (ResultNoneVisualStateName, null);

	private Status? _hotReloadStatus;
	private RemoteControlStatus? _devServerStatus;

	private readonly Dictionary<long, ServerEntry> _serverHrEntries = new();
	private readonly Dictionary<long, ApplicationEntry> _appHrEntries = new();

	public HotReloadStatusView(IDiagnosticViewContext ctx)
	{
		_ctx = ctx;
		DefaultStyleKey = typeof(HotReloadStatusView);
		History = [];

		UpdateVisualStates(false);

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

		DispatcherQueue.TryEnqueue(() =>
		{
			UpdateLog(oldStatus, devServerStatus);

			UpdateVisualStates();
		});
	}

	public void OnHotReloadStatusChanged(Status status)
	{
		var oldStatus = _hotReloadStatus;
		_hotReloadStatus = status;

		UpdateLog(oldStatus, status);

		UpdateVisualStates();
	}

	private void UpdateLog(RemoteControlStatus? oldStatus, RemoteControlStatus newStatus)
	{
		if (DevServerEntry.TryCreateNew(oldStatus, newStatus) is { } entry)
		{
			Insert(History, entry);
		}
	}

	private void UpdateLog(Status? oldStatus, Status status)
	{
		// Add or update the entries for the **operations** (server and the application).
		foreach (var srvOp in status.Server.Operations)
		{
			ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_serverHrEntries, srvOp.Id, out var exists);
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
			ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_appHrEntries, localOp.Id, out var exists);
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
		if (EngineEntry.TryCreateNew(oldStatus, status) is { } engineEntry)
		{
			Insert(log, engineEntry);
		}
	}

	public void UpdateVisualStates(bool useTransitions = true)
	{
		var log = History;

		var connectionEntry = log.FirstOrDefault(e => e.Source is EntrySource.Engine or EntrySource.DevServer);
		var operationEntries = log.Where(entry => entry.Source is EntrySource.Server or EntrySource.Application).ToList();

		// Update the "status"(a.k.a. "connection state") visual state.
		if (connectionEntry is null)
		{
			HeadLine = null;
			VisualStateManager.GoToState(this, StatusUnknownVisualStateName, useTransitions);
		}
		else
		{
			HeadLine = connectionEntry.Description;
			var state = (connectionEntry.Icon & ~(EntryIcon.HotReload | EntryIcon.Connection)) switch
			{
				EntryIcon.Loading => StatusInitializingVisualStateName,
				EntryIcon.Success => StatusReadyVisualStateName,
				EntryIcon.Warning when operationEntries.Any(op => op.IsSuccess ?? false) => StatusReadyVisualStateName,
				EntryIcon.Warning => StatusWarningVisualStateName,
				EntryIcon.Error => StatusErrorVisualStateName,
				_ => StatusUnknownVisualStateName
			};
			VisualStateManager.GoToState(this, state, useTransitions);
		}

		// Then the "result" visual state (en send notifications).
		var result = operationEntries switch
		{
			{ Count: 0 } => (ResultNoneVisualStateName, default),
			_ when operationEntries.Any(op => op.IsSuccess is null) => (ResultNoneVisualStateName, default),
			[ServerEntry { IsFinal: true, IsSuccess: true } e, ..] => (ResultSuccessVisualStateName, e),
			[ServerEntry { IsFinal: true, IsSuccess: false } e, ..] => (ResultFailedVisualStateName, e),
			[ApplicationEntry { IsSuccess: true } e, ..] => (ResultSuccessVisualStateName, e),
			[ApplicationEntry { IsSuccess: false } e, ..] => (ResultFailedVisualStateName, e),
			_ => (ResultNoneVisualStateName, default(HotReloadLogEntry))
		};
		if (result != _result)
		{
			_result = result;
			VisualStateManager.GoToState(this, _result.state, useTransitions);

			var notif = _result.state switch
			{
				ResultNoneVisualStateName when operationEntries is { Count: > 0 } => ProcessingNotification,
				ResultSuccessVisualStateName => SuccessNotification,
				ResultFailedVisualStateName => FailureNotification,
				_ => default
			};
			if (notif is not null)
			{
				if (notif.Content is null or HotReloadLogEntry)
				{
					notif.Content = operationEntries[0];
				}

				_ctx.Notify(notif);
			}
		}
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
