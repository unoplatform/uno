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
	Removing,
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
	AppNotificationProgressSnapshot? Progress,
	string DeliveryCorrelation = "",
	long Revision = 1,
	string OperationOwner = "legacy",
	DateTimeOffset OperationLeaseExpirationUtc = default)
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
			Progress,
			Payload);
}

internal sealed record AppNotificationStateSnapshot(
	int SchemaVersion,
	uint NextId,
	IReadOnlyList<AppNotificationStateRecord> Records,
	IReadOnlyList<string>? DeliveryReceipts = null)
{
	public const int CurrentSchemaVersion = 4;

	public static AppNotificationStateSnapshot Empty { get; } = new(
		CurrentSchemaVersion,
		1,
		Array.Empty<AppNotificationStateRecord>(),
		Array.Empty<string>());
}

internal interface IAppNotificationStatePersistence
{
	AppNotificationStateSnapshot Load();

	void Save(AppNotificationStateSnapshot state);
}

internal interface IMergingAppNotificationStatePersistence
{
	AppNotificationStateSnapshot MergeAndSave(AppNotificationStateSnapshot state);
}

internal interface ITransactionalAppNotificationStatePersistence
{
	AppNotificationStateSnapshot ExecuteTransaction(
		Func<AppNotificationStateTransactionContext, AppNotificationStateSnapshot> transaction);
}

internal interface IAppNotificationIdAllocator
{
	uint AllocateId(IReadOnlyCollection<uint> localIds);
}

internal sealed class AppNotificationStateTransactionContext
{
	private readonly Func<IReadOnlyCollection<uint>, uint> _allocateId;

	public AppNotificationStateTransactionContext(
		AppNotificationStateSnapshot state,
		Func<IReadOnlyCollection<uint>, uint> allocateId)
	{
		State = state;
		_allocateId = allocateId;
	}

	public AppNotificationStateSnapshot State { get; }

	public uint AllocateId(IReadOnlyCollection<uint> localIds) => _allocateId(localIds);
}

internal enum AppNotificationShowReservationKind
{
	Duplicate,
	New,
	Replacement,
	Busy,
}

internal sealed record AppNotificationShowReservation(
	AppNotificationShowReservationKind Kind,
	AppNotificationStateRecord? Record,
	AppNotificationStateRecord? PreviousRecord,
	IReadOnlyList<AppNotificationDuplicateReservation> DuplicateRecords);

internal sealed record AppNotificationDuplicateReservation(
	AppNotificationStateRecord Original,
	AppNotificationStateRecord Removal);

internal sealed class InMemoryAppNotificationStatePersistence : IAppNotificationStatePersistence, ITransactionalAppNotificationStatePersistence
{
	private readonly object _gate = new();
	private AppNotificationStateSnapshot _state;

	public InMemoryAppNotificationStatePersistence(AppNotificationStateSnapshot? state = null)
	{
		_state = Clone(state ?? AppNotificationStateSnapshot.Empty);
	}

	public AppNotificationStateSnapshot Load()
	{
		lock (_gate)
		{
			return Clone(_state);
		}
	}

	public void Save(AppNotificationStateSnapshot state)
	{
		lock (_gate)
		{
			_state = Clone(state);
		}
	}

	public AppNotificationStateSnapshot ExecuteTransaction(
		Func<AppNotificationStateTransactionContext, AppNotificationStateSnapshot> transaction)
	{
		ArgumentNullException.ThrowIfNull(transaction);
		lock (_gate)
		{
			var latest = Clone(_state);
			var next = transaction(new AppNotificationStateTransactionContext(
				latest,
				localIds => AppNotificationStateIdAllocator.FindAvailableId(
					latest.NextId,
					latest.Records.Select(record => record.Id).Concat(localIds))));
			_state = Clone(next);
			return Clone(_state);
		}
	}

	private static AppNotificationStateSnapshot Clone(AppNotificationStateSnapshot state)
		=> state with
		{
			Records = state.Records.ToArray(),
			DeliveryReceipts = (state.DeliveryReceipts ?? Array.Empty<string>()).ToArray(),
		};
}

internal sealed class AppNotificationStateStore
{
	private const int MaximumDeliveryReceipts = 10_000;
	private readonly object _gate = new();
	private readonly IAppNotificationStatePersistence _persistence;
	private AppNotificationStateSnapshot _state;

