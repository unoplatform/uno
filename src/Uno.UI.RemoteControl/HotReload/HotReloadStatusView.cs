#nullable enable

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Diagnostics.UI;
using Uno.UI.RemoteControl.HotReload.Messages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;
using static Uno.UI.RemoteControl.RemoteControlStatus;
using Path = System.IO.Path;

namespace Uno.UI.RemoteControl.HotReload;

[TemplatePart(Name = DevServerStatusPartName, Type = typeof(RemoteControlStatusView))]
[TemplateVisualState(GroupName = "Status", Name = StatusUnknownVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusErrorVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusInitializingVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusIdleVisualStateName)]
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
	private const string StatusIdleVisualStateName = "Idle";
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
		typeof(ObservableCollection<HotReloadEntryViewModel>),
		typeof(HotReloadStatusView),
		new PropertyMetadata(default(ObservableCollection<HotReloadEntryViewModel>)));

	public ObservableCollection<HotReloadEntryViewModel> History
	{
		get => (ObservableCollection<HotReloadEntryViewModel>)GetValue(HistoryProperty);
		private init => SetValue(HistoryProperty, value);
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
	private Classification _devServerState;

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

			if (devServerView.Parent is Panel devServerTouchTarget) // TODO: The RemoteControlStatusView should be a templatable control and do that by it own...
			{
				devServerTouchTarget.Tapped += (snd, _) => devServerView.ShowDetails();
			}

			if (XamlRoot is { } root)
			{
				DiagnosticsOverlay.Get(root).Hide(RemoteControlStatusView.Id);
			}
		}
	}

	private void OnDevServerStatusChanged(RemoteControlStatus devServerStatus)
	{
		_devServerState = devServerStatus.GetSummary().kind;

		UpdateStatusVisualState();
	}

	public void OnHotReloadStatusChanged(Status status)
	{
		_hotReloadStatus = status;

		UpdateHeadline(status);
		UpdateHistory(status);

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
			HotReloadState.Disabled => "Hot-reload is disabled.",
			HotReloadState.Initializing => "Hot-reload engine is initializing.",
			HotReloadState.Idle => "Hot-reload engine is ready and listening for file changes.",
			HotReloadState.Processing => "Hot-reload engine is processing file changes",
			_ => "Unable to determine the state of the hot-reload engine."
		};
	}

	private void UpdateHistory(Status status)
	{
		var history = History;
		var vms = history.ToDictionary(op => (IsServer: op.IsIDE, op.Id));

		foreach (var srvOp in status.Server.Operations)
		{
			if (vms.TryGetValue((true, srvOp.Id), out var vm))
			{
				vm.Update(srvOp);
			}
			else
			{
				history.Insert(FindIndex(srvOp.StartTime), new HotReloadEntryViewModel(srvOp));
			}
		}

		foreach (var localOp in status.Local.Operations)
		{
			if (vms.TryGetValue((false, localOp.Id), out var vm))
			{
				vm.Update(localOp);
			}
			else
			{
				history.Insert(FindIndex(localOp.StartTime), new HotReloadEntryViewModel(localOp));
			}
		}

		// Finally once we synced the history, we update the "result" visual state.
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
			switch (resultState)
			{
				case ResultSuccessVisualStateName when SuccessNotification is not null:
					_ctx.Notify(SuccessNotification);
					break;

				case ResultFailedVisualStateName when FailureNotification is not null:
					_ctx.Notify(FailureNotification);
					break;
			}
		}

		int FindIndex(DateTimeOffset date)
		{
			for (var i = 0; i < history.Count; i++)
			{
				if (history[i].Start > date)
				{
					return i;
				}
			}

			return 0;
		}
	}

	private void UpdateStatusVisualState()
	{
		var state = (_devServerStatus: _devServerState, _hotReloadStatus?.State) switch
		{
			(Classification.Error, _) => StatusErrorVisualStateName,
			(_, HotReloadState.Disabled) => StatusErrorVisualStateName,

			(_, HotReloadState.Initializing) => StatusInitializingVisualStateName,
			(Classification.Info, _) => StatusInitializingVisualStateName,

			(Classification.Warning, _) when !HasSuccessfulLocalOperation(_hotReloadStatus) => StatusUnknownVisualStateName, // e.g. invalid processors version

			(_, HotReloadState.Idle) => StatusIdleVisualStateName,

			(_, HotReloadState.Processing) => StatusProcessingVisualStateName,

			_ => StatusUnknownVisualStateName
		};

		VisualStateManager.GoToState(this, state, true);

		static bool HasSuccessfulLocalOperation(Status? status)
			=> status is not null
				&& status.Local.Operations.Any(op => op.Result is HotReloadClientResult.Success);
	}
}

