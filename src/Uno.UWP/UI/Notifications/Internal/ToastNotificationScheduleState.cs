#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Windows.UI.Notifications.Internal;

internal enum ToastNotificationScheduleStatus
{
	Active,
	Canceling,
	Delivering,
}

internal enum ToastNotificationNativeOperationKind
{
	Schedule,
	Cancel,
	Retry,
}

internal sealed record ToastNotificationScheduleRecord(
	string ScheduleIdentifier,
	string Payload,
	DateTimeOffset DeliveryTimeUtc,
	DateTimeOffset? ExpirationTimeUtc,
	string Id,
	string Tag,
	string Group,
	bool SuppressPopup,
	TimeSpan? SnoozeInterval,
	uint MaximumSnoozeCount,
	ToastNotificationScheduleStatus Status = ToastNotificationScheduleStatus.Active,
	NotificationMirroring NotificationMirroring = NotificationMirroring.Allowed,
	long Revision = 0,
	string DeliveryClaimOwner = "",
	string DeliveryClaimToken = "",
	DateTimeOffset DeliveryClaimExpirationUtc = default);

internal sealed record ToastNotificationNativeOperation(
	string ScheduleIdentifier,
	ToastNotificationNativeOperationKind Kind,
	string OperationIdentifier,
	long RecordRevision = 0,
	long Revision = 0);

internal sealed record ToastNotificationDeliveryClaim(
	ToastNotificationScheduleRecord Record,
	string Owner,
	string Token,
	long Revision)
{
	public string ScheduleIdentifier => Record.ScheduleIdentifier;

	public ToastNotificationScheduleStatus Status => Record.Status;
}

internal sealed record ToastNotificationScheduleSnapshot(
	int SchemaVersion,
	IReadOnlyList<ToastNotificationScheduleRecord> Records,
	long Revision = 0,
	IReadOnlyList<ToastNotificationNativeOperation>? NativeOperations = null)
{
	public const int CurrentSchemaVersion = 3;

	public static ToastNotificationScheduleSnapshot Empty { get; } = new(
		CurrentSchemaVersion,
		Array.Empty<ToastNotificationScheduleRecord>(),
		NativeOperations: Array.Empty<ToastNotificationNativeOperation>());
}

internal interface IToastNotificationSchedulePersistence
{
	ToastNotificationScheduleSnapshot Load();

	void Save(ToastNotificationScheduleSnapshot state);
}

internal interface IMergingToastNotificationSchedulePersistence
{
	ToastNotificationScheduleSnapshot MergeAndSave(
		ToastNotificationScheduleSnapshot baseline,
		ToastNotificationScheduleSnapshot state);
}

internal sealed class ToastNotificationScheduleConflictException : InvalidOperationException
{
	public ToastNotificationScheduleConflictException(
		string identifier,
		long expectedRevision,
		long latestRevision)
		: base(
			$"Scheduled notification {identifier} was changed by another process " +
			$"(expected revision {expectedRevision}, latest revision {latestRevision}).")
	{
	}
}