	public AppNotificationStateStore(IAppNotificationStatePersistence persistence)
	{
		_persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
		var loaded = persistence.Load();
		_state = loaded.SchemaVersion is >= 1 and <= AppNotificationStateSnapshot.CurrentSchemaVersion
			? Normalize(loaded)
			: AppNotificationStateSnapshot.Empty;
	}

	public void Reload()
	{
		lock (_gate)
		{
			var loaded = _persistence.Load();
			_state = loaded.SchemaVersion is >= 1 and <= AppNotificationStateSnapshot.CurrentSchemaVersion
				? Normalize(loaded)
				: AppNotificationStateSnapshot.Empty;
		}
	}

	public AppNotificationShowReservation PrepareShow(
		string payload,
		string tag,
		string group,
		DateTimeOffset expiration,
		bool expiresOnReboot,
		string? bootIdentifier,
		AppNotificationPriority priority,
		bool suppressDisplay,
		AppNotificationProgressSnapshot? progress,
		DateTimeOffset now,
		string operationOwner,
		DateTimeOffset operationLeaseExpiration,
		bool replaceTagAndGroup,
		string deliveryCorrelation = "")
	{
		ArgumentNullException.ThrowIfNull(operationOwner);
		lock (_gate)
		{
			AppNotificationShowReservation? reservation = null;
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				if (deliveryCorrelation.Length > 0 &&
					((current.DeliveryReceipts ?? Array.Empty<string>()).Contains(deliveryCorrelation, StringComparer.Ordinal) ||
						current.Records.Any(record =>
							record.DeliveryCorrelation == deliveryCorrelation &&
							record.PostingState == AppNotificationPostingState.Shown)))
				{
					reservation = new AppNotificationShowReservation(
						AppNotificationShowReservationKind.Duplicate,
						null,
						null,
						Array.Empty<AppNotificationDuplicateReservation>());
					return current;
				}
				if (deliveryCorrelation.Length > 0 &&
					current.Records.Any(record => record.DeliveryCorrelation == deliveryCorrelation))
				{
					reservation = new AppNotificationShowReservation(
						AppNotificationShowReservationKind.Busy,
						null,
						null,
						Array.Empty<AppNotificationDuplicateReservation>());
					return current;
				}

				var matches = replaceTagAndGroup && tag.Length > 0
					? current.Records
						.Where(record =>
							record.PostingState != AppNotificationPostingState.Removing &&
							record.Tag == tag &&
							record.Group == group)
						.OrderBy(record => record.CreatedUtc)
						.ToArray()
					: Array.Empty<AppNotificationStateRecord>();
				if (matches.Any(record => HasForeignLiveMutationLease(record, operationOwner, now)))
				{
					reservation = new AppNotificationShowReservation(
						AppNotificationShowReservationKind.Busy,
						null,
						null,
						Array.Empty<AppNotificationDuplicateReservation>());
					return current;
				}
				if (matches.Length > 0)
				{
					var previous = matches[0];
					var duplicates = matches
						.Skip(1)
						.Select(original => new AppNotificationDuplicateReservation(
							original,
							original with
							{
								PostingState = AppNotificationPostingState.Removing,
								Revision = NextRevision(original),
								OperationOwner = operationOwner,
								OperationLeaseExpirationUtc = operationLeaseExpiration.ToUniversalTime(),
							}))
						.ToArray();
					var duplicateRemovals = duplicates.ToDictionary(duplicate => duplicate.Removal.Id, duplicate => duplicate.Removal);
					var replacement = new AppNotificationStateRecord(
						previous.Id,
						payload,
						tag,
						group,
						previous.CreatedUtc,
						expiration.ToUniversalTime(),
						expiresOnReboot,
						expiresOnReboot ? bootIdentifier : null,
						priority,
						suppressDisplay,
						AppNotificationPostingState.Updating,
						progress,
						deliveryCorrelation,
						NextRevision(previous),
						operationOwner,
						operationLeaseExpiration.ToUniversalTime());
					reservation = new AppNotificationShowReservation(
						AppNotificationShowReservationKind.Replacement,
						replacement,
						previous,
						duplicates);
					return current with
					{
						SchemaVersion = AppNotificationStateSnapshot.CurrentSchemaVersion,
						Records = current.Records
							.Select(record =>
								record.Id == previous.Id
									? replacement
									: duplicateRemovals.TryGetValue(record.Id, out var removal)
										? removal
										: record)
							.ToArray(),
					};
				}

				var ids = current.Records.Select(record => record.Id).ToArray();
				var id = transaction.AllocateId(ids);
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
					progress,
					deliveryCorrelation,
					1,
					operationOwner,
					operationLeaseExpiration.ToUniversalTime());
				reservation = new AppNotificationShowReservation(
					AppNotificationShowReservationKind.New,
					record,
					null,
					Array.Empty<AppNotificationDuplicateReservation>());
				return current with
				{
					SchemaVersion = AppNotificationStateSnapshot.CurrentSchemaVersion,
					NextId = IncrementId(id),
					Records = current.Records.Append(record).ToArray(),
				};
			});
			return reservation!;
		}
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
		DateTimeOffset now,
		string deliveryCorrelation = "")
		=> PrepareShow(
			payload,
			tag,
			group,
			expiration,
			expiresOnReboot,
			bootIdentifier,
			priority,
			suppressDisplay,
			progress,
			now,
			"legacy",
			DateTimeOffset.MinValue,
			replaceTagAndGroup: false,
			deliveryCorrelation).Record!;

	public void MarkShown(uint id)
	{
		lock (_gate)
		{
			var marked = false;
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var record = current.Records.FirstOrDefault(candidate => candidate.Id == id);
				if (record is null)
				{
					return current;
				}
				marked = true;
				return MarkShown(current, record);
			});
			if (!marked)
			{
				throw new InvalidOperationException($"Notification state record {id} was not found.");
			}
		}
	}

	public bool TryMarkShown(AppNotificationStateRecord expected)
	{
		ArgumentNullException.ThrowIfNull(expected);
		lock (_gate)
		{
			var marked = false;
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var record = current.Records.FirstOrDefault(candidate =>
					candidate.Id == expected.Id &&
					candidate.Revision == expected.Revision);
				if (record is null)
				{
					return current;
				}
				marked = true;
				return MarkShown(current, record);
			});
			return marked;
		}
	}

	public AppNotificationStateRecord BeginReplacement(
		uint id,
		string payload,
		string tag,
		string group,
		DateTimeOffset expiration,
		bool expiresOnReboot,
		string? bootIdentifier,
		AppNotificationPriority priority,
		bool suppressDisplay,
		AppNotificationProgressSnapshot? progress,
		DateTimeOffset now,
		string deliveryCorrelation = "")
	{
		AppNotificationStateRecord? replacement = null;
		UpdateRecord(id, record => replacement = new AppNotificationStateRecord(
			id,
			payload,
			tag,
			group,
			record.CreatedUtc,
			expiration.ToUniversalTime(),
			expiresOnReboot,
			expiresOnReboot ? bootIdentifier : null,
			priority,
			suppressDisplay,
			AppNotificationPostingState.Updating,
			progress,
			deliveryCorrelation,
			NextRevision(record),
			"legacy",
			DateTimeOffset.MinValue));
		return replacement!;
	}

	public void Abort(uint id)
		=> Remove(record => record.Id == id);

	public bool TryAbort(AppNotificationStateRecord expected)
		=> TryRemove(expected);

	public bool TryRemove(AppNotificationStateRecord expected)
	{
		ArgumentNullException.ThrowIfNull(expected);
		lock (_gate)
		{
			var removed = false;
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var records = current.Records
					.Where(record =>
					{
						if (record.Id == expected.Id && record.Revision == expected.Revision)
						{
							removed = true;
							return false;
						}
						return true;
					})
					.ToArray();
				return removed ? current with { Records = records } : current;
			});
			return removed;
		}
	}

	public bool TryBeginRemoval(
		AppNotificationStateRecord expected,
		string operationOwner,
		DateTimeOffset operationLeaseExpiration,
		out AppNotificationStateRecord? removal)
	{
		ArgumentNullException.ThrowIfNull(expected);
		ArgumentNullException.ThrowIfNull(operationOwner);
		AppNotificationStateRecord? result = null;
		lock (_gate)
		{
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var records = current.Records.Select(record =>
				{
					if (record.Id != expected.Id || record.Revision != expected.Revision)
					{
						return record;
					}
					if (record.PostingState == AppNotificationPostingState.Removing &&
						record.OperationOwner != operationOwner &&
						record.OperationLeaseExpirationUtc > DateTimeOffset.UtcNow)
					{
						return record;
					}
					result = record with
					{
						PostingState = AppNotificationPostingState.Removing,
						Revision = NextRevision(record),
						OperationOwner = operationOwner,
						OperationLeaseExpirationUtc = operationLeaseExpiration.ToUniversalTime(),
					};
					return result;
				}).ToArray();
				return result is null ? current : current with { Records = records };
			});
		}
		removal = result;
		return result is not null;
	}

	public bool TryRestore(AppNotificationStateRecord expected, AppNotificationStateRecord restore)
	{
		ArgumentNullException.ThrowIfNull(expected);
		ArgumentNullException.ThrowIfNull(restore);
		lock (_gate)
		{
			var restored = false;
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var records = current.Records.Select(record =>
				{
					if (record.Id != expected.Id || record.Revision != expected.Revision)
					{
						return record;
					}
					restored = true;
					return restore with
					{
						Revision = NextRevision(record),
						PostingState = AppNotificationPostingState.Shown,
						OperationOwner = string.Empty,
						OperationLeaseExpirationUtc = DateTimeOffset.MinValue,
					};
				}).ToArray();
				return restored ? current with { Records = records } : current;
			});
			return restored;
		}
	}

	public bool TryResolveFailedShow(
		AppNotificationStateRecord expected,
		AppNotificationStateRecord? restore,
		IReadOnlyList<AppNotificationDuplicateReservation> duplicates)
	{
		ArgumentNullException.ThrowIfNull(expected);
		ArgumentNullException.ThrowIfNull(duplicates);
		lock (_gate)
		{
			var resolved = false;
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var currentById = current.Records.ToDictionary(record => record.Id);
				if (!currentById.TryGetValue(expected.Id, out var currentPrimary) ||
					currentPrimary.Revision != expected.Revision ||
					duplicates.Any(duplicate =>
						!currentById.TryGetValue(duplicate.Removal.Id, out var currentDuplicate) ||
						currentDuplicate.Revision != duplicate.Removal.Revision))
				{
					return current;
				}

				var duplicateById = duplicates.ToDictionary(duplicate => duplicate.Removal.Id);
				resolved = true;
				return current with
				{
					Records = current.Records
						.Select(record =>
						{
							if (record.Id == expected.Id)
							{
								return restore is null
									? null
									: restore with
									{
										Revision = NextRevision(record),
										PostingState = AppNotificationPostingState.Shown,
										OperationOwner = string.Empty,
										OperationLeaseExpirationUtc = DateTimeOffset.MinValue,
									};
							}
							return duplicateById.TryGetValue(record.Id, out var duplicate)
								? duplicate.Original with { Revision = NextRevision(record) }
								: record;
						})
						.Where(record => record is not null)
						.Select(record => record!)
						.ToArray(),
				};
			});
			return resolved;
		}
	}

	public bool TryClaimExpiredOperation(
		AppNotificationStateRecord expected,
		string operationOwner,
		DateTimeOffset operationLeaseExpiration,
		DateTimeOffset now,
		out AppNotificationStateRecord? claimed)
	{
		ArgumentNullException.ThrowIfNull(expected);
		ArgumentNullException.ThrowIfNull(operationOwner);
		AppNotificationStateRecord? result = null;
		lock (_gate)
		{
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var records = current.Records.Select(record =>
				{
					if (record.Id != expected.Id ||
						record.Revision != expected.Revision ||
						record.PostingState is not AppNotificationPostingState.Posting and
							not AppNotificationPostingState.Updating and
							not AppNotificationPostingState.Removing ||
						record.OperationLeaseExpirationUtc > now.ToUniversalTime())
					{
						return record;
					}
					result = record with
					{
						Revision = NextRevision(record),
						OperationOwner = operationOwner,
						OperationLeaseExpirationUtc = operationLeaseExpiration.ToUniversalTime(),
					};
					return result;
				}).ToArray();
				return result is null ? current : current with { Records = records };
			});
		}
		claimed = result;
		return result is not null;
	}

	public IReadOnlyList<AppNotificationStateRecord> GetShown()
	{
		lock (_gate)
		{
			return _state.Records
				.Where(record => record.PostingState is
					AppNotificationPostingState.Shown or
					AppNotificationPostingState.Updating or
					AppNotificationPostingState.Removing)
				.OrderBy(record => record.CreatedUtc)
				.ToArray();
		}
	}

	public IReadOnlyList<AppNotificationStateRecord> GetPendingPostings()
		=> GetRecords(record => record.PostingState == AppNotificationPostingState.Posting);

	public IReadOnlyList<AppNotificationStateRecord> GetPendingUpdates()
		=> GetRecords(record => record.PostingState == AppNotificationPostingState.Updating);

	public IReadOnlyList<AppNotificationStateRecord> GetPendingRemovals()
		=> GetRecords(record => record.PostingState == AppNotificationPostingState.Removing);

	public IReadOnlyList<AppNotificationStateRecord> GetAllRecords()
		=> GetRecords(_ => true);

	public IReadOnlyList<AppNotificationStateRecord> GetExpired(DateTimeOffset now, string? bootIdentifier)
		=> GetRecords(record =>
			IsManaged(record) &&
			((record.ExpirationUtc > DateTimeOffset.FromFileTime(0) && record.ExpirationUtc <= now.ToUniversalTime()) ||
				(record.ExpiresOnReboot &&
					record.BootIdentifier is not null &&
					bootIdentifier is not null &&
					record.BootIdentifier != bootIdentifier)));

	public IReadOnlyList<AppNotificationStateRecord> GetById(uint id)
		=> GetRecords(record => record.Id == id);

	public IReadOnlyList<AppNotificationStateRecord> GetByTag(string tag)
		=> GetByTagAndGroup(tag, string.Empty);

	public IReadOnlyList<AppNotificationStateRecord> GetByTagAndGroup(string tag, string group)
		=> GetRecords(record => record.Tag == tag && record.Group == group);

	public IReadOnlyList<AppNotificationStateRecord> GetByGroup(string group)
		=> GetRecords(record => record.Group == group);

	public AppNotificationStateRecord? GetByDeliveryCorrelation(string deliveryCorrelation)
	{
		ArgumentNullException.ThrowIfNull(deliveryCorrelation);
		lock (_gate)
		{
			return _state.Records.FirstOrDefault(record => IsManaged(record) && record.DeliveryCorrelation == deliveryCorrelation);
		}
	}

	public bool HasDeliveryReceipt(string deliveryCorrelation)
	{
		ArgumentNullException.ThrowIfNull(deliveryCorrelation);
		lock (_gate)
		{
			return (_state.DeliveryReceipts ?? Array.Empty<string>()).Contains(deliveryCorrelation, StringComparer.Ordinal);
		}
	}

	public IReadOnlyList<AppNotificationStateRecord> RemoveById(uint id)
		=> Remove(record => record.Id == id);

	public IReadOnlyList<AppNotificationStateRecord> RemoveByTag(string tag)
		=> RemoveByTagAndGroup(tag, string.Empty);

	public IReadOnlyList<AppNotificationStateRecord> RemoveByTagAndGroup(string tag, string group)
		=> Remove(record => IsManaged(record) && record.Tag == tag && record.Group == group);

	public IReadOnlyList<AppNotificationStateRecord> RemoveByGroup(string group)
		=> Remove(record => IsManaged(record) && record.Group == group);

	public IReadOnlyList<AppNotificationStateRecord> RemoveAll()
		=> Remove(IsManaged);

	public void Restore(AppNotificationStateRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);
		UpdateRecord(record.Id, current => record with { Revision = NextRevision(current) });
	}

	public IReadOnlyList<AppNotificationStateRecord> ReconcileActiveIds(IReadOnlyCollection<uint> activeIds)
	{
		ArgumentNullException.ThrowIfNull(activeIds);
		var active = activeIds.ToHashSet();
		var expected = GetShown();
		var removed = new List<AppNotificationStateRecord>();
		foreach (var record in expected.Where(record => !active.Contains(record.Id)))
		{
			if (TryRemove(record))
			{
				removed.Add(record);
			}
		}
		return removed;
	}

	public AppNotificationProgressResult BeginProgressUpdate(string tag, string? group, AppNotificationProgressSnapshot progress, out IReadOnlyList<AppNotificationStateRecord> recordsToUpdate)
		=> BeginProgressUpdate(
			tag,
			group ?? string.Empty,
			progress,
			"legacy",
			DateTimeOffset.MinValue,
			out recordsToUpdate);

	public AppNotificationProgressResult BeginProgressUpdate(
		string tag,
		string group,
		AppNotificationProgressSnapshot progress,
		string operationOwner,
		DateTimeOffset operationLeaseExpiration,
		out IReadOnlyList<AppNotificationStateRecord> recordsToUpdate)
		=> BeginProgressUpdate(
			tag,
			group,
			progress,
			operationOwner,
			operationLeaseExpiration,
			DateTimeOffset.UtcNow,
			out recordsToUpdate);

	public AppNotificationProgressResult BeginProgressUpdate(
		string tag,
		string group,
		AppNotificationProgressSnapshot progress,
		string operationOwner,
		DateTimeOffset operationLeaseExpiration,
		DateTimeOffset now,
		out IReadOnlyList<AppNotificationStateRecord> recordsToUpdate)
	{
		ArgumentNullException.ThrowIfNull(group);
		ArgumentNullException.ThrowIfNull(operationOwner);
		now = now.ToUniversalTime();
		lock (_gate)
		{
			AppNotificationProgressResult result = AppNotificationProgressResult.AppNotificationNotFound;
			IReadOnlyList<AppNotificationStateRecord> updates = Array.Empty<AppNotificationStateRecord>();
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var taggedRecords = current.Records
					.Where(record =>
						record.PostingState != AppNotificationPostingState.Removing &&
						record.Tag == tag &&
						record.Group == group)
					.ToArray();
				if (taggedRecords.Any(record => HasForeignLiveMutationLease(record, operationOwner, now)))
				{
					return current;
				}
				var matches = taggedRecords.Where(IsManaged).ToArray();
				if (matches.Length == 0)
				{
					return current;
				}

				result = AppNotificationProgressResult.Succeeded;
				updates = matches
					.Where(record => record.Progress is null || progress.SequenceNumber > record.Progress.SequenceNumber)
					.Select(record => record with
					{
						Progress = progress,
						PostingState = AppNotificationPostingState.Updating,
						Revision = NextRevision(record),
						OperationOwner = operationOwner,
						OperationLeaseExpirationUtc = operationLeaseExpiration.ToUniversalTime(),
					})
					.ToArray();
				if (updates.Count == 0)
				{
					return current;
				}
				var updatesById = updates.ToDictionary(record => record.Id);
				return current with
				{
					Records = current.Records
						.Select(record => updatesById.TryGetValue(record.Id, out var updated) ? updated : record)
						.ToArray(),
				};
			});
			recordsToUpdate = updates;
			return result;
		}
	}

	private static bool HasForeignLiveMutationLease(
		AppNotificationStateRecord record,
		string operationOwner,
		DateTimeOffset now)
		=> record.PostingState is AppNotificationPostingState.Posting or AppNotificationPostingState.Updating &&
			!string.Equals(record.OperationOwner, operationOwner, StringComparison.Ordinal) &&
			record.OperationLeaseExpirationUtc > now;

	private void UpdateRecord(uint id, Func<AppNotificationStateRecord, AppNotificationStateRecord> update)
	{
		lock (_gate)
		{
			var found = false;
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				var records = current.Records.Select(record =>
				{
					if (record.Id != id)
					{
						return record;
					}
					found = true;
					return update(record);
				}).ToArray();
				return found ? current with { Records = records } : current;
			});
			if (!found)
			{
				throw new InvalidOperationException($"Notification state record {id} was not found.");
			}
		}
	}

	private IReadOnlyList<AppNotificationStateRecord> Remove(Func<AppNotificationStateRecord, bool> predicate)
	{
		lock (_gate)
		{
			IReadOnlyList<AppNotificationStateRecord> removed = Array.Empty<AppNotificationStateRecord>();
			Mutate(transaction =>
			{
				var current = Normalize(transaction.State);
				removed = current.Records.Where(predicate).ToArray();
				if (removed.Count == 0)
				{
					return current;
				}
				var removedIds = removed.Select(record => record.Id).ToHashSet();
				return current with { Records = current.Records.Where(record => !removedIds.Contains(record.Id)).ToArray() };
			});
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
		if (_persistence is IMergingAppNotificationStatePersistence merging)
		{
			_state = merging.MergeAndSave(next);
		}
		else
		{
			_persistence.Save(next);
			_state = next;
		}
	}

	private void Mutate(Func<AppNotificationStateTransactionContext, AppNotificationStateSnapshot> mutation)
	{
		if (_persistence is ITransactionalAppNotificationStatePersistence transactional)
		{
			_state = Normalize(transactional.ExecuteTransaction(mutation));
			return;
		}

		var context = new AppNotificationStateTransactionContext(
			_state,
			localIds => _persistence is IAppNotificationIdAllocator allocator
				? allocator.AllocateId(localIds)
				: AppNotificationStateIdAllocator.FindAvailableId(
					_state.NextId,
					_state.Records.Select(record => record.Id).Concat(localIds)));
		Commit(mutation(context));
	}

	private static AppNotificationStateSnapshot MarkShown(
		AppNotificationStateSnapshot state,
		AppNotificationStateRecord record)
	{
		var shown = record with
		{
			PostingState = AppNotificationPostingState.Shown,
			Revision = NextRevision(record),
			OperationOwner = string.Empty,
			OperationLeaseExpirationUtc = DateTimeOffset.MinValue,
		};
		var receipts = (state.DeliveryReceipts ?? Array.Empty<string>()).ToList();
		if (shown.DeliveryCorrelation.Length > 0 && !receipts.Contains(shown.DeliveryCorrelation, StringComparer.Ordinal))
		{
			receipts.Add(shown.DeliveryCorrelation);
			if (receipts.Count > MaximumDeliveryReceipts)
			{
				receipts.RemoveRange(0, receipts.Count - MaximumDeliveryReceipts);
			}
		}
		return state with
		{
			Records = state.Records.Select(candidate => candidate.Id == record.Id ? shown : candidate).ToArray(),
			DeliveryReceipts = receipts,
		};
	}

	private static AppNotificationStateSnapshot Normalize(AppNotificationStateSnapshot state)
	{
		var receipts = (state.DeliveryReceipts ?? Array.Empty<string>())
			.Concat(state.SchemaVersion < 3
				? state.Records
					.Where(record => record.PostingState == AppNotificationPostingState.Shown)
					.Select(record => record.DeliveryCorrelation)
				: Array.Empty<string>())
			.Where(receipt => receipt.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.TakeLast(MaximumDeliveryReceipts)
			.ToArray();
		return state with
		{
			SchemaVersion = AppNotificationStateSnapshot.CurrentSchemaVersion,
			NextId = state.NextId == 0 ? 1 : state.NextId,
			Records = state.Records
				.Select(record => record with
				{
					Revision = record.Revision <= 0 ? 1 : record.Revision,
					OperationOwner = record.OperationOwner ?? "legacy",
					OperationLeaseExpirationUtc = record.OperationLeaseExpirationUtc.ToUniversalTime(),
				})
				.ToArray(),
			DeliveryReceipts = receipts,
		};
	}

	private static bool IsManaged(AppNotificationStateRecord record)
		=> record.PostingState is AppNotificationPostingState.Shown or AppNotificationPostingState.Updating;

	private static long NextRevision(AppNotificationStateRecord record)
		=> record.Revision == long.MaxValue ? 1 : record.Revision + 1;

	private static uint IncrementId(uint id) => id == uint.MaxValue ? 1 : id + 1;
}

internal static class AppNotificationStateIdAllocator
{
	public static uint FindAvailableId(uint start, IEnumerable<uint> usedIds)
	{
		var used = usedIds.ToHashSet();
		var candidate = start == 0 ? 1 : start;
		var firstCandidate = candidate;
		do
		{
			if (!used.Contains(candidate))
			{
				return candidate;
			}
			candidate = candidate == uint.MaxValue ? 1 : candidate + 1;
		}
		while (candidate != firstCandidate);

		throw new InvalidOperationException("No app notification IDs are available.");
	}
}
