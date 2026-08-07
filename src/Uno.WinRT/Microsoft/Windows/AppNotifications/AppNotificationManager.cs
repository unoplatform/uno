#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[ContractVersion(typeof(AppNotificationsContract), 1 * 0x10000u)]
public sealed class AppNotificationManager
{
	private static readonly AppNotificationManager _default = new();
	private readonly object _gate = new();
	private readonly object _lifecycleGate = new();
	private readonly Func<IAppNotificationManagerBackend?> _backendFactory;
	private readonly Func<AppNotificationStateStore> _stateStoreFactory;
	private IAppNotificationManagerBackend? _backend;
	private AppNotificationStateStore? _stateStore;
	private TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs>? _notificationInvoked;
	private volatile bool _hasRegistration;
	private bool _isRegistered;
	private bool _isLifecycleTransitioning;

	private AppNotificationManager()
	{
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
		=> Show(notification, replaceTagAndGroup: false, requiresRegistration: true);

	internal void ShowReplacingTagAndGroup(AppNotification notification)
		=> Show(notification, replaceTagAndGroup: true, requiresRegistration: false);

	internal void ShowScheduled(AppNotification notification, string deliveryCorrelation)
	{
		ArgumentNullException.ThrowIfNull(deliveryCorrelation);
		Show(notification, replaceTagAndGroup: true, requiresRegistration: false, deliveryCorrelation);
	}

	private void Show(AppNotification notification, bool replaceTagAndGroup, bool requiresRegistration, string deliveryCorrelation = "")
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return;
		}
		ArgumentNullException.ThrowIfNull(notification);

