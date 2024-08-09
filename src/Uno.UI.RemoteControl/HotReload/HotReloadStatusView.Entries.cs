#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Uno.UI.RemoteControl.HotReload.Messages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;
using static Uno.UI.RemoteControl.RemoteControlStatus;

namespace Uno.UI.RemoteControl.HotReload;

internal record DevServerEntry() : HotReloadLogEntry(EntrySource.DevServer, -1, DateTimeOffset.Now)
{
	public static DevServerEntry? TryCreateNew(RemoteControlStatus? oldStatus, RemoteControlStatus newStatus)
	{
		if (oldStatus is not null && oldStatus.State == newStatus.State)
		{
			return null;
		}

		var (iconState, desc) = (oldStatus, newStatus) switch
		{
			(_, { State: ConnectionState.NoServer }) => (EntryIcon.Error, "No endpoint found"),
			(not null, { State: ConnectionState.Connecting }) => (EntryIcon.Loading, "Connecting..."),
			(null or { State: not ConnectionState.ConnectionTimeout }, { State: ConnectionState.ConnectionTimeout }) => (EntryIcon.Error, "Timeout"),
			(null or { State: not ConnectionState.ConnectionFailed }, { State: ConnectionState.ConnectionFailed }) => (EntryIcon.Error, "Connection error"),

			(null or { IsVersionValid: not false }, { IsVersionValid: false }) => (EntryIcon.Warning, "Version mismatch"),
			(null or { InvalidFrames.Count: 0 }, { InvalidFrames.Count: > 0 }) => (EntryIcon.Warning, "Unknown messages"),
			(null or { MissingRequiredProcessors.IsEmpty: true }, { MissingRequiredProcessors.IsEmpty: false }) => (EntryIcon.Warning, "Processors missing"),

			({ KeepAlive.State: KeepAliveState.Idle or KeepAliveState.Ok }, { KeepAlive.State: KeepAliveState.Late }) => (EntryIcon.Error, "Connection lost (>1000ms)"),
			({ KeepAlive.State: KeepAliveState.Idle or KeepAliveState.Ok }, { KeepAlive.State: KeepAliveState.Lost }) => (EntryIcon.Error, "Connection lost (>1s)"),
			({ KeepAlive.State: KeepAliveState.Idle or KeepAliveState.Ok }, { KeepAlive.State: KeepAliveState.Aborted }) => (EntryIcon.Error, "Connection lost (keep-alive)"),
			({ State: ConnectionState.Connected }, { State: ConnectionState.Disconnected }) => (EntryIcon.Error, "Connection lost"),

			({ State: ConnectionState.Connected }, { State: ConnectionState.Reconnecting }) => (EntryIcon.Error, "Connection lost (reconnecting)"),

			_ => (default, default)
		};

		return desc is null
			? null
			: new DevServerEntry { Title = desc, Icon = iconState | EntryIcon.Connection };
	}
}

internal record EngineEntry() : HotReloadLogEntry(EntrySource.Engine, -1, DateTimeOffset.Now)
{
	public static EngineEntry? TryCreateNew(Status? oldStatus, Status status)
		=> (oldStatus?.State ?? HotReloadState.Initializing, status.State) switch
		{
			( < HotReloadState.Ready, HotReloadState.Ready) => new EngineEntry { Title = "Connected", Icon = EntryIcon.Connection | EntryIcon.Success },
			(not HotReloadState.Disabled, HotReloadState.Disabled) => new EngineEntry { Title = "Cannot initialize", Icon = EntryIcon.Connection | EntryIcon.Error },
			_ => null
		};
}

internal record ServerEntry : HotReloadLogEntry
{
	public ServerEntry(HotReloadServerOperationData srvOp)
		: base(EntrySource.Server, srvOp.Id, srvOp.StartTime)
	{
		Update(srvOp);
	}

	/// <summary>
	/// Indicates if this notification is the final one for the operation, INCLUDING application wide.
	/// </summary>
	public bool IsFinal { get; private set; }

	public void Update(HotReloadServerOperationData srvOp)
	{
		IsFinal = srvOp.Result is not HotReloadServerResult.Success;
		(IsSuccess, Icon) = srvOp.Result switch
		{
			null => (default, EntryIcon.HotReload | EntryIcon.Loading),
			HotReloadServerResult.Success or HotReloadServerResult.NoChanges => (true, EntryIcon.HotReload | EntryIcon.Success),
			_ => (false, EntryIcon.HotReload | EntryIcon.Error)
		};
		Title = srvOp.Result switch
		{
			HotReloadServerResult.NoChanges => "No changes detected",
			HotReloadServerResult.RudeEdit => "Rude edit detected, restart required",
			HotReloadServerResult.Failed => "Compilation errors",
			HotReloadServerResult.Aborted => "Operation cancelled",
			HotReloadServerResult.InternalError => "An error occured",
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
			HotReloadClientResult.Success => "Update successful",
			HotReloadClientResult.Failed => "An error occured",
			_ => null
		};
		Description = Join("type", localOp.CuratedTypes);
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
	Loading = 1 << 0,
	Success = 1 << 1,
	Warning = 1 << 2,
	Error = 1 << 3,

	// Source
	Connection = 1 << 8,
	HotReload = 2 << 8,
}


[Windows.UI.Xaml.Data.Bindable]
internal record HotReloadLogEntry(EntrySource Source, long Id, DateTimeOffset Timestamp) : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	public bool? IsSuccess { get; set; }
	public TimeSpan? Duration { get; set; }
	public EntryIcon Icon { get; set; }

	public string? Title { get; set; }
	public string? Description { get; set; }
	public string? ToastDescription => Description ?? Title;

	public string TimeInfo => Duration switch
	{
		null => $"{Timestamp:T}",
		{ TotalMilliseconds: < 1000 } ms => $"{ms.TotalMilliseconds:F0} ms - {Timestamp:T}",
		{ } s => $"{s.TotalSeconds:N0} s - {Timestamp:T}",
	};

	protected void RaiseChanged()
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));

	protected static string? Join(string kind, string[] items, int? total = null, int max = 5)
	{
		const int maxLength = 70;

		if (items is { Length: 0 } && total is null)
		{
			return null;
		}

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
