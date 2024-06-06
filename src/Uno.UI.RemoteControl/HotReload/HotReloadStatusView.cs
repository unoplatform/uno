using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RemoteControl.HotReload.Messages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;

namespace Uno.UI.RemoteControl.HotReload;

internal sealed partial class HotReloadStatusView : Control
{
	#region HeadLine (DP)
	public static DependencyProperty HeadLineProperty { get; } = DependencyProperty.Register(
		nameof(HeadLine),
		typeof(string),
		typeof(HotReloadStatusView),
		new PropertyMetadata(default(string), (snd, args) => ToolTipService.SetToolTip(snd, args.NewValue?.ToString())));

	public string HeadLine
	{
		get => (string)GetValue(HeadLineProperty);
		set => SetValue(HeadLineProperty, value);
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

	public HotReloadStatusView()
	{
		DefaultStyleKey = typeof(HotReloadStatusView);
		History = [];

		UpdateHeadline(null);
	}

	public void Update(Status status)
	{
		SyncOperations(status);
		UpdateHeadline(status.State);

		VisualStateManager.GoToState(this, GetStatusVisualState(status.State), true);
		VisualStateManager.GoToState(this, GetResultVisualState(), true);
	}

	private void SyncOperations(Status status)
	{
		var operations = History;
		var vms = operations.ToDictionary(op => (op.IsServer, op.Id));

		foreach (var srvOp in status.Server.Operations)
		{
			if (!vms.TryGetValue((true, srvOp.Id), out var vm))
			{
				vm = new HotReloadEntryViewModel(true, srvOp.Id, srvOp.StartTime);
				operations.Insert(FindIndex(srvOp.StartTime), vm);
			}

			string[] files = srvOp.FilePaths.Select(Path.GetFileName).ToArray()!;

			vm.IsCompleted = srvOp.Result is not null;
			vm.IsSuccess = srvOp.Result is HotReloadServerResult.Success or HotReloadServerResult.NoChanges;
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
				operations.Insert(FindIndex(localOp.StartTime), vm);
			}

			var types = localOp.CuratedTypes;

			vm.IsCompleted = localOp.Result is not null;
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

		string Join(string[] items, string itemType, int maxItems = 5)
			=> items switch
			{
				{ Length: 0 } => "",
				{ Length: 1 } => $" in {items[0]}",
				{ Length: < 3 } => $" in {string.Join(",", items[..^1])} and {items[^1]}",
				_ => $" in {string.Join(",", items[..3])} and {items.Length - 3} other {itemType}"
			};

		int FindIndex(DateTimeOffset date)
		{
			for (var i = 0; i < operations.Count; i++)
			{
				if (operations[i].Start > date)
				{
					return i - 1;
				}
			}

			return 0;
		}
	}

	public string UpdateHeadline(HotReloadState? state)
		=> HeadLine = state switch
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

	private static string GetStatusVisualState(HotReloadState state)
		=> state switch
		{
			HotReloadState.Disabled => "Disabled",
			HotReloadState.Initializing => "Initializing",
			HotReloadState.Idle => "Idle",
			HotReloadState.Processing => "Processing",
			_ => "Unknown"
		};

	private string GetResultVisualState()
	{
		var operations = History;
		if (operations is { Count: 0 } || operations.Any(op => !op.IsCompleted))
		{
			return "None"; // Makes sure to restore to previous None!
		}

		return operations[0].IsSuccess ? "Success" : "Failed";
	}
}

[Microsoft.UI.Xaml.Data.Bindable]
internal sealed record HotReloadEntryViewModel(bool IsServer, long Id, DateTimeOffset Start) : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	public bool IsCompleted { get; set; }
	public bool IsSuccess { get; set; }
	public TimeSpan? Duration { get; set; }
	public string? Description { get; set; }

	// Quick patch as we don't have MVUX
	public string Title => $"{Start.LocalDateTime:T} - {(IsServer ? "Server" : "Application")}{GetDuration()}".ToString(CultureInfo.CurrentCulture);
	public Color Color => (IsCompleted, IsSuccess) switch
	{
		(false, _) => Colors.Yellow,
		(true, false) => Colors.Red,
		(true, true) => Colors.Green,
	};

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
