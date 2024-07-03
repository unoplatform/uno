#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Uno.UI.RemoteControl.HotReload.Messages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;

namespace Uno.UI.RemoteControl.HotReload;

internal enum EntrySource
{
	DevServer,
	Engine,
	Server,
	Application
}


internal record DevServerEntry() : HotReloadLogEntry(EntrySource.DevServer, -1, DateTimeOffset.Now);

internal record EngineEntry() : HotReloadLogEntry(EntrySource.Engine, -1, DateTimeOffset.Now);


internal record ServerEntry : HotReloadLogEntry
{
	public ServerEntry(HotReloadServerOperationData srvOp)
		: base(EntrySource.Server, srvOp.Id, srvOp.StartTime)
	{
		Update(srvOp);
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
}

internal record ApplicationEntry : HotReloadLogEntry
{
	public ApplicationEntry(HotReloadClientOperation localOp)
		: base(EntrySource.Application, localOp.Id, localOp.StartTime)
	{
		Update(localOp);
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
}

[Microsoft.UI.Xaml.Data.Bindable]
internal record HotReloadLogEntry(EntrySource Source, long Id, DateTimeOffset Timestamp) : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	public bool? IsSuccess { get; set; }
	public TimeSpan? Duration { get; set; }
	public string? Description { get; set; }

	// Quick patch as we don't have MVUX
	public string Title => $"{Timestamp.LocalDateTime:T} - {Source}{GetDuration()}".ToString(CultureInfo.CurrentCulture);

	public string GetSource()
		=> Source switch
		{
			EntrySource.DevServer => "Dev-Server",
			EntrySource.Engine => "Engine",
			EntrySource.Server => "IDE",
			EntrySource.Application => "Application",
			_ => "Unknown"
		};

	protected void RaiseChanged()
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));

	private string GetDuration()
		=> Duration switch
		{
			null => string.Empty,
			{ TotalMilliseconds: < 1000 } ms => $" - {ms.TotalMilliseconds:F0} ms",
			{ TotalSeconds: < 3 } s => $" - {s.TotalSeconds:N0} s",
			{ } d => $" - {d:g}"
		};

	protected static string Join(string[] items, string itemType)
		=> items switch
		{
			{ Length: 0 } => "",
			{ Length: 1 } => $" in {items[0]}",
			{ Length: <= 3 } => $" in {string.Join(",", items[..^1])} and {items[^1]}",
			_ => $" in {string.Join(",", items[..3])} and {items.Length - 3} other {itemType}"
		};
}
