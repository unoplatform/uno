#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications.Internal;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[ContractVersion(typeof(AppNotificationsContract), 1 * 0x10000u)]
public sealed class AppNotificationManager
{
	private static readonly TimeSpan OperationLeaseDuration = TimeSpan.FromMinutes(1);
	private static readonly AppNotificationManager _default = new();
	private readonly object _gate = new();
	private readonly object _lifecycleGate = new();
	private readonly Dictionary<string, DeferredShowOperation> _deferredShowOperations = new(StringComparer.Ordinal);
	private readonly Dictionary<uint, List<string>> _deferredShowOperationsById = new();
	private readonly Func<IAppNotificationManagerBackend?> _backendFactory;
	private readonly Func<AppNotificationStateStore> _stateStoreFactory;
	private readonly string _operationOwner = Guid.NewGuid().ToString("N");
	private IAppNotificationManagerBackend? _backend;
	private AppNotificationStateStore? _stateStore;
	private Task _persistentStateRecoveryTask = Task.CompletedTask;
	private TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs>? _notificationInvoked;
	private volatile bool _hasRegistration;
	private bool _isRegistered;
	private bool _isLifecycleTransitioning;
	private bool _isBackendConfigured;

	private AppNotificationManager()
	{
#if __WASM__
		WebAssemblyAppNotificationConfiguration.Capture();
#endif
		_backendFactory = AppNotificationManagerBackendFactory.Create;
		_stateStoreFactory = () => new AppNotificationStateStore(AppNotificationStatePersistenceFactory.Create());
	}

	internal AppNotificationManager(IAppNotificationManagerBackend? backend)
	{
		_backend = backend;
		_backendFactory = () => backend;
		_stateStoreFactory = () => new AppNotificationStateStore(new InMemoryAppNotificationStatePersistence());
	}

	internal AppNotificationManager(Func<IAppNotificationManagerBackend?> backendFactory)
	{
		_backendFactory = backendFactory;
		_stateStoreFactory = () => new AppNotificationStateStore(new InMemoryAppNotificationStatePersistence());
	}

	internal AppNotificationManager(IAppNotificationManagerBackend? backend, IAppNotificationStatePersistence persistence)
	{
		_backend = backend;
		_backendFactory = () => backend;
		_stateStoreFactory = () => new AppNotificationStateStore(persistence);
	}

	public static AppNotificationManager Default => _default;

	public AppNotificationSetting Setting => GetBackend() is { IsSupported: true } backend
		? backend.Setting
		: AppNotificationSetting.Unsupported;

	[ContractVersion(typeof(AppNotificationsContract), 2 * 0x10000u)]
	public static bool IsSupported() => _default.GetBackend()?.IsSupported == true;

	public void Register()
	{
		if (GetBackend() is { IsSupported: true } backend)
		{
			lock (_lifecycleGate)
			{
				if (_isLifecycleTransitioning)
				{
					throw new InvalidOperationException("Another app notification registration operation is already in progress.");
				}
				if (_isRegistered)
				{
					throw new InvalidOperationException("The application is already registered for app notifications.");
				}
				_isLifecycleTransitioning = true;
			}
			try
			{
				backend.Register();
				lock (_lifecycleGate)
				{
					_hasRegistration = true;
					_isRegistered = true;
				}
				AppNotificationActivationBroker.Register(OnNotificationActivated);
			}
			finally
			{
				lock (_lifecycleGate)
				{
					_isLifecycleTransitioning = false;
				}
			}
		}
	}

	[ContractVersion(typeof(AppNotificationsContract), 2 * 0x10000u)]
	public void Register(string displayName, Uri iconUri)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		if (string.IsNullOrEmpty(displayName))
		{
			throw new ArgumentException("A display name is required.", nameof(displayName));
		}
		ArgumentNullException.ThrowIfNull(iconUri);

