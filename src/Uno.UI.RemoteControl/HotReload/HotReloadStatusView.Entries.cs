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
		(IsSuccess, Icon) = srvOp.Result switch
		{
			null => (default(bool?), EntryIcon.HotReload | EntryIcon.Loading),
			HotReloadServerResult.Success or HotReloadServerResult.NoChanges => (true, EntryIcon.HotReload | EntryIcon.Success),
			_ => (false, EntryIcon.HotReload | EntryIcon.Error)
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
		Description = Join("file", srvOp.FilePaths.Select(Path.GetFileName).ToArray()!);
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
		(IsSuccess, Icon) = localOp.Result switch
		{
			null => (default(bool?), EntryIcon.HotReload | EntryIcon.Loading),
			HotReloadClientResult.Success => (true, EntryIcon.HotReload | EntryIcon.Success),
			_ => (false, EntryIcon.HotReload | EntryIcon.Error)
		};
		Title = localOp.Result switch
		{
			null => "Processing...",
			HotReloadClientResult.Success => "Update successful.",
			HotReloadClientResult.Failed => "An error occured.",
			_ => null
		};
		Description = Join("type", localOp.CuratedTypes, localOp.Types.Length);
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
	public EntryIcon Icon { get; set; }
	public string? Title { get; set; }
	public string? Description { get; set; }

	public string TimeInfo => Duration switch
	{
		null => $"{Timestamp:T}",
		{ TotalMilliseconds: < 1000 } ms => $"{ms.TotalMilliseconds:F0} ms - {Timestamp:T}",
		{ } s => $"{s.TotalSeconds:N0} s - {Timestamp:T}",
	};

	protected void RaiseChanged()
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));

	protected static string Join(string kind, string[] items, int? total = null, int max = 5)
	{
		const int maxLength = 70;

		var sb = new StringBuilder(maxLength + 12 /* and xx more*/);
		int count;
		for (count = 0; count < Math.Min(items.Length, max); count++)
		{
			var item = items[count];
			if (sb.Length + 2 /*, */ + item.Length < maxLength)
			{
				if (count is not 0) sb.Append(", ");
				sb.Append(item);
			}
			else
			{
				break;
			}
		}

		var remaining = total - count;
		if (remaining > 0)
		{
			sb.Append((count, remaining) switch
			{
				(0, 1) => $"1 {kind}",
				(0, _) => $"{remaining} {kind}s",
				_ => $" and {remaining} more"
			});
		}

		return sb.ToString();
	}
}
