#nullable enable

using System;
using System.Collections.Generic;

#if IS_UNIT_TESTS
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;
#else
namespace Windows.UI.Notifications.Internal;
#endif

internal sealed class AndroidToastNotificationBootReceiverLifecycle : IToastNotificationScheduleLifecycle
{
	private const int MaximumReconciliationAttempts = 8;
	private static readonly object _gate = new();
	private readonly Func<ToastNotificationScheduleSnapshot> _loadState;
	private readonly Action<bool> _setEnabled;
	private readonly Func<bool> _hasBootPermission;

	public AndroidToastNotificationBootReceiverLifecycle(
		IToastNotificationSchedulePersistence persistence,
		Action<bool> setEnabled,
		Func<bool> hasBootPermission)
		: this(persistence.Load, setEnabled, hasBootPermission)
	{
		ArgumentNullException.ThrowIfNull(persistence);
	}

	internal AndroidToastNotificationBootReceiverLifecycle(
		Func<ToastNotificationScheduleSnapshot> loadState,
		Action<bool> setEnabled,
		Func<bool> hasBootPermission)
	{
		_loadState = loadState ?? throw new ArgumentNullException(nameof(loadState));
		_setEnabled = setEnabled ?? throw new ArgumentNullException(nameof(setEnabled));
		_hasBootPermission = hasBootPermission ?? throw new ArgumentNullException(nameof(hasBootPermission));
	}

	internal AndroidToastNotificationBootReceiverLifecycle(
		Func<IReadOnlyList<ToastNotificationScheduleRecord>> loadRecords,
		Action<bool> setEnabled,
		Func<bool> hasBootPermission)
		: this(
			() => new ToastNotificationScheduleSnapshot(
				ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
				loadRecords()),
			setEnabled,
			hasBootPermission)
	{
		ArgumentNullException.ThrowIfNull(loadRecords);
	}

	public void ValidateNewSchedule()
	{
		if (!_hasBootPermission())
		{
			throw new InvalidOperationException(
				"Scheduled Android notifications require android.permission.RECEIVE_BOOT_COMPLETED in the application's manifest.");
		}
	}

	public void OnSchedulesChanged()
		=> UpdateReceiverState();

	public void Reconcile()
		=> UpdateReceiverState();

	private void UpdateReceiverState()
	{
		lock (_gate)
		{
			for (var attempt = 0; attempt < MaximumReconciliationAttempts; attempt++)
			{
				var enabled = _hasBootPermission() && ShouldEnable(_loadState());
				_setEnabled(enabled);
				if (enabled == (_hasBootPermission() && ShouldEnable(_loadState())))
				{
					return;
				}
			}
			throw new InvalidOperationException(
				"Android could not reconcile scheduled notification recovery because the durable schedule state kept changing.");
		}
	}

	internal static bool ShouldEnable(ToastNotificationScheduleSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state.Records.Count > 0 ||
			ToastNotificationScheduleSnapshotMerger.GetOperations(state).Count > 0;
	}
}

internal static class AndroidToastNotificationRecoveryActions
{
	internal const string BootCompleted = "android.intent.action.BOOT_COMPLETED";
	internal const string MyPackageReplaced = "android.intent.action.MY_PACKAGE_REPLACED";

	public static bool ShouldRecover(string? action)
		=> action is BootCompleted or MyPackageReplaced;
}
