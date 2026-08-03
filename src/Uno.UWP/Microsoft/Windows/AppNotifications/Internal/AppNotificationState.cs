#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Windows.AppNotifications.Internal;

internal enum AppNotificationPostingState
{
	Posting,
	Shown,
	Updating,
}

internal sealed record AppNotificationProgressSnapshot(
	uint SequenceNumber,
	string Title,
	double Value,
	string ValueStringOverride,
	string Status)
{
	public static AppNotificationProgressSnapshot From(global::Microsoft.Windows.AppNotifications.AppNotificationProgressData data)
		=> new(data.SequenceNumber, data.Title, data.Value, data.ValueStringOverride, data.Status);

	public global::Microsoft.Windows.AppNotifications.AppNotificationProgressData ToProgressData()
		=> new(SequenceNumber)
		{
			Title = Title,
			Value = Value,
			ValueStringOverride = ValueStringOverride,
			Status = Status,
		};
}

internal sealed record AppNotificationStateRecord(
	uint Id,
	string Payload,
	string Tag,
	string Group,
	DateTimeOffset CreatedUtc,
	DateTimeOffset ExpirationUtc,
	bool ExpiresOnReboot,
	string? BootIdentifier,
	AppNotificationPriority Priority,
	bool SuppressDisplay,
	AppNotificationPostingState PostingState,
	AppNotificationProgressSnapshot? Progress)
{
	public AppNotificationEnvelope ToEnvelope()
		=> new(
			Id,
			AppNotificationPayloadParser.Parse(Payload),
			Tag,
			Group,
			ExpirationUtc,
			ExpiresOnReboot,
			SuppressDisplay,
			Priority,
			Progress);
}

internal sealed record AppNotificationStateSnapshot(
	int SchemaVersion,
	uint NextId,
	IReadOnlyList<AppNotificationStateRecord> Records)
{
	public const int CurrentSchemaVersion = 1;

	public static AppNotificationStateSnapshot Empty { get; } = new(CurrentSchemaVersion, 1, Array.Empty<AppNotificationStateRecord>());
}

internal interface IAppNotificationStatePersistence
{
	AppNotificationStateSnapshot Load();

	void Save(AppNotificationStateSnapshot state);
}

internal sealed class InMemoryAppNotificationStatePersistence : IAppNotificationStatePersistence
{
	private AppNotificationStateSnapshot _state;

	public InMemoryAppNotificationStatePersistence(AppNotificationStateSnapshot? state = null)
	{
		_state = Clone(state ?? AppNotificationStateSnapshot.Empty);
	}

	public AppNotificationStateSnapshot Load() => Clone(_state);

	public void Save(AppNotificationStateSnapshot state) => _state = Clone(state);

	private static AppNotificationStateSnapshot Clone(AppNotificationStateSnapshot state)
		=> state with { Records = state.Records.ToArray() };
}

internal sealed class AppNotificationStateStore
{
	private readonly object _gate = new();
	private readonly IAppNotificationStatePersistence _persistence;
	private AppNotificationStateSnapshot _state;

	public AppNotificationStateStore(IAppNotificationStatePersistence persistence)
	{
		_persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
		var loaded = persistence.Load();
		_state = loaded.SchemaVersion == AppNotificationStateSnapshot.CurrentSchemaVersion
			? Normalize(loaded)
			: AppNotificationStateSnapshot.Empty;
	}

	public AppNotificationStateRecord Reserve(
		string payload,
		string tag,
		string group,
		DateTimeOffset expiration,
		bool expiresOnReboot,
		string? bootIdentifier,
		AppNotificationPriority priority,
		bool suppressDisplay,
		AppNotificationProgressSnapshot? progress,
		DateTimeOffset now)
	{
		lock (_gate)
		{
			var records = _state.Records.ToList();
			var id = FindAvailableId(_state.NextId, records);
			var record = new AppNotificationStateRecord(
				id,
				payload,
				tag,
				group,
				now.ToUniversalTime(),
				expiration.ToUniversalTime(),
				expiresOnReboot,
				expiresOnReboot ? bootIdentifier : null,
				priority,
				suppressDisplay,
				AppNotificationPostingState.Posting,
				progress);
			records.Add(record);
			Commit(new AppNotificationStateSnapshot(
				AppNotificationStateSnapshot.CurrentSchemaVersion,
				IncrementId(id),
				records));
			return record;
		}
	}

	public void MarkShown(uint id)
		=> UpdateRecord(id, record => record with { PostingState = AppNotificationPostingState.Shown });

	public void Abort(uint id)
		=> Remove(record => record.Id == id);

	public IReadOnlyList<AppNotificationStateRecord> GetShown()
	{
		lock (_gate)
		{
			return _state.Records
				.Where(IsManaged)
				.OrderBy(record => record.CreatedUtc)
				.ToArray();
		}
	}

	public IReadOnlyList<AppNotificationStateRecord> GetPendingPostings()
		=> GetRecords(record => record.PostingState == AppNotificationPostingState.Posting);

	public IReadOnlyList<AppNotificationStateRecord> GetPendingUpdates()
		=> GetRecords(record => record.PostingState == AppNotificationPostingState.Updating);

	public IReadOnlyList<AppNotificationStateRecord> GetExpired(DateTimeOffset now, string? bootIdentifier)
		=> GetRecords(record =>
			IsManaged(record) &&
			((record.ExpirationUtc > DateTimeOffset.FromFileTime(0) && record.ExpirationUtc <= now.ToUniversalTime()) ||
				(record.ExpiresOnReboot &&
					record.BootIdentifier is not null &&
					bootIdentifier is not null &&
					record.BootIdentifier != bootIdentifier)));