internal static class ToastNotificationScheduleSnapshotMerger
{
	public static ToastNotificationScheduleSnapshot Merge(
		ToastNotificationScheduleSnapshot baseline,
		ToastNotificationScheduleSnapshot state,
		ToastNotificationScheduleSnapshot latest)
	{
		var baselineRecords = baseline.Records.ToDictionary(record => record.ScheduleIdentifier, StringComparer.Ordinal);
		var nextRecords = state.Records.ToDictionary(record => record.ScheduleIdentifier, StringComparer.Ordinal);
		var latestRecords = latest.Records.ToDictionary(record => record.ScheduleIdentifier, StringComparer.Ordinal);
		var baselineOperations = GetOperations(baseline).ToDictionary(operation => operation.ScheduleIdentifier, StringComparer.Ordinal);
		var nextOperations = GetOperations(state).ToDictionary(operation => operation.ScheduleIdentifier, StringComparer.Ordinal);
		var latestOperations = GetOperations(latest).ToDictionary(operation => operation.ScheduleIdentifier, StringComparer.Ordinal);
		var changedIdentifiers = baselineRecords.Keys
			.Union(nextRecords.Keys, StringComparer.Ordinal)
			.Union(baselineOperations.Keys, StringComparer.Ordinal)
			.Union(nextOperations.Keys, StringComparer.Ordinal)
			.Where(identifier =>
			{
				baselineRecords.TryGetValue(identifier, out var baselineRecord);
				nextRecords.TryGetValue(identifier, out var nextRecord);
				baselineOperations.TryGetValue(identifier, out var baselineOperation);
				nextOperations.TryGetValue(identifier, out var nextOperation);
				return !Equals(baselineRecord, nextRecord) || !Equals(baselineOperation, nextOperation);
			})
			.ToArray();

		foreach (var identifier in changedIdentifiers)
		{
			baselineRecords.TryGetValue(identifier, out var baselineRecord);
			latestRecords.TryGetValue(identifier, out var latestRecord);
			baselineOperations.TryGetValue(identifier, out var baselineOperation);
			latestOperations.TryGetValue(identifier, out var latestOperation);
			if (!Equals(baselineRecord, latestRecord) || !Equals(baselineOperation, latestOperation))
			{
				throw new ToastNotificationScheduleConflictException(
					identifier,
					Math.Max(baselineRecord?.Revision ?? 0, baselineOperation?.Revision ?? 0),
					Math.Max(latestRecord?.Revision ?? 0, latestOperation?.Revision ?? 0));
			}
		}

		if (changedIdentifiers.Length == 0)
		{
			return Clone(latest);
		}

		var transactionRevision = checked(latest.Revision + 1);
		foreach (var identifier in changedIdentifiers)
		{
			baselineRecords.TryGetValue(identifier, out var baselineRecord);
			nextRecords.TryGetValue(identifier, out var nextRecord);
			var recordChanged = !Equals(baselineRecord, nextRecord);
			if (recordChanged)
			{
				if (nextRecord is null)
				{
					latestRecords.Remove(identifier);
				}
				else
				{
					latestRecords[identifier] = nextRecord with { Revision = transactionRevision };
				}
			}

			baselineOperations.TryGetValue(identifier, out var baselineOperation);
			nextOperations.TryGetValue(identifier, out var nextOperation);
			var operationChanged = !Equals(baselineOperation, nextOperation);
			if (operationChanged)
			{
				if (nextOperation is null)
				{
					latestOperations.Remove(identifier);
				}
				else
				{
					var recordRevision = nextOperation.Kind is ToastNotificationNativeOperationKind.Schedule or ToastNotificationNativeOperationKind.Retry
						? latestRecords[identifier].Revision
						: nextOperation.RecordRevision;
					latestOperations[identifier] = nextOperation with
					{
						RecordRevision = recordRevision,
						Revision = transactionRevision,
					};
				}
			}
		}

		return new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			latestRecords.Values
				.OrderBy(record => record.DeliveryTimeUtc)
				.ThenBy(record => record.ScheduleIdentifier, StringComparer.Ordinal)
				.ToArray(),
			transactionRevision,
			latestOperations.Values
				.OrderBy(operation => operation.ScheduleIdentifier, StringComparer.Ordinal)
				.ToArray());
	}

	public static IReadOnlyList<ToastNotificationNativeOperation> GetOperations(ToastNotificationScheduleSnapshot state)
		=> state.NativeOperations ?? Array.Empty<ToastNotificationNativeOperation>();

	public static ToastNotificationScheduleSnapshot Clone(ToastNotificationScheduleSnapshot state)
		=> state with
		{
			Records = state.Records.ToArray(),
			NativeOperations = GetOperations(state).ToArray(),
		};
}

internal sealed class InMemoryToastNotificationSchedulePersistence : IToastNotificationSchedulePersistence, IMergingToastNotificationSchedulePersistence
{
	private readonly object _gate = new();
	private ToastNotificationScheduleSnapshot _state;

