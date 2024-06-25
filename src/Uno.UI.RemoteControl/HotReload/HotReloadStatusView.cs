#nullable enable

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Data;
using Uno.Diagnostics.UI;
using Uno.UI.RemoteControl.HotReload.Messages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;
using Path = System.IO.Path;

namespace Uno.UI.RemoteControl.HotReload;

[TemplateVisualState(GroupName = "Status", Name = StatusUnknownVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusErrorVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusInitializingVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusIdleVisualStateName)]
[TemplateVisualState(GroupName = "Status", Name = StatusProcessingVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultNoneVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultSuccessVisualStateName)]
[TemplateVisualState(GroupName = "Result", Name = ResultFailedVisualStateName)]
public sealed partial class HotReloadStatusView : Control
{
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

	internal ObservableCollection<HotReloadEntryViewModel> History
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
	private string _resultState = "None";

	public HotReloadStatusView(IDiagnosticViewContext ctx)
	{
		_ctx = ctx;
		DefaultStyleKey = typeof(HotReloadStatusView);
		History = [];

		UpdateHeadline(null);

		Loaded += OnLoaded;
	}

	private static void OnLoaded(object sender, RoutedEventArgs e)
	{
		// A bit hackish, but for now when the hot-reload indicator is present, we request to dev-server indicator to show only if the state is important.
		if (sender is HotReloadStatusView { XamlRoot: { } root }
			&& DiagnosticsOverlay.Get(root).Find(nameof(RemoteControlStatusView)) is RemoteControlStatusView devServerView)
		{
			devServerView.IsAutoHideEnabled = true;
		}
	}

	public void Update(Status status)
	{
		UpdateHistory(status);
		UpdateHeadline(status.State);

		VisualStateManager.GoToState(this, GetStatusVisualState(status.State), true);

		var resultState = GetResultVisualState();
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
	}