		lock (_gate)
		{
			if (requiresRegistration && !_hasRegistration)
			{
				return;
			}
			var snapshot = notification.CaptureSnapshot();
			if (snapshot.Id != 0)
			{
				throw new COMException("The notification has already been posted.", unchecked((int)0x803E0106));
			}

			var payload = AppNotificationPayloadParser.Parse(snapshot.Payload);
			if (backend.Setting != AppNotificationSetting.Enabled)
			{
				return;
			}

			var activeIds = RecoverPendingOperations(backend);
			SweepExpired(backend);
			var state = GetStateStore();
			if (deliveryCorrelation.Length > 0 && state.HasDeliveryReceipt(deliveryCorrelation))
			{
				return;
			}
			if (activeIds is not null)
			{
				state.ReconcileActiveIds(activeIds);
			}
			var progress = snapshot.Progress is null ? null : AppNotificationProgressSnapshot.From(snapshot.Progress);
			if (replaceTagAndGroup && snapshot.Tag.Length > 0 && state.GetByTagAndGroup(snapshot.Tag, snapshot.Group) is { Count: > 0 } matches)
			{
				var replacement = state.BeginReplacement(
					matches[0].Id,
					snapshot.Payload,
					snapshot.Tag,
					snapshot.Group,
					snapshot.Expiration,
					snapshot.ExpiresOnReboot,
					backend.BootIdentifier,
					snapshot.Priority,
					snapshot.SuppressDisplay,
					progress,
					DateTimeOffset.UtcNow,
					deliveryCorrelation);
				if (!backend.TryUpdate(replacement))
				{
					return;
				}

				state.MarkShown(replacement.Id);
				notification.SetNotificationId(replacement.Id);
				foreach (var duplicate in matches.Skip(1))
				{
					backend.Remove(duplicate);
					state.RemoveById(duplicate.Id);
				}
				return;
			}
			var record = state.Reserve(
				snapshot.Payload,
				snapshot.Tag,
				snapshot.Group,
				snapshot.Expiration,
				snapshot.ExpiresOnReboot,
				backend.BootIdentifier,
				snapshot.Priority,
				snapshot.SuppressDisplay,
				progress,
				DateTimeOffset.UtcNow,
				deliveryCorrelation);
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
			if (!backend.TryShow(envelope))
			{
				state.Abort(record.Id);
				return;
			}

			try
			{
				state.MarkShown(record.Id);
				notification.SetNotificationId(record.Id);
			}
			catch
			{
				try
				{
					backend.Remove(record);
					state.Abort(record.Id);
				}
				catch
				{
					// Preserve uncertain state for recovery on the next manager operation.
				}
				throw;
			}
		}
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
		ValidateIdentifier(group, nameof(group));
		var progress = AppNotificationProgressSnapshot.From(data.Clone());
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
		return Task.Run(() => Update(progress, tag, group: null)).AsAsyncOperation();
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
		return Task.Run(() => RemoveById(notificationId)).AsAsyncAction();
	}

	public IAsyncAction RemoveByTagAsync(string tag)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.CompletedTask.AsAsyncAction();
		}
		ValidateIdentifier(tag, nameof(tag));
		return Task.Run(() => RemoveByTag(tag)).AsAsyncAction();
	}

	public IAsyncAction RemoveByTagAndGroupAsync(string tag, string group)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.CompletedTask.AsAsyncAction();
		}
		ValidateIdentifier(tag, nameof(tag));
		ValidateIdentifier(group, nameof(group));
		return Task.Run(() => RemoveByTagAndGroup(tag, group)).AsAsyncAction();
	}

	public IAsyncAction RemoveByGroupAsync(string group)
	{
		if (GetBackend() is not { IsSupported: true })
		{
			return Task.CompletedTask.AsAsyncAction();
		}
		ValidateIdentifier(group, nameof(group));
		return Task.Run(() => RemoveByGroup(group)).AsAsyncAction();
	}

	public IAsyncAction RemoveAllAsync()
		=> Task.Run(RemoveAll).AsAsyncAction();

	public IAsyncOperation<IList<AppNotification>> GetAllAsync()
		=> Task.Run<IList<AppNotification>>(GetAll).AsAsyncOperation();

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
			return _backend;
		}
		lock (_gate)
		{
			return _backend ??= _backendFactory();
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

	private AppNotificationProgressResult Update(AppNotificationProgressSnapshot progress, string tag, string? group)
	{
		if (GetBackend() is not { IsSupported: true } backend)
		{
			return AppNotificationProgressResult.Unsupported;
		}

		lock (_gate)
		{
			var activeIds = RecoverPendingOperations(backend);
			var unresolvedUpdates = GetStateStore().GetPendingUpdates();
			SweepExpired(backend);
			var state = GetStateStore();
			if (activeIds is not null)
			{
				state.ReconcileActiveIds(activeIds);
			}
			var result = state.BeginProgressUpdate(tag, group, progress, out var updates);
			if (result != AppNotificationProgressResult.Succeeded || updates.Count == 0)
			{
				return result == AppNotificationProgressResult.Succeeded &&
					unresolvedUpdates.Any(record =>
						record.Tag == tag &&
						(group is null || record.Group == group) &&
						record.Progress?.SequenceNumber == progress.SequenceNumber)
					? AppNotificationProgressResult.AppNotificationNotFound
					: result;
			}

			var succeeded = 0;
			foreach (var record in updates)
			{
				if (backend.TryUpdate(record))
				{
					state.MarkShown(record.Id);
					succeeded++;
				}
			}
			return succeeded > 0 ? AppNotificationProgressResult.Succeeded : AppNotificationProgressResult.AppNotificationNotFound;
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
			RecoverPendingOperations(backend);
			SweepExpired(backend);
			var state = GetStateStore();
			var records = state.GetShown();
			foreach (var record in records)
			{
				backend.Remove(record);
			}
			backend.RemoveAll();
			state.RemoveAll();
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
			var activeIds = RecoverPendingOperations(backend);
			SweepExpired(backend);
			var state = GetStateStore();
			if (activeIds is not null)
			{
				state.ReconcileActiveIds(activeIds);
			}
			return state.GetShown().Select(CreateNotification).ToList();
		}
	}

	private void Remove(IAppNotificationManagerBackend backend, Func<AppNotificationStateStore, IReadOnlyList<AppNotificationStateRecord>> remove)
	{
		lock (_gate)
		{
			RecoverPendingOperations(backend);
			SweepExpired(backend);
			var state = GetStateStore();
			var records = remove(state);
			foreach (var record in records)
			{
				backend.Remove(record);
			}
			foreach (var record in records)
			{
				state.RemoveById(record.Id);
			}
		}
	}

	private void SweepExpired(IAppNotificationManagerBackend backend)
	{
		var state = GetStateStore();
		var records = state.GetExpired(DateTimeOffset.UtcNow, backend.BootIdentifier);
		foreach (var record in records)
		{
			backend.Remove(record);
		}
		foreach (var record in records)
		{
			state.RemoveById(record.Id);
		}
	}

	private IReadOnlyCollection<uint>? RecoverPendingOperations(IAppNotificationManagerBackend backend)
	{
		var state = GetStateStore();
		var activeIds = backend.GetActiveNotificationIds();
		var active = activeIds?.ToHashSet();
		foreach (var record in state.GetPendingPostings())
		{
			if (active is null || active.Contains(record.Id))
			{
				backend.Remove(record);
			}
			state.Abort(record.Id);
		}
		foreach (var record in state.GetPendingUpdates())
		{
			if (active is not null && !active.Contains(record.Id))
			{
				state.Abort(record.Id);
			}
			else if (backend.TryUpdate(record))
			{
				state.MarkShown(record.Id);
			}
		}
		return activeIds;
	}

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

	private void OnNotificationActivated(AppNotificationActivation activation)
		=> _notificationInvoked?.Invoke(this, new AppNotificationActivatedEventArgs(activation.Argument, activation.UserInput));
}