	public IReadOnlyList<AppNotificationStateRecord> GetById(uint id)
		=> GetRecords(record => IsManaged(record) && record.Id == id);

	public IReadOnlyList<AppNotificationStateRecord> GetByTag(string tag)
		=> GetRecords(record => IsManaged(record) && record.Tag == tag);

	public IReadOnlyList<AppNotificationStateRecord> GetByTagAndGroup(string tag, string group)
		=> GetRecords(record => IsManaged(record) && record.Tag == tag && record.Group == group);

	public IReadOnlyList<AppNotificationStateRecord> GetByGroup(string group)
		=> GetRecords(record => IsManaged(record) && record.Group == group);

	public IReadOnlyList<AppNotificationStateRecord> RemoveById(uint id)
		=> Remove(record => record.Id == id);

	public IReadOnlyList<AppNotificationStateRecord> RemoveByTag(string tag)
		=> Remove(record => IsManaged(record) && record.Tag == tag);

	public IReadOnlyList<AppNotificationStateRecord> RemoveByTagAndGroup(string tag, string group)
		=> Remove(record => IsManaged(record) && record.Tag == tag && record.Group == group);

	public IReadOnlyList<AppNotificationStateRecord> RemoveByGroup(string group)
		=> Remove(record => IsManaged(record) && record.Group == group);

	public IReadOnlyList<AppNotificationStateRecord> RemoveAll()
		=> Remove(IsManaged);

	public IReadOnlyList<AppNotificationStateRecord> ReconcileActiveIds(IReadOnlyCollection<uint> activeIds)
	{
		ArgumentNullException.ThrowIfNull(activeIds);
		var active = activeIds.ToHashSet();
		return Remove(record => IsManaged(record) && !active.Contains(record.Id));
	}

	public AppNotificationProgressResult BeginProgressUpdate(string tag, string? group, AppNotificationProgressSnapshot progress, out IReadOnlyList<AppNotificationStateRecord> recordsToUpdate)
	{
		lock (_gate)
		{
			var matches = _state.Records
				.Where(record =>
					IsManaged(record) &&
					record.Tag == tag &&
					(group is null || record.Group == group))
				.ToArray();
			if (matches.Length == 0)
			{
				recordsToUpdate = Array.Empty<AppNotificationStateRecord>();
				return AppNotificationProgressResult.AppNotificationNotFound;
			}

			recordsToUpdate = matches
				.Where(record => record.Progress is null || progress.SequenceNumber > record.Progress.SequenceNumber)
				.Select(record => record with { Progress = progress, PostingState = AppNotificationPostingState.Updating })
				.ToArray();
			if (recordsToUpdate.Count > 0)
			{
				var updates = recordsToUpdate.ToDictionary(record => record.Id);
				Commit(_state with
				{
					Records = _state.Records
						.Select(record => updates.TryGetValue(record.Id, out var updated) ? updated : record)
						.ToArray(),
				});
			}
			return AppNotificationProgressResult.Succeeded;
		}
	}

	private void UpdateRecord(uint id, Func<AppNotificationStateRecord, AppNotificationStateRecord> update)
	{
		lock (_gate)
		{
			var found = false;
			var records = _state.Records.Select(record =>
			{
				if (record.Id != id)
				{
					return record;
				}
				found = true;
				return update(record);
			}).ToArray();
			if (!found)
			{
				throw new InvalidOperationException($"Notification state record {id} was not found.");
			}
			Commit(_state with { Records = records });
		}
	}

	private IReadOnlyList<AppNotificationStateRecord> Remove(Func<AppNotificationStateRecord, bool> predicate)
	{
		lock (_gate)
		{
			var removed = _state.Records.Where(predicate).ToArray();
			if (removed.Length > 0)
			{
				var removedIds = removed.Select(record => record.Id).ToHashSet();
				Commit(_state with { Records = _state.Records.Where(record => !removedIds.Contains(record.Id)).ToArray() });
			}
			return removed;
		}
	}

	private IReadOnlyList<AppNotificationStateRecord> GetRecords(Func<AppNotificationStateRecord, bool> predicate)
	{
		lock (_gate)
		{
			return _state.Records.Where(predicate).ToArray();
		}
	}

	private void Commit(AppNotificationStateSnapshot next)
	{
		_persistence.Save(next);
		_state = next;
	}

	private static AppNotificationStateSnapshot Normalize(AppNotificationStateSnapshot state)
		=> state with
		{
			NextId = state.NextId == 0 ? 1 : state.NextId,
			Records = state.Records.ToArray(),
		};

	private static bool IsManaged(AppNotificationStateRecord record)
		=> record.PostingState is AppNotificationPostingState.Shown or AppNotificationPostingState.Updating;

	private static uint FindAvailableId(uint start, IReadOnlyCollection<AppNotificationStateRecord> records)
	{
		var used = records.Select(record => record.Id).ToHashSet();
		var candidate = start == 0 ? 1 : start;
		var firstCandidate = candidate;
		do
		{
			if (!used.Contains(candidate))
			{
				return candidate;
			}
			candidate = IncrementId(candidate);
		}
		while (candidate != firstCandidate);

		throw new InvalidOperationException("No app notification IDs are available.");
	}

	private static uint IncrementId(uint id) => id == uint.MaxValue ? 1 : id + 1;
}