[Windows.UI.Xaml.Data.Bindable]
internal sealed record HotReloadEntryViewModel(bool IsIDE, long Id, DateTimeOffset Start) : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	public bool? IsSuccess { get; set; }
	public TimeSpan? Duration { get; set; }
	public string? Description { get; set; }

	// Quick patch as we don't have MVUX
	public string Title => $"{Start.LocalDateTime:T} - {(IsIDE ? "IDE" : "Application")}{GetDuration()}".ToString(CultureInfo.CurrentCulture);

	public HotReloadEntryViewModel(HotReloadClientOperation localOp)
		: this(false, localOp.Id, localOp.StartTime)
	{
		Update(localOp);
	}

	public HotReloadEntryViewModel(HotReloadServerOperationData srvOp)
		: this(true, srvOp.Id, srvOp.StartTime)
	{
		Update(srvOp);
	}

	internal void Update(HotReloadClientOperation localOp)
	{
		var types = localOp.CuratedTypes;

		IsSuccess = localOp.Result switch
		{
			null => null,
			HotReloadClientResult.Success => true,
			_ => false
		};
		Description = localOp.Result switch
		{
			null => $"Processing changes{Join(types, "types")} (total of {localOp.Types.Length} types updated).",
			HotReloadClientResult.Success => $"Application received changes{Join(types, "types")} and updated the view (total of {localOp.Types.Length} types updated).",
			HotReloadClientResult.Failed => $"Application received changes{Join(types, "types")} (total of {localOp.Types.Length} types updated) but failed to update the view ({localOp.Exceptions.FirstOrDefault()?.Message}).",
			HotReloadClientResult.Ignored when localOp.Types is null or { Length: 0 } => $"Application received changes{Join(types, "types")} but view was not been updated because {localOp.IgnoreReason}.",
			HotReloadClientResult.Ignored => $"Application received changes{Join(types, "types")} (total of {localOp.Types.Length} types updated) but view was not been updated because {localOp.IgnoreReason}.",
			_ => $"Unknown application operation result: {localOp.Result}."
		};
		Duration = localOp.EndTime is not null ? localOp.EndTime - localOp.StartTime : null;

		RaiseChanged();
	}

	public void Update(HotReloadServerOperationData srvOp)
	{
		string[] files = srvOp.FilePaths.Select(Path.GetFileName).ToArray()!;

		IsSuccess = srvOp.Result switch
		{
			null => null,
			HotReloadServerResult.Success or HotReloadServerResult.NoChanges => true,
			_ => false
		};
		Description = srvOp.Result switch
		{
			null => $"Processing changes{Join(files, "files")}.",
			HotReloadServerResult.NoChanges => $"No changes detected by the server{Join(files, "files")}.",
			HotReloadServerResult.Success => $"IDE successfully detected and compiled changes{Join(files, "files")}.",
			HotReloadServerResult.RudeEdit => $"IDE detected changes{Join(files, "files")} but is not able to apply them.",
			HotReloadServerResult.Failed => $"IDE detected changes{Join(files, "files")} but is not able to compile them.",
			HotReloadServerResult.Aborted => $"Hot-reload has been cancelled (usually because some other changes has been detected).",
			HotReloadServerResult.InternalError => "Hot-reload failed for due to an internal error.",
			_ => $"Unknown IDE operation result: {srvOp.Result}."
		};
		Duration = srvOp.EndTime is not null ? srvOp.EndTime - srvOp.StartTime : null;

		RaiseChanged();
	}

	private void RaiseChanged()
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));

	private string GetDuration()
		=> Duration switch
		{
			null => string.Empty,
			{ TotalMilliseconds: < 1000 } ms => $" - {ms.TotalMilliseconds:F0} ms",
			{ TotalSeconds: < 3 } s => $" - {s.TotalSeconds:N0} s",
			{ } d => $" - {d:g}"
		};

	private static string Join(string[] items, string itemType)
		=> items switch
		{
			{ Length: 0 } => "",
			{ Length: 1 } => $" in {items[0]}",
			{ Length: <= 3 } => $" in {string.Join(",", items[..^1])} and {items[^1]}",
			_ => $" in {string.Join(",", items[..3])} and {items.Length - 3} other {itemType}"
		};
}