	private void UpdateHistory(Status status)
	{
		var history = History;
		var vms = history.ToDictionary(op => (op.IsServer, op.Id));

		foreach (var srvOp in status.Server.Operations)
		{
			if (!vms.TryGetValue((true, srvOp.Id), out var vm))
			{
				vm = new HotReloadEntryViewModel(true, srvOp.Id, srvOp.StartTime);
				history.Insert(FindIndex(srvOp.StartTime), vm);
			}

			string[] files = srvOp.FilePaths.Select(Path.GetFileName).ToArray()!;

			vm.IsSuccess = srvOp.Result switch
			{
				null => null,
				HotReloadServerResult.Success or HotReloadServerResult.NoChanges => true,
				_ => false
			};

			vm.Description = srvOp.Result switch
			{
				null => $"Processing changes{Join(files, "files")}.",
				HotReloadServerResult.NoChanges => $"No changes detected by the server{Join(files, "files")}.",
				HotReloadServerResult.Success => $"Server successfully detected and compiled changes{Join(files, "files")}.",
				HotReloadServerResult.RudeEdit => $"Server detected changes{Join(files, "files")} but is not able to apply them.",
				HotReloadServerResult.Failed => $"Server detected changes{Join(files, "files")} but is not able to compile them.",
				HotReloadServerResult.Aborted => $"Hot-reload has been aborted (usually because some other changes has been detected).",
				HotReloadServerResult.InternalError => "Hot-reload failed for due to an internal error.",
				_ => $"Unknown server operation result: {srvOp.Result}."
			};
			vm.Duration = srvOp.EndTime is not null ? srvOp.EndTime - srvOp.StartTime : null;
			vm.RaiseChanged();
		}

		foreach (var localOp in status.Local.Operations)
		{
			if (!vms.TryGetValue((false, localOp.Id), out var vm))
			{
				vm = new HotReloadEntryViewModel(false, localOp.Id, localOp.StartTime);
				history.Insert(FindIndex(localOp.StartTime), vm);
			}

			var types = localOp.CuratedTypes;

			vm.IsSuccess = localOp.Result is HotReloadClientResult.Success;
			vm.Description = localOp.Result switch
			{
				null => $"Processing changes{Join(types, "types")} (total of {localOp.Types.Length} types updated).",
				HotReloadClientResult.Success => $"Application received changes{Join(types, "types")} and updated the view (total of {localOp.Types.Length} types updated).",
				HotReloadClientResult.Failed => $"Application received changes{Join(types, "types")} (total of {localOp.Types.Length} types updated) but failed to update the view ({localOp.Exceptions.FirstOrDefault()?.Message}).",
				HotReloadClientResult.Ignored => $"Application received changes{Join(types, "types")} (total of {localOp.Types.Length} types updated) but view was not been updated because {localOp.IgnoreReason}.",
				_ => $"Unknown application operation result: {localOp.Result}."
			};
			vm.Duration = localOp.EndTime is not null ? localOp.EndTime - localOp.StartTime : null;
			vm.RaiseChanged();
		}

		static string Join(string[] items, string itemType, int maxItems = 5)
			=> items switch
			{
				{ Length: 0 } => "",
				{ Length: 1 } => $" in {items[0]}",
				{ Length: < 3 } => $" in {string.Join(",", items[..^1])} and {items[^1]}",
				_ => $" in {string.Join(",", items[..3])} and {items.Length - 3} other {itemType}"
			};

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

	private void UpdateHeadline(HotReloadState? state)
	{
		HeadLine = state switch
		{
			null => """
					State of the hot-reload engine is unknown.
					This usually indicates that connection to the dev-server failed, but if running within VisualStudio, updates might still be detected.
					""",
			HotReloadState.Disabled => "Hot-reload server is disabled.",
			HotReloadState.Initializing => "Hot-reload engine is initializing.",
			HotReloadState.Idle => "Hot-reload server is ready and listening for file changes.",
			HotReloadState.Processing => "Hot-reload engine is processing file changes",
			_ => "Unable to determine the state of the hot-reload engine."
		};
	}

	private static string GetStatusVisualState(HotReloadState state)
		=> state switch
		{
			HotReloadState.Disabled => StatusErrorVisualStateName,
			HotReloadState.Initializing => StatusInitializingVisualStateName,
			HotReloadState.Idle => StatusIdleVisualStateName,
			HotReloadState.Processing => StatusProcessingVisualStateName,
			_ => StatusUnknownVisualStateName
		};

	private string GetResultVisualState()
	{
		var operations = History;
		if (operations is { Count: 0 } || operations.Any(op => op.IsSuccess is null))
		{
			return ResultNoneVisualStateName; // Makes sure to restore to previous None!
		}

		return operations[0].IsSuccess switch
		{
			true => ResultSuccessVisualStateName,
			false => ResultFailedVisualStateName,
			_ => ResultNoneVisualStateName
		};
	}
}

#nullable disable
internal sealed class BoolToObjectConverter : IValueConverter
{
	public object TrueValue { get; set; }

	public object FalseValue { get; set; }

	public object NullValue { get; set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value == null)
		{
			return NullValue;
		}

		return (bool)value ? TrueValue : FalseValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
}
#nullable enable

[Microsoft.UI.Xaml.Data.Bindable]
internal sealed record HotReloadEntryViewModel(bool IsServer, long Id, DateTimeOffset Start) : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	public bool? IsSuccess { get; set; }
	public TimeSpan? Duration { get; set; }
	public string? Description { get; set; }

	// Quick patch as we don't have MVUX
	public string Title => $"{Start.LocalDateTime:T} - {(IsServer ? "Server" : "Application")}{GetDuration()}".ToString(CultureInfo.CurrentCulture);

	public void RaiseChanged()
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));

	private string GetDuration()
		=> Duration switch
		{
			null => string.Empty,
			{ TotalMilliseconds: < 1000 } ms => $" - {ms.TotalMilliseconds:F0} ms",
			{ TotalSeconds: < 3 } s => $" - {s.TotalSeconds:N2} s",
			{ } d => $" - {d:g}"
		};
}