		lock (_lifecycleGate)
		{
			if (_isLifecycleTransitioning)
			{
				throw new InvalidOperationException("Another app notification registration operation is already in progress.");
			}
			if (_isRegistered)
			{
				throw new InvalidOperationException("The application is already registered for app notifications.");
			}
			_isLifecycleTransitioning = true;
		}
		try
		{
			backend.Register(displayName, iconUri);
			lock (_lifecycleGate)
			{
				_hasRegistration = true;
				_isRegistered = true;
			}
			AppNotificationActivationBroker.Register(OnNotificationActivated);
		}
		finally
		{
			lock (_lifecycleGate)
			{
				_isLifecycleTransitioning = false;
			}
		}
	}

	public void Unregister()
	{
		if (GetBackend() is { IsSupported: true } backend)
		{
			lock (_lifecycleGate)
			{
				if (_isLifecycleTransitioning)
				{
					throw new InvalidOperationException("Another app notification registration operation is already in progress.");
				}
				if (!_isRegistered)
				{
					throw new InvalidOperationException("The application is not registered for app notifications.");
				}
				_isLifecycleTransitioning = true;
			}
			var completed = false;
			try
			{
				backend.Unregister();
				AppNotificationActivationBroker.Unregister(OnNotificationActivated);
				completed = true;
			}
			finally
			{
				lock (_lifecycleGate)
				{
					if (completed)
					{
						_isRegistered = false;
					}
					_isLifecycleTransitioning = false;
				}
			}
		}
	}

	public void UnregisterAll()
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}

		bool hadRegistration;
		lock (_lifecycleGate)
		{
			if (_isLifecycleTransitioning)
			{
				throw new InvalidOperationException("Another app notification registration operation is already in progress.");
			}
			_isLifecycleTransitioning = true;
			hadRegistration = _hasRegistration;
			_hasRegistration = false;
		}
		var nativeRegistrationRemoved = false;
		try
		{
			lock (_gate)
			{
				backend.UnregisterAll();
			}
			nativeRegistrationRemoved = true;
			AppNotificationActivationBroker.Unregister(OnNotificationActivated);
		}
		finally
		{
			lock (_lifecycleGate)
			{
				if (nativeRegistrationRemoved)
				{
					_isRegistered = false;
				}
				else
				{
					_hasRegistration = hadRegistration;
				}
				_isLifecycleTransitioning = false;
			}
		}
	}

	public void Show(AppNotification notification)
		=> _ = Show(notification, replaceTagAndGroup: true, requiresRegistration: true);

	internal void ShowReplacingTagAndGroup(AppNotification notification)
		=> _ = Show(notification, replaceTagAndGroup: true, requiresRegistration: false);

	internal AppNotificationPostingResult ShowScheduled(AppNotification notification, string deliveryCorrelation)
	{
		ArgumentNullException.ThrowIfNull(deliveryCorrelation);
		return Show(notification, replaceTagAndGroup: true, requiresRegistration: false, deliveryCorrelation);
	}

	private AppNotificationPostingResult Show(AppNotification notification, bool replaceTagAndGroup, bool requiresRegistration, string deliveryCorrelation = "")
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return AppNotificationPostingResult.NotPosted;
		}
		ArgumentNullException.ThrowIfNull(notification);

		lock (_gate)
		{
			if (requiresRegistration && !_hasRegistration)
			{
				return AppNotificationPostingResult.NotPosted;
			}
			var snapshot = notification.CaptureSnapshot();
			if (snapshot.Id != 0)
			{
				throw new COMException("The notification has already been posted.", unchecked((int)0x803E0106));
			}

			var payload = AppNotificationPayloadParser.Parse(snapshot.Payload);
			if (backend.Setting != AppNotificationSetting.Enabled)
			{
				return AppNotificationPostingResult.NotPosted;
			}

			var state = GetStateStore();
			state.Reload();
			var activeIds = RecoverPendingOperations(backend);
			SweepExpired(backend);
			ReconcileActiveIds(state.GetAllRecords(), activeIds);
			var now = DateTimeOffset.UtcNow;
			var progress = snapshot.Progress is null ? null : AppNotificationProgressSnapshot.From(snapshot.Progress);
			var reservation = state.PrepareShow(
				snapshot.Payload,
				snapshot.Tag,
				snapshot.Group,
				snapshot.Expiration,
				snapshot.ExpiresOnReboot,
				backend.BootIdentifier,
				snapshot.Priority,
				snapshot.SuppressDisplay,
				progress,
				now,
				_operationOwner,
				now + OperationLeaseDuration,
				replaceTagAndGroup,
				deliveryCorrelation);
			if (reservation.Kind == AppNotificationShowReservationKind.Duplicate)
			{
				return AppNotificationPostingResult.AlreadyPosted;
			}
			if (reservation.Kind == AppNotificationShowReservationKind.Busy)
			{
				return AppNotificationPostingResult.NotPosted;
			}
			var record = reservation.Record!;
			if (reservation.Kind == AppNotificationShowReservationKind.Replacement)
			{
				var previous = reservation.PreviousRecord!;
				var defersCompletion = backend is IDeferredAppNotificationManagerBackend { DefersShowCompletion: true };
				if (!TryUpdate(
					backend,
					record,
					previous,
					DeferredFailureBehavior.Restore,
					reservation.DuplicateRecords))
				{
					state.TryResolveFailedShow(record, previous, reservation.DuplicateRecords);
					return AppNotificationPostingResult.NotPosted;
				}

				if (!defersCompletion)
				{
					if (!state.TryMarkShown(record))
					{
						return ResolvePostingStateConflict(state, deliveryCorrelation);
					}
				}
				notification.SetNotificationId(record.Id);
				if (!defersCompletion)
				{
					RemoveDuplicateRecords(backend, reservation.DuplicateRecords);
				}
				return AppNotificationPostingResult.Posted;
			}
			var envelope = new AppNotificationEnvelope(
				record.Id,
				payload,
				snapshot.Tag,
				snapshot.Group,
				snapshot.Expiration,
				snapshot.ExpiresOnReboot,
				snapshot.SuppressDisplay,
				snapshot.Priority,
				progress,
				snapshot.Payload);
			if (!TryShow(backend, envelope, record))
			{
				state.TryAbort(record);
				return AppNotificationPostingResult.NotPosted;
			}

			try
			{
				notification.SetNotificationId(record.Id);
				if (backend is not IDeferredAppNotificationManagerBackend { DefersShowCompletion: true })
				{
					if (!state.TryMarkShown(record))
					{
						return ResolvePostingStateConflict(state, deliveryCorrelation);
					}
				}
			}
			catch
			{
				try
				{
					backend.Remove(record);
					state.TryAbort(record);
				}
				catch
				{
					// Preserve uncertain state for recovery on the next manager operation.
				}
				throw;
			}
			return AppNotificationPostingResult.Posted;
		}
	}

	private static AppNotificationPostingResult ResolvePostingStateConflict(
		AppNotificationStateStore state,
		string deliveryCorrelation)
	{
		state.Reload();
		return deliveryCorrelation.Length > 0 && state.HasDeliveryReceipt(deliveryCorrelation)
			? AppNotificationPostingResult.AlreadyPosted
			: AppNotificationPostingResult.NotPosted;
	}

	[Overload("UpdateAsync")]
	public IAsyncOperation<AppNotificationProgressResult> UpdateAsync(AppNotificationProgressData data, string tag, string group)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.FromResult(AppNotificationProgressResult.Unsupported).AsAsyncOperation();
		}
		ArgumentNullException.ThrowIfNull(data);
		ValidateIdentifier(tag, nameof(tag));
		ValidateGroup(group, nameof(group));
		var progress = AppNotificationProgressSnapshot.From(data.Clone());
		if (GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend)
		{
			return UpdateAsyncCore(asyncBackend, progress, tag, group).AsAsyncOperation();
		}
		return Task.Run(() => Update(progress, tag, group)).AsAsyncOperation();
	}

	[Overload("UpdateAsync2")]
	public IAsyncOperation<AppNotificationProgressResult> UpdateAsync(AppNotificationProgressData data, string tag)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.FromResult(AppNotificationProgressResult.Unsupported).AsAsyncOperation();
		}
		ArgumentNullException.ThrowIfNull(data);
		ValidateIdentifier(tag, nameof(tag));
		var progress = AppNotificationProgressSnapshot.From(data.Clone());
		if (GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend)
		{
			return UpdateAsyncCore(asyncBackend, progress, tag, string.Empty).AsAsyncOperation();
		}
		return Task.Run(() => Update(progress, tag, string.Empty)).AsAsyncOperation();
	}

	public IAsyncAction RemoveByIdAsync(uint notificationId)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.CompletedTask.AsAsyncAction();
		}
		if (notificationId == 0)
		{
			throw new ArgumentException("A non-zero notification ID is required.", nameof(notificationId));
		}
		if (GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend)
		{
			return RemoveAsyncCore(asyncBackend, store => store.GetById(notificationId)).AsAsyncAction();
		}
		return Task.Run(() => RemoveById(notificationId)).AsAsyncAction();
	}

	public IAsyncAction RemoveByTagAsync(string tag)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.CompletedTask.AsAsyncAction();
		}
		ValidateIdentifier(tag, nameof(tag));
		if (GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend)
		{
			return RemoveAsyncCore(asyncBackend, store => store.GetByTag(tag)).AsAsyncAction();
		}
		return Task.Run(() => RemoveByTag(tag)).AsAsyncAction();
	}

	public IAsyncAction RemoveByTagAndGroupAsync(string tag, string group)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.CompletedTask.AsAsyncAction();
		}
		ValidateIdentifier(tag, nameof(tag));
		ValidateGroup(group, nameof(group));
		if (GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend)
		{
			return RemoveAsyncCore(asyncBackend, store => store.GetByTagAndGroup(tag, group)).AsAsyncAction();
		}
		return Task.Run(() => RemoveByTagAndGroup(tag, group)).AsAsyncAction();
	}

	public IAsyncAction RemoveByGroupAsync(string group)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.CompletedTask.AsAsyncAction();
		}
		ValidateIdentifier(group, nameof(group));
		if (GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend)
		{
			return RemoveAsyncCore(asyncBackend, store => store.GetByGroup(group)).AsAsyncAction();
		}
		return Task.Run(() => RemoveByGroup(group)).AsAsyncAction();
	}

	public IAsyncAction RemoveAllAsync()
		=> GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend
			? RemoveAllAsyncCore(asyncBackend).AsAsyncAction()
			: Task.Run(RemoveAll).AsAsyncAction();

	public IAsyncOperation<IList<AppNotification>> GetAllAsync()
		=> GetBackend() is IAsyncAppNotificationManagerBackend asyncBackend
			? GetAllAsyncCore(asyncBackend).AsAsyncOperation()
			: Task.Run<IList<AppNotification>>(GetAll).AsAsyncOperation();

	public event TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs> NotificationInvoked
	{
		add
		{
			lock (_lifecycleGate)
			{
				if (_isRegistered || _isLifecycleTransitioning)
				{
					throw new InvalidOperationException("NotificationInvoked handlers must be added before Register is called.");
				}
				_notificationInvoked += value;
			}
		}
		remove => _notificationInvoked -= value;
	}

	private IAppNotificationManagerBackend? GetBackend()
	{
		if (_backend is not null)
		{
			ConfigureBackend(_backend);
			return _backend;
		}
		lock (_gate)
		{
			var backend = _backend ??= _backendFactory();
			if (backend is not null)
			{
				ConfigureBackend(backend);
			}
			return backend;
		}
	}

	private void ConfigureBackend(IAppNotificationManagerBackend backend)
	{
		if (_isBackendConfigured)
		{
			return;
		}
		lock (_gate)
		{
			if (_isBackendConfigured)
			{
				return;
			}
			if (backend is IDeferredAppNotificationManagerBackend deferred)
			{
				deferred.SetShowCompletedHandler(OnDeferredShowCompleted);
			}
			_isBackendConfigured = true;
			if (backend is IAsyncAppNotificationManagerBackend asyncBackend &&
				backend is IAppNotificationActiveIdRefreshCapability { RequiresActiveIdsForStateChanges: true })
			{
				_persistentStateRecoveryTask = RecoverPersistentStateAsync(backend, asyncBackend);
			}
		}
	}

	private void OnDeferredShowCompleted(string operationCorrelation, uint id, bool succeeded)
	{
		lock (_gate)
		{
			if (!_deferredShowOperations.TryGetValue(operationCorrelation, out var operation) || operation.PendingRecord.Id != id)
			{
				return;
			}

			var state = GetStateStore();
			state.Reload();
			var resolvedRecord = succeeded
				? operation.PendingRecord with { PostingState = AppNotificationPostingState.Shown }
				: operation.RollbackRecord;
			PropagateDeferredResolution(operation, resolvedRecord);
			if (succeeded)
			{
				if (state.TryMarkShown(operation.PendingRecord) && operation.DuplicateRecords.Count > 0)
				{
					RemoveDuplicateRecords(GetBackend()!, operation.DuplicateRecords);
				}
			}
			else if (operation.FailureBehavior == DeferredFailureBehavior.Abort)
			{
				state.TryResolveFailedShow(
					operation.PendingRecord,
					restore: null,
					duplicates: operation.DuplicateRecords);
			}
			else if (operation.FailureBehavior == DeferredFailureBehavior.Restore)
			{
				state.TryResolveFailedShow(
					operation.PendingRecord,
					operation.RollbackRecord,
					operation.DuplicateRecords);
			}
			RemoveDeferredOperation(operationCorrelation);
		}
	}

	private bool TryShow(
		IAppNotificationManagerBackend backend,
		AppNotificationEnvelope notification,
		AppNotificationStateRecord pendingRecord)
	{
		if (backend is not IDeferredAppNotificationManagerBackend { DefersShowCompletion: true } deferred)
		{
			return backend.TryShow(notification);
		}

		var operation = RegisterDeferredOperation(pendingRecord, rollbackRecord: null, DeferredFailureBehavior.Abort);
		if (deferred.TryShow(notification, operation.Correlation))
		{
			return true;
		}
		RemoveDeferredOperation(operation.Correlation);
		return false;
	}

	private bool TryUpdate(
		IAppNotificationManagerBackend backend,
		AppNotificationStateRecord pendingRecord,
		AppNotificationStateRecord? rollbackRecord,
		DeferredFailureBehavior failureBehavior,
		IReadOnlyList<AppNotificationDuplicateReservation>? duplicateRecords = null)
	{
		if (backend is not IDeferredAppNotificationManagerBackend { DefersShowCompletion: true } deferred)
		{
			return backend.TryUpdate(pendingRecord);
		}

		var operation = RegisterDeferredOperation(pendingRecord, rollbackRecord, failureBehavior, duplicateRecords);
		if (deferred.TryUpdate(pendingRecord, operation.Correlation))
		{
			return true;
		}
		RemoveDeferredOperation(operation.Correlation);
		return false;
	}

	private DeferredShowOperation RegisterDeferredOperation(
		AppNotificationStateRecord pendingRecord,
		AppNotificationStateRecord? rollbackRecord,
		DeferredFailureBehavior failureBehavior,
		IReadOnlyList<AppNotificationDuplicateReservation>? duplicateRecords = null)
	{
		var operation = new DeferredShowOperation(
			Guid.NewGuid().ToString("N"),
			pendingRecord,
			rollbackRecord,
			failureBehavior,
			duplicateRecords ?? Array.Empty<AppNotificationDuplicateReservation>());
		_deferredShowOperations.Add(operation.Correlation, operation);
		if (!_deferredShowOperationsById.TryGetValue(pendingRecord.Id, out var correlations))
		{
			correlations = new List<string>();
			_deferredShowOperationsById.Add(pendingRecord.Id, correlations);
		}
		correlations.Add(operation.Correlation);
		return operation;
	}

	private void PropagateDeferredResolution(DeferredShowOperation operation, AppNotificationStateRecord? resolvedRecord)
	{
		if (!_deferredShowOperationsById.TryGetValue(operation.PendingRecord.Id, out var correlations))
		{
			return;
		}

		var operationIndex = correlations.IndexOf(operation.Correlation);
		for (var index = operationIndex + 1; index < correlations.Count; index++)
		{
			if (_deferredShowOperations.TryGetValue(correlations[index], out var later) &&
				later.RollbackRecord is { } rollback &&
				rollback.Id == operation.PendingRecord.Id &&
				rollback.Revision == operation.PendingRecord.Revision)
			{
				later.RollbackRecord = resolvedRecord;
			}
		}
	}

	private void RemoveDeferredOperation(string operationCorrelation)
	{
		if (!_deferredShowOperations.Remove(operationCorrelation, out var operation) ||
			!_deferredShowOperationsById.TryGetValue(operation.PendingRecord.Id, out var correlations))
		{
			return;
		}

		correlations.Remove(operationCorrelation);
		if (correlations.Count == 0)
		{
			_deferredShowOperationsById.Remove(operation.PendingRecord.Id);
		}
	}

	private AppNotificationStateStore GetStateStore()
	{
		if (_stateStore is not null)
		{
			return _stateStore;
		}
		lock (_gate)
		{
			return _stateStore ??= _stateStoreFactory();
		}
	}

	private AppNotificationProgressResult Update(AppNotificationProgressSnapshot progress, string tag, string group)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return AppNotificationProgressResult.Unsupported;
		}
		if (backend is IAppNotificationProgressUpdateCapability { SupportsProgressUpdates: false })
		{
			return AppNotificationProgressResult.Unsupported;
		}

		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			var activeIds = RecoverPendingOperations(backend);
			SweepExpired(backend);
			ReconcileActiveIds(state.GetAllRecords(), activeIds);
			var unresolvedUpdates = state.GetPendingUpdates();
			var now = DateTimeOffset.UtcNow;
			var result = state.BeginProgressUpdate(
				tag,
				group,
				progress,
				_operationOwner,
				now + OperationLeaseDuration,
				now,
				out var updates);
			if (result != AppNotificationProgressResult.Succeeded || updates.Count == 0)
			{
				return result == AppNotificationProgressResult.Succeeded &&
					unresolvedUpdates.Any(record =>
						record.Tag == tag &&
						record.Group == group &&
						record.Progress?.SequenceNumber == progress.SequenceNumber)
					? AppNotificationProgressResult.AppNotificationNotFound
					: result;
			}

			var succeeded = 0;
			foreach (var record in updates)
			{
				if (TryUpdate(backend, record, rollbackRecord: null, DeferredFailureBehavior.Preserve))
				{
					if (backend is not IDeferredAppNotificationManagerBackend { DefersShowCompletion: true })
					{
						state.TryMarkShown(record);
					}
					succeeded++;
				}
			}
			return succeeded > 0 ? AppNotificationProgressResult.Succeeded : AppNotificationProgressResult.AppNotificationNotFound;
		}
	}

	private async Task<AppNotificationProgressResult> UpdateAsyncCore(
		IAsyncAppNotificationManagerBackend asyncBackend,
		AppNotificationProgressSnapshot progress,
		string tag,
		string group)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return AppNotificationProgressResult.Unsupported;
		}
		if (backend is IAppNotificationProgressUpdateCapability { SupportsProgressUpdates: false })
		{
			return AppNotificationProgressResult.Unsupported;
		}
		if (backend is IDeferredAppNotificationManagerBackend deferred)
		{
			await deferred.WaitForPendingShowsAsync();
		}
		await WaitForPersistentStateRecoveryAsync();
		if (!await RefreshAndReconcileStateAsync(backend, asyncBackend))
		{
			return AppNotificationProgressResult.AppNotificationNotFound;
		}

		IReadOnlyList<AppNotificationStateRecord> updates;
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			var unresolvedUpdates = state.GetPendingUpdates();
			var now = DateTimeOffset.UtcNow;
			var result = state.BeginProgressUpdate(
				tag,
				group,
				progress,
				_operationOwner,
				now + OperationLeaseDuration,
				now,
				out updates);
			if (result != AppNotificationProgressResult.Succeeded || updates.Count == 0)
			{
				return result == AppNotificationProgressResult.Succeeded &&
					unresolvedUpdates.Any(record =>
						record.Tag == tag &&
						record.Group == group &&
						record.Progress?.SequenceNumber == progress.SequenceNumber)
					? AppNotificationProgressResult.AppNotificationNotFound
					: result;
			}
		}

		var succeeded = 0;
		foreach (var record in updates)
		{
			if (await asyncBackend.TryUpdateAsync(record))
			{
				TryMarkShown(record);
				succeeded++;
			}
		}
		return succeeded > 0 ? AppNotificationProgressResult.Succeeded : AppNotificationProgressResult.AppNotificationNotFound;
	}

	private async Task WaitForPersistentStateRecoveryAsync()
	{
		Task recovery;
		lock (_gate)
		{
			recovery = _persistentStateRecoveryTask;
		}
		await recovery;
	}

	private async Task RecoverPersistentStateAsync(
		IAppNotificationManagerBackend backend,
		IAsyncAppNotificationManagerBackend asyncBackend)
	{
		try
		{
			await RefreshAndReconcileStateAsync(backend, asyncBackend);
		}
		catch
		{
			// A later asynchronous operation will retry the persistent refresh.
		}
	}

	private async Task<bool> RefreshAndReconcileStateAsync(
		IAppNotificationManagerBackend backend,
		IAsyncAppNotificationManagerBackend asyncBackend)
	{
		IReadOnlyList<AppNotificationStateRecord> captured;
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			captured = state.GetAllRecords();
		}
		var activeIds = await asyncBackend.GetActiveNotificationIdsAsync();
		if (activeIds is null &&
			backend is IAppNotificationActiveIdRefreshCapability { RequiresActiveIdsForStateChanges: true })
		{
			return false;
		}
		await ReconcileAwaitedState(backend, asyncBackend, captured, activeIds);
		return true;
	}

	private async Task ReconcileAwaitedState(
		IAppNotificationManagerBackend backend,
		IAsyncAppNotificationManagerBackend asyncBackend,
		IReadOnlyList<AppNotificationStateRecord> captured,
		IReadOnlyCollection<uint>? activeIds)
	{
		var active = activeIds?.ToHashSet();
		var now = DateTimeOffset.UtcNow;
		foreach (var record in captured)
		{
			var isActive = active?.Contains(record.Id);
			var isExpired = IsExpired(record, now, backend.BootIdentifier);
			if (HasForeignLiveOperationLease(record, now) &&
				(record.PostingState == AppNotificationPostingState.Updating || isExpired))
			{
				continue;
			}
			if (isExpired)
			{
				if (isActive == false)
				{
					TryRemove(record);
				}
				else
				{
					await RemoveWithAcknowledgementAsync(asyncBackend, record, now);
				}
				continue;
			}

			switch (record.PostingState)
			{
				case AppNotificationPostingState.Shown when isActive == false:
					TryRemove(record);
					break;
				case AppNotificationPostingState.Posting when isActive == true:
					TryMarkShown(record);
					break;
				case AppNotificationPostingState.Posting when isActive == false:
					if (TryClaimExpired(record, now, out var abandonedPosting))
					{
						TryRemove(abandonedPosting!);
					}
					break;
				case AppNotificationPostingState.Posting when
					backend is not IAppNotificationActiveIdRefreshCapability { RequiresActiveIdsForStateChanges: true }:
					if (TryClaimExpired(record, now, out var uncertainPosting) &&
						await asyncBackend.RemoveAsync(uncertainPosting!))
					{
						TryRemove(uncertainPosting!);
					}
					break;
				case AppNotificationPostingState.Updating when isActive == false:
					TryRemove(record);
					break;
				case AppNotificationPostingState.Updating when isActive == true:
					if (TryClaimExpired(record, now, out var pendingUpdate) &&
						await asyncBackend.TryUpdateAsync(pendingUpdate!))
					{
						TryMarkShown(pendingUpdate!);
					}
					break;
				case AppNotificationPostingState.Removing when isActive == false:
					TryRemove(record);
					break;
				case AppNotificationPostingState.Removing:
					if (TryClaimExpired(record, now, out var pendingRemoval) &&
						await asyncBackend.RemoveAsync(pendingRemoval!))
					{
						TryRemove(pendingRemoval!);
					}
					break;
			}
		}
	}

	private async Task RemoveWithAcknowledgementAsync(
		IAsyncAppNotificationManagerBackend asyncBackend,
		AppNotificationStateRecord record,
		DateTimeOffset now)
	{
		AppNotificationStateRecord? removal;
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			if (!state.TryBeginRemoval(record, _operationOwner, now + OperationLeaseDuration, out removal))
			{
				return;
			}
		}
		if (await asyncBackend.RemoveAsync(removal!))
		{
			TryRemove(removal!);
		}
	}

	private async Task RemoveAsyncCore(
		IAsyncAppNotificationManagerBackend asyncBackend,
		Func<AppNotificationStateStore, IReadOnlyList<AppNotificationStateRecord>> select)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		if (backend is IDeferredAppNotificationManagerBackend deferred)
		{
			await deferred.WaitForPendingShowsAsync();
		}
		await WaitForPersistentStateRecoveryAsync();
		if (!await RefreshAndReconcileStateAsync(backend, asyncBackend))
		{
			return;
		}
		IReadOnlyList<AppNotificationStateRecord> records;
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			records = select(state);
		}
		foreach (var record in records)
		{
			await RemoveWithAcknowledgementAsync(asyncBackend, record, DateTimeOffset.UtcNow);
		}
	}

	private async Task RemoveAllAsyncCore(IAsyncAppNotificationManagerBackend asyncBackend)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		if (backend is IDeferredAppNotificationManagerBackend deferred)
		{
			await deferred.WaitForPendingShowsAsync();
		}
		await WaitForPersistentStateRecoveryAsync();
		if (!await RefreshAndReconcileStateAsync(backend, asyncBackend))
		{
			return;
		}
		IReadOnlyList<AppNotificationStateRecord> records;
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			records = state.GetAllRecords();
		}
		foreach (var record in records)
		{
			await RemoveWithAcknowledgementAsync(asyncBackend, record, DateTimeOffset.UtcNow);
		}
	}

	private async Task<IList<AppNotification>> GetAllAsyncCore(IAsyncAppNotificationManagerBackend asyncBackend)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return new List<AppNotification>();
		}
		if (backend is IDeferredAppNotificationManagerBackend deferred)
		{
			await deferred.WaitForPendingShowsAsync();
		}
		await WaitForPersistentStateRecoveryAsync();
		await RefreshAndReconcileStateAsync(backend, asyncBackend);
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			return state.GetShown().Select(CreateNotification).ToList();
		}
	}

	internal void RemoveById(uint notificationId)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		Remove(backend, store => store.GetById(notificationId));
	}

	internal void RemoveByTag(string tag)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		Remove(backend, store => store.GetByTag(tag));
	}

	internal void RemoveByTagAndGroup(string tag, string group)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		Remove(backend, store => store.GetByTagAndGroup(tag, group));
	}

	internal void RemoveByGroup(string group)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		Remove(backend, store => store.GetByGroup(group));
	}

	internal void RemoveAll()
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			RecoverPendingOperations(backend);
			SweepExpired(backend);
			var records = state.GetAllRecords();
			foreach (var record in records)
			{
				RemoveWithAcknowledgement(backend, record);
			}
		}
	}

	internal IList<AppNotification> GetAll()
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return new List<AppNotification>();
		}

		lock (_gate)
		{
			GetStateStore().Reload();
			var activeIds = RecoverPendingOperations(backend);
			SweepExpired(backend);
			var state = GetStateStore();
			ReconcileActiveIds(state.GetAllRecords(), activeIds);
			return state.GetShown().Select(CreateNotification).ToList();
		}
	}

	private void Remove(IAppNotificationManagerBackend backend, Func<AppNotificationStateStore, IReadOnlyList<AppNotificationStateRecord>> remove)
	{
		lock (_gate)
		{
			GetStateStore().Reload();
			RecoverPendingOperations(backend);
			SweepExpired(backend);
			var state = GetStateStore();
			var records = remove(state);
			foreach (var record in records)
			{
				RemoveWithAcknowledgement(backend, record);
			}
		}
	}

	private void SweepExpired(IAppNotificationManagerBackend backend)
	{
		var state = GetStateStore();
		var now = DateTimeOffset.UtcNow;
		var records = state.GetExpired(now, backend.BootIdentifier);
		foreach (var record in records)
		{
			if (!HasForeignLiveOperationLease(record, now))
			{
				RemoveWithAcknowledgement(backend, record);
			}
		}
	}

	private IReadOnlyCollection<uint>? RecoverPendingOperations(IAppNotificationManagerBackend backend)
	{
		var state = GetStateStore();
		var activeIds = backend.GetActiveNotificationIds();
		var active = activeIds?.ToHashSet();
		var deferredBackend = backend as IDeferredAppNotificationManagerBackend;
		var now = DateTimeOffset.UtcNow;
		foreach (var record in state.GetAllRecords())
		{
			var isActive = active?.Contains(record.Id);
			if (record.PostingState is AppNotificationPostingState.Posting or AppNotificationPostingState.Updating &&
				deferredBackend?.IsShowPending(record.Id) == true)
			{
				continue;
			}
			if (isActive is null &&
				backend is IAppNotificationActiveIdRefreshCapability { RequiresActiveIdsForStateChanges: true } &&
				record.PostingState is AppNotificationPostingState.Posting or AppNotificationPostingState.Updating)
			{
				continue;
			}

			switch (record.PostingState)
			{
				case AppNotificationPostingState.Posting when isActive == true:
					state.TryMarkShown(record);
					break;
				case AppNotificationPostingState.Posting when isActive == false:
					if (state.TryClaimExpiredOperation(
						record,
						_operationOwner,
						now + OperationLeaseDuration,
						now,
						out var abandonedPosting))
					{
						state.TryRemove(abandonedPosting!);
					}
					break;
				case AppNotificationPostingState.Posting:
					if (state.TryClaimExpiredOperation(
						record,
						_operationOwner,
						now + OperationLeaseDuration,
						now,
						out var uncertainPosting) &&
						TryRemoveAbandoned(backend, uncertainPosting!))
					{
						state.TryRemove(uncertainPosting!);
					}
					break;
				case AppNotificationPostingState.Updating when isActive == false:
					if (!HasForeignLiveOperationLease(record, now))
					{
						state.TryRemove(record);
					}
					break;
				case AppNotificationPostingState.Updating when isActive != false:
					if (state.TryClaimExpiredOperation(
						record,
						_operationOwner,
						now + OperationLeaseDuration,
						now,
						out var pendingUpdate) &&
						TryUpdate(backend, pendingUpdate!, rollbackRecord: null, DeferredFailureBehavior.Preserve) &&
						deferredBackend is not { DefersShowCompletion: true })
					{
						state.TryMarkShown(pendingUpdate!);
					}
					break;
				case AppNotificationPostingState.Removing when isActive == false:
					state.TryRemove(record);
					break;
				case AppNotificationPostingState.Removing:
					if (state.TryClaimExpiredOperation(
						record,
						_operationOwner,
						now + OperationLeaseDuration,
						now,
						out var pendingRemoval) &&
						TryRemoveAbandoned(backend, pendingRemoval!))
					{
						state.TryRemove(pendingRemoval!);
					}
					break;
			}
		}
		return activeIds;
	}

	// Recovery of operations abandoned by an earlier process must not fail the caller's own
	// operation; the record stays durable so a later recovery pass can retry it.
	private static bool TryRemoveAbandoned(IAppNotificationManagerBackend backend, AppNotificationStateRecord record)
	{
		try
		{
			backend.Remove(record);
			return true;
		}
		catch (Exception exception)
		{
			if (typeof(AppNotificationManager).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(AppNotificationManager).Log().LogWarning($"An abandoned app notification operation could not be completed: {exception.Message}");
			}
			return false;
		}
	}

	private void RemoveWithAcknowledgement(IAppNotificationManagerBackend backend, AppNotificationStateRecord record)
	{
		var state = GetStateStore();
		if (!state.TryBeginRemoval(
			record,
			_operationOwner,
			DateTimeOffset.UtcNow + OperationLeaseDuration,
			out var removal))
		{
			return;
		}
		backend.Remove(removal!);
		state.TryRemove(removal!);
	}

	private void RemoveDuplicateRecords(
		IAppNotificationManagerBackend backend,
		IReadOnlyList<AppNotificationDuplicateReservation> duplicates)
	{
		var state = GetStateStore();
		foreach (var duplicate in duplicates)
		{
			try
			{
				backend.Remove(duplicate.Removal);
				state.TryRemove(duplicate.Removal);
			}
			catch
			{
				// Keep durable state when native duplicate cleanup is not acknowledged.
			}
		}
	}

	private void ReconcileActiveIds(
		IReadOnlyList<AppNotificationStateRecord> captured,
		IReadOnlyCollection<uint>? activeIds)
	{
		if (activeIds is null)
		{
			return;
		}
		var active = activeIds.ToHashSet();
		foreach (var record in captured)
		{
			if (record.PostingState == AppNotificationPostingState.Shown && !active.Contains(record.Id))
			{
				TryRemove(record);
			}
		}
	}

	private bool TryClaimExpired(
		AppNotificationStateRecord record,
		DateTimeOffset now,
		out AppNotificationStateRecord? claimed)
	{
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			return state.TryClaimExpiredOperation(
				record,
				_operationOwner,
				now + OperationLeaseDuration,
				now,
				out claimed);
		}
	}

	private bool TryMarkShown(AppNotificationStateRecord record)
	{
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			return state.TryMarkShown(record);
		}
	}

	private bool TryRemove(AppNotificationStateRecord record)
	{
		lock (_gate)
		{
			var state = GetStateStore();
			state.Reload();
			return state.TryRemove(record);
		}
	}

	private static bool IsExpired(
		AppNotificationStateRecord record,
		DateTimeOffset now,
		string? bootIdentifier)
		=> (record.ExpirationUtc > DateTimeOffset.FromFileTime(0) && record.ExpirationUtc <= now) ||
			(record.ExpiresOnReboot &&
				record.BootIdentifier is not null &&
				bootIdentifier is not null &&
				!string.Equals(record.BootIdentifier, bootIdentifier, StringComparison.Ordinal));

	private bool HasForeignLiveOperationLease(AppNotificationStateRecord record, DateTimeOffset now)
		=> record.PostingState is AppNotificationPostingState.Posting or AppNotificationPostingState.Updating &&
			!string.Equals(record.OperationOwner, _operationOwner, StringComparison.Ordinal) &&
			record.OperationLeaseExpirationUtc > now;

	private static AppNotification CreateNotification(AppNotificationStateRecord record)
	{
		var notification = new AppNotification(record.Payload)
		{
			Tag = record.Tag,
			Group = record.Group,
			Expiration = record.ExpirationUtc,
			ExpiresOnReboot = record.ExpiresOnReboot,
			Progress = record.Progress?.ToProgressData(),
		};
		notification.SetNotificationId(record.Id);
		return notification;
	}

	private static void ValidateIdentifier(string value, string parameterName)
	{
		if (string.IsNullOrEmpty(value))
		{
			throw new ArgumentException("A non-empty notification identifier is required.", parameterName);
		}
	}

	private static void ValidateGroup(string value, string parameterName)
	{
		if (value is null)
		{
			throw new ArgumentNullException(parameterName);
		}
	}

	private void OnNotificationActivated(AppNotificationActivation activation)
		=> _notificationInvoked?.Invoke(this, new AppNotificationActivatedEventArgs(activation.Argument, activation.UserInput));

	private enum DeferredFailureBehavior
	{
		Abort,
		Restore,
		Preserve,
	}

	private sealed class DeferredShowOperation
	{
		public DeferredShowOperation(
			string correlation,
			AppNotificationStateRecord pendingRecord,
			AppNotificationStateRecord? rollbackRecord,
			DeferredFailureBehavior failureBehavior,
			IReadOnlyList<AppNotificationDuplicateReservation> duplicateRecords)
		{
			Correlation = correlation;
			PendingRecord = pendingRecord;
			RollbackRecord = rollbackRecord;
			FailureBehavior = failureBehavior;
			DuplicateRecords = duplicateRecords;
		}

		public string Correlation { get; }

		public AppNotificationStateRecord PendingRecord { get; }

		public AppNotificationStateRecord? RollbackRecord { get; set; }

		public DeferredFailureBehavior FailureBehavior { get; }

		public IReadOnlyList<AppNotificationDuplicateReservation> DuplicateRecords { get; }
	}
}