	public InMemoryToastNotificationSchedulePersistence(ToastNotificationScheduleSnapshot? state = null)
	{
		_state = ToastNotificationScheduleSnapshotMerger.Clone(state ?? ToastNotificationScheduleSnapshot.Empty);
	}

	public ToastNotificationScheduleSnapshot Load()
	{
		lock (_gate)
		{
			return ToastNotificationScheduleSnapshotMerger.Clone(_state);
		}
	}

	public void Save(ToastNotificationScheduleSnapshot state)
	{
		lock (_gate)
		{
			_state = ToastNotificationScheduleSnapshotMerger.Clone(state);
		}
	}

	public ToastNotificationScheduleSnapshot MergeAndSave(
		ToastNotificationScheduleSnapshot baseline,
		ToastNotificationScheduleSnapshot state)
	{
		lock (_gate)
		{
			_state = ToastNotificationScheduleSnapshotMerger.Merge(baseline, state, _state);
			return ToastNotificationScheduleSnapshotMerger.Clone(_state);
		}
	}
}

internal sealed class ToastNotificationScheduleStore
{
	internal const int MaximumScheduledNotifications = 4096;
	private const int MaximumConflictRetries = 16;
	private readonly object _gate = new();
	private readonly IToastNotificationSchedulePersistence _persistence;
	private ToastNotificationScheduleSnapshot _state;

	public ToastNotificationScheduleStore(IToastNotificationSchedulePersistence persistence)
	{
		_persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
		_state = Normalize(persistence.Load());
	}

	public void Reload()
	{
		lock (_gate)
		{
			ReloadCore();
		}
	}

	public ToastNotificationNativeOperation Add(ToastNotificationScheduleRecord record, DateTimeOffset now)
	{
		ArgumentNullException.ThrowIfNull(record);
		if (record.DeliveryTimeUtc <= now.ToUniversalTime())
		{
			throw new COMException("The scheduled notification delivery time must be in the future.", unchecked((int)0x80070718));
		}

		lock (_gate)
		{
			return Mutate(
				current =>
				{
					if (current.Records.Count >= MaximumScheduledNotifications)
					{
						throw new COMException("The maximum number of scheduled notifications has been reached.", unchecked((int)0x80070718));
					}
					if (current.Records.Any(item => item.ScheduleIdentifier == record.ScheduleIdentifier))
					{
						throw new InvalidOperationException("The scheduled notification is already registered.");
					}

					var active = ClearClaim(record with
					{
						Status = ToastNotificationScheduleStatus.Active,
						Revision = 0,
					});
					var operation = CreateOperation(active.ScheduleIdentifier, ToastNotificationNativeOperationKind.Schedule);
					return (
						WithEntry(current, active, operation),
						operation);
				},
				current => GetOperation(current, record.ScheduleIdentifier)!);
		}
	}

	public ToastNotificationScheduleRecord? Get(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		lock (_gate)
		{
			ReloadCore();
			return GetRecord(_state, scheduleIdentifier);
		}
	}

	public IReadOnlyList<ToastNotificationScheduleRecord> GetAll()
	{
		lock (_gate)
		{
			ReloadCore();
			return _state.Records
				.Where(record => record.Status == ToastNotificationScheduleStatus.Active)
				.OrderBy(record => record.DeliveryTimeUtc)
				.ToArray();
		}
	}

	public IReadOnlyList<ToastNotificationScheduleRecord> GetAllDurableRecords()
	{
		lock (_gate)
		{
			ReloadCore();
			return _state.Records.ToArray();
		}
	}

	public IReadOnlyList<ToastNotificationNativeOperation> GetPendingNativeOperations()
	{
		lock (_gate)
		{
			ReloadCore();
			return GetOperations(_state).ToArray();
		}
	}

