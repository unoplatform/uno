#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Uno.UI.RemoteControl.HotReload.Messages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;

namespace Uno.UI.RemoteControl.HotReload;

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
		IsSuccess = srvOp.Result switch
		{
			null => null,
			HotReloadServerResult.Success or HotReloadServerResult.NoChanges => true,
			_ => false
		};
		Title = srvOp.Result switch
		{
			HotReloadServerResult.NoChanges => "No changes detected.",
			HotReloadServerResult.RudeEdit => "Rude edit detected, restart required.",
			HotReloadServerResult.Failed => "Compilation errors.",
			HotReloadServerResult.Aborted => "Operation cancelled.",
			HotReloadServerResult.InternalError => "An error occured.",
			_ => null
		};
		Description = Join(srvOp.FilePaths.Select(Path.GetFileName).ToArray()!);
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
		IsSuccess = localOp.Result switch
		{
			null => null,
			HotReloadClientResult.Success => true,
			_ => false
		};
		Title = localOp.Result switch
		{
			null => "Processing...",
			HotReloadClientResult.Success => "Update successful.",
			HotReloadClientResult.Failed => "An error occured.",
			_ => null
		};
		Description = Join(localOp.CuratedTypes, localOp.Types.Length);
		Duration = localOp.EndTime is not null ? localOp.EndTime - localOp.StartTime : null;

		RaiseChanged();
	}
}

public enum EntrySource
{
	DevServer,
	Engine,
	Server,
	Application
}

[Flags]
public enum EntryIcon
{
	// Kind
	Loading = 0x1,
	Success = 0x2,
	Warning = 0x3,
	Error = 0x4,

	// Source
	Connection = 0x1 << 8,
	HotReload = 0x2 << 8,
}


[Microsoft.UI.Xaml.Data.Bindable]
internal record HotReloadLogEntry(EntrySource Source, long Id, DateTimeOffset Timestamp) : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	public bool? IsSuccess { get; set; }
	public TimeSpan? Duration { get; set; }

	public string? Title { get; set; }
	public string? Description { get; set; }

	public string TimeInfo => Duration is null
		? $"{Timestamp.LocalDateTime:HH:mm:ss}"
		: $"{GetDuration()} - {Timestamp.LocalDateTime:HH:mm:ss}";

	public EntryIcon Icon => (Source, IsSuccess) switch
	{
		(EntrySource.DevServer or EntrySource.Engine, null) => EntryIcon.Connection | EntryIcon.Warning, // Screen orange indicator
		(EntrySource.DevServer or EntrySource.Engine, true) => EntryIcon.Connection | EntryIcon.Success, // Screen green indicator
		(EntrySource.DevServer or EntrySource.Engine, false) => EntryIcon.Connection | EntryIcon.Error, // Screen red indicator

		// EntrySource.Application or EntrySource.Server
		(_, null) => EntryIcon.HotReload | EntryIcon.Loading, // Loading wheel
		(_, true) => EntryIcon.HotReload | EntryIcon.Success, // Fire with green indicator
		(_, false) => EntryIcon.HotReload | EntryIcon.Loading, // Fir with red indicator
	};

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
			{ } s => $" - {s.TotalSeconds:N0} s"
		};

	protected static string Join(string[] items, int? total = null, int max = 5)
	{
		const int maxLength = 70;

		var sb = new StringBuilder(maxLength + 12 /* and xx more*/);
		int i;
		for (i = 0; i < Math.Min(items.Length, max); i++)
		{
			var item = items[i];
			if (sb.Length + 2 /*, */ + item.Length < maxLength)
			{
				if (i is not 0) sb.Append(", ");
				sb.Append(item);
			}
			else
			{
				break;
			}
		}

		var remaining = total - i;
		if (remaining > 0)
		{
			sb.Append(" and ");
			sb.Append(remaining);
			sb.Append(" more");
		}

		return sb.ToString();
	}
}