	public ToastNotificationNativeOperation? RequestRemove(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					var record = GetRecord(current, scheduleIdentifier);
					var operation = GetOperation(current, scheduleIdentifier);
					if (record is null && operation is null)
					{
						return (null, null);
					}
					var cancel = CreateOperation(scheduleIdentifier, ToastNotificationNativeOperationKind.Cancel);
					return (
						WithEntry(current, record: null, operation: cancel),
						cancel);
				},
				current => GetOperation(current, scheduleIdentifier));
		}
	}

	public ToastNotificationNativeOperation? RequestSchedule(
		string scheduleIdentifier,
		ToastNotificationNativeOperationKind kind)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		if (kind is not ToastNotificationNativeOperationKind.Schedule and not ToastNotificationNativeOperationKind.Retry)
		{
			throw new ArgumentOutOfRangeException(nameof(kind));
		}

		lock (_gate)
		{
			return Mutate(
				current =>
				{
					if (GetOperation(current, scheduleIdentifier) is { } existing)
					{
						return (null, existing);
					}
					if (GetRecord(current, scheduleIdentifier) is not { Status: ToastNotificationScheduleStatus.Active } record)
					{
						return (null, null);
					}
					var operation = CreateOperation(scheduleIdentifier, kind, record.Revision);
					return (
						WithEntry(current, record, operation),
						operation);
				},
				current => GetOperation(current, scheduleIdentifier));
		}
	}

	public ToastNotificationDeliveryClaim? TryClaimDelivery(
		string scheduleIdentifier,
		string owner,
		DateTimeOffset now,
		DateTimeOffset expiration)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		ArgumentException.ThrowIfNullOrEmpty(owner);
		lock (_gate)
		{
			return Mutate<ToastNotificationDeliveryClaim?>(
				current =>
				{
					var record = GetRecord(current, scheduleIdentifier);
					if (record is null ||
						record.Status == ToastNotificationScheduleStatus.Canceling ||
						HasLiveClaim(record, now))
					{
						return (null, null);
					}
					var claimed = record with
					{
						Status = ToastNotificationScheduleStatus.Delivering,
						DeliveryClaimOwner = owner,
						DeliveryClaimToken = Guid.NewGuid().ToString("N"),
						DeliveryClaimExpirationUtc = expiration.ToUniversalTime(),
					};
					return (
						WithEntry(current, claimed, operation: null),
						new ToastNotificationDeliveryClaim(claimed, owner, claimed.DeliveryClaimToken, claimed.Revision));
				},
				current =>
				{
					var claimed = GetRecord(current, scheduleIdentifier)!;
					return new ToastNotificationDeliveryClaim(claimed, owner, claimed.DeliveryClaimToken, claimed.Revision);
				});
		}
	}

	public ToastNotificationNativeOperation? RetryDelivery(
		ToastNotificationDeliveryClaim claim,
		DateTimeOffset deliveryTimeUtc)
		=> TransitionClaim(claim, deliveryTimeUtc, ToastNotificationNativeOperationKind.Retry);

	public bool ReleaseDeliveryClaim(ToastNotificationDeliveryClaim claim)
	{
		ArgumentNullException.ThrowIfNull(claim);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					var record = GetRecord(current, claim.Record.ScheduleIdentifier);
					if (!MatchesClaim(record, claim))
					{
						return (null, false);
					}
					return (
						WithEntry(
							current,
							ClearClaim(record! with { Status = ToastNotificationScheduleStatus.Active }),
							operation: null),
						true);
				});
		}
	}

	public ToastNotificationNativeOperation? CompleteDelivery(ToastNotificationDeliveryClaim claim)
	{
		ArgumentNullException.ThrowIfNull(claim);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					if (!MatchesClaim(GetRecord(current, claim.Record.ScheduleIdentifier), claim))
					{
						return (null, null);
					}
					var cancel = CreateOperation(
						claim.Record.ScheduleIdentifier,
						ToastNotificationNativeOperationKind.Cancel);
					return (
						WithEntry(current, record: null, operation: cancel),
						cancel);
				},
				current => GetOperation(current, claim.Record.ScheduleIdentifier));
		}
	}

	public bool CompleteNativeDelivery(ToastNotificationDeliveryClaim claim)
	{
		ArgumentNullException.ThrowIfNull(claim);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					if (!MatchesClaim(GetRecord(current, claim.Record.ScheduleIdentifier), claim))
					{
						return (null, false);
					}
					return (
						WithEntry(
							current,
							record: null,
							operation: null,
							claim.Record.ScheduleIdentifier),
						true);
				});
		}
	}

	public bool PrepareRecoveryState(DateTimeOffset now, TimeSpan retryDelay)
	{
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					var records = current.Records.ToDictionary(record => record.ScheduleIdentifier, StringComparer.Ordinal);
					var operations = GetOperations(current).ToDictionary(operation => operation.ScheduleIdentifier, StringComparer.Ordinal);
					var changed = false;
					foreach (var record in current.Records)
					{
						if (record.Status == ToastNotificationScheduleStatus.Canceling)
						{
							records.Remove(record.ScheduleIdentifier);
							operations[record.ScheduleIdentifier] = CreateOperation(
								record.ScheduleIdentifier,
								ToastNotificationNativeOperationKind.Cancel);
							changed = true;
						}
						else if (record.Status == ToastNotificationScheduleStatus.Delivering && !HasLiveClaim(record, now))
						{
							if (record.ExpirationTimeUtc is { } expiration && now.ToUniversalTime() > expiration)
							{
								records.Remove(record.ScheduleIdentifier);
								operations[record.ScheduleIdentifier] = CreateOperation(
									record.ScheduleIdentifier,
									ToastNotificationNativeOperationKind.Cancel);
							}
							else
							{
								var retryTime = now.ToUniversalTime() + retryDelay;
								if (record.ExpirationTimeUtc is { } retryExpiration && retryTime > retryExpiration)
								{
									retryTime = retryExpiration;
								}
								records[record.ScheduleIdentifier] = ClearClaim(record with
								{
									Status = ToastNotificationScheduleStatus.Active,
									DeliveryTimeUtc = retryTime,
								});
								operations[record.ScheduleIdentifier] = CreateOperation(
									record.ScheduleIdentifier,
									ToastNotificationNativeOperationKind.Retry,
									record.Revision);
							}
							changed = true;
						}
					}

					return changed
						? (current with
						{
							Records = records.Values.ToArray(),
							NativeOperations = operations.Values.ToArray(),
						}, true)
						: (null, false);
				});
		}
	}

	public bool TryCompleteNativeOperation(ToastNotificationNativeOperation expected)
	{
		ArgumentNullException.ThrowIfNull(expected);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					var operation = GetOperation(current, expected.ScheduleIdentifier);
					if (!MatchesOperation(operation, expected))
					{
						return (null, false);
					}
					if (expected.Kind is ToastNotificationNativeOperationKind.Schedule or ToastNotificationNativeOperationKind.Retry &&
						GetRecord(current, expected.ScheduleIdentifier)?.Revision != expected.RecordRevision)
					{
						return (null, false);
					}
					return (
						WithEntry(
							current,
							GetRecord(current, expected.ScheduleIdentifier),
							operation: null,
							expected.ScheduleIdentifier),
						true);
				});
		}
	}

	public ToastNotificationNativeOperation? MarkRegistrationForRetry(ToastNotificationNativeOperation expected)
	{
		ArgumentNullException.ThrowIfNull(expected);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					var operation = GetOperation(current, expected.ScheduleIdentifier);
					var record = GetRecord(current, expected.ScheduleIdentifier);
					if (!MatchesOperation(operation, expected) ||
						record?.Revision != expected.RecordRevision ||
						expected.Kind != ToastNotificationNativeOperationKind.Schedule)
					{
						return (null, null);
					}
					var retry = CreateOperation(
						expected.ScheduleIdentifier,
						ToastNotificationNativeOperationKind.Retry,
						record.Revision);
					return (
						WithEntry(current, record, retry),
						retry);
				},
				current => GetOperation(current, expected.ScheduleIdentifier));
		}
	}

	public ToastNotificationNativeOperation? PrepareCompensation(ToastNotificationNativeOperation applied)
	{
		ArgumentNullException.ThrowIfNull(applied);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					var record = GetRecord(current, applied.ScheduleIdentifier);
					var operation = GetOperation(current, applied.ScheduleIdentifier);
					if (operation is not null && !MatchesOperation(operation, applied))
					{
						return (null, operation);
					}
					if (operation is not null &&
						applied.Kind is ToastNotificationNativeOperationKind.Schedule or ToastNotificationNativeOperationKind.Retry &&
						record?.Revision == applied.RecordRevision)
					{
						return (null, operation);
					}
					if (operation is null &&
						applied.Kind is ToastNotificationNativeOperationKind.Schedule or ToastNotificationNativeOperationKind.Retry &&
						record is { Status: ToastNotificationScheduleStatus.Active } &&
						record.Revision == applied.RecordRevision)
					{
						return (null, null);
					}
					if (operation is null && applied.Kind == ToastNotificationNativeOperationKind.Cancel && record is null)
					{
						return (null, null);
					}

					ToastNotificationNativeOperationKind desired;
					ToastNotificationScheduleRecord? desiredRecord = record;
					if (record is null || record.Status is ToastNotificationScheduleStatus.Canceling or ToastNotificationScheduleStatus.Delivering)
					{
						desired = ToastNotificationNativeOperationKind.Cancel;
						if (record?.Status == ToastNotificationScheduleStatus.Canceling)
						{
							desiredRecord = null;
						}
					}
					else
					{
						desired = applied.Kind == ToastNotificationNativeOperationKind.Retry
							? ToastNotificationNativeOperationKind.Retry
							: ToastNotificationNativeOperationKind.Schedule;
					}
					var compensation = CreateOperation(
						applied.ScheduleIdentifier,
						desired,
						desiredRecord?.Revision ?? 0);
					return (
						WithEntry(current, desiredRecord, compensation),
						compensation);
				},
				current => GetOperation(current, applied.ScheduleIdentifier));
		}
	}

	private ToastNotificationNativeOperation? TransitionClaim(
		ToastNotificationDeliveryClaim claim,
		DateTimeOffset? deliveryTimeUtc,
		ToastNotificationNativeOperationKind? operationKind)
	{
		ArgumentNullException.ThrowIfNull(claim);
		lock (_gate)
		{
			return Mutate(
				current =>
				{
					var record = GetRecord(current, claim.Record.ScheduleIdentifier);
					if (!MatchesClaim(record, claim))
					{
						return (null, null);
					}
					var active = ClearClaim(record! with
					{
						Status = ToastNotificationScheduleStatus.Active,
						DeliveryTimeUtc = deliveryTimeUtc?.ToUniversalTime() ?? record!.DeliveryTimeUtc,
					});
					var operation = operationKind is { } kind
						? CreateOperation(active.ScheduleIdentifier, kind, active.Revision)
						: null;
					return (
						WithEntry(current, active, operation),
						operation);
				},
				current => GetOperation(current, claim.Record.ScheduleIdentifier));
		}
	}

	private TResult Mutate<TResult>(
		Func<ToastNotificationScheduleSnapshot, (ToastNotificationScheduleSnapshot? State, TResult Result)> prepare,
		Func<ToastNotificationScheduleSnapshot, TResult>? committedResult = null)
	{
		for (var attempt = 0; attempt < MaximumConflictRetries; attempt++)
		{
			ReloadCore();
			var (next, result) = prepare(_state);
			if (next is null)
			{
				return result;
			}
			try
			{
				Commit(next);
				return committedResult is null ? result : committedResult(_state);
			}
			catch (ToastNotificationScheduleConflictException) when (attempt + 1 < MaximumConflictRetries)
			{
			}
		}
		throw new InvalidOperationException("Scheduled notification state kept changing during a durable mutation.");
	}

	private void Commit(ToastNotificationScheduleSnapshot next)
	{
		next = Normalize(next);
		if (_persistence is IMergingToastNotificationSchedulePersistence merging)
		{
			_state = Normalize(merging.MergeAndSave(_state, next));
		}
		else
		{
			_persistence.Save(next);
			_state = next;
		}
	}

	private void ReloadCore() => _state = Normalize(_persistence.Load());

	private static ToastNotificationScheduleSnapshot WithEntry(
		ToastNotificationScheduleSnapshot state,
		ToastNotificationScheduleRecord? record,
		ToastNotificationNativeOperation? operation,
		string? scheduleIdentifier = null)
	{
		var identifier = scheduleIdentifier ?? record?.ScheduleIdentifier ?? operation?.ScheduleIdentifier
			?? throw new ArgumentException("A schedule record or native operation is required.");
		return state with
		{
			Records = state.Records
				.Where(candidate => candidate.ScheduleIdentifier != identifier)
				.Concat(record is null ? Array.Empty<ToastNotificationScheduleRecord>() : new[] { record })
				.ToArray(),
			NativeOperations = GetOperations(state)
				.Where(candidate => candidate.ScheduleIdentifier != identifier)
				.Concat(operation is null ? Array.Empty<ToastNotificationNativeOperation>() : new[] { operation })
				.ToArray(),
		};
	}

	private static ToastNotificationScheduleRecord? GetRecord(
		ToastNotificationScheduleSnapshot state,
		string scheduleIdentifier)
		=> state.Records.FirstOrDefault(record => record.ScheduleIdentifier == scheduleIdentifier);

	private static ToastNotificationNativeOperation? GetOperation(
		ToastNotificationScheduleSnapshot state,
		string scheduleIdentifier)
		=> GetOperations(state).FirstOrDefault(operation => operation.ScheduleIdentifier == scheduleIdentifier);

	private static IReadOnlyList<ToastNotificationNativeOperation> GetOperations(ToastNotificationScheduleSnapshot state)
		=> ToastNotificationScheduleSnapshotMerger.GetOperations(state);

	private static ToastNotificationNativeOperation CreateOperation(
		string scheduleIdentifier,
		ToastNotificationNativeOperationKind kind,
		long recordRevision = 0)
		=> new(
			scheduleIdentifier,
			kind,
			Guid.NewGuid().ToString("N"),
			recordRevision);

	private static bool MatchesOperation(
		ToastNotificationNativeOperation? current,
		ToastNotificationNativeOperation expected)
		=> current is not null &&
			current.OperationIdentifier == expected.OperationIdentifier &&
			current.Revision == expected.Revision &&
			current.Kind == expected.Kind &&
			current.RecordRevision == expected.RecordRevision;

	private static bool MatchesClaim(
		ToastNotificationScheduleRecord? current,
		ToastNotificationDeliveryClaim claim)
		=> current is not null &&
			current.Status == ToastNotificationScheduleStatus.Delivering &&
			current.Revision == claim.Revision &&
			current.DeliveryClaimOwner == claim.Owner &&
			current.DeliveryClaimToken == claim.Token;

	private static bool HasLiveClaim(ToastNotificationScheduleRecord record, DateTimeOffset now)
		=> record.Status == ToastNotificationScheduleStatus.Delivering &&
			record.DeliveryClaimOwner.Length > 0 &&
			record.DeliveryClaimToken.Length > 0 &&
			record.DeliveryClaimExpirationUtc > now.ToUniversalTime();

	private static ToastNotificationScheduleRecord ClearClaim(ToastNotificationScheduleRecord record)
		=> record with
		{
			DeliveryClaimOwner = string.Empty,
			DeliveryClaimToken = string.Empty,
			DeliveryClaimExpirationUtc = DateTimeOffset.MinValue,
		};

	private static ToastNotificationScheduleSnapshot Normalize(ToastNotificationScheduleSnapshot state)
		=> state.SchemaVersion is >= 1 and <= ToastNotificationScheduleSnapshot.CurrentSchemaVersion
			? state with
			{
				SchemaVersion = ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
				Records = state.Records
					.Select(record => record with { Revision = Math.Max(0, record.Revision) })
					.ToArray(),
				Revision = Math.Max(0, state.Revision),
				NativeOperations = GetOperations(state)
					.Select(operation => operation with
					{
						RecordRevision = Math.Max(0, operation.RecordRevision),
						Revision = Math.Max(0, operation.Revision),
					})
					.ToArray(),
			}
			: ToastNotificationScheduleSnapshot.Empty;
}
