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

	public AndroidToastNotificationBootReceiverLifecycle(
		IToastNotificationSchedulePersistence persistence,
		Action<bool> setEnabled)
		: this(persistence.Load, setEnabled)
	{
		ArgumentNullException.ThrowIfNull(persistence);
	}

	internal AndroidToastNotificationBootReceiverLifecycle(
		Func<ToastNotificationScheduleSnapshot> loadState,
		Action<bool> setEnabled)
	{
		_loadState = loadState ?? throw new ArgumentNullException(nameof(loadState));
		_setEnabled = setEnabled ?? throw new ArgumentNullException(nameof(setEnabled));
	}

	internal AndroidToastNotificationBootReceiverLifecycle(
		Func<IReadOnlyList<ToastNotificationScheduleRecord>> loadRecords,
		Action<bool> setEnabled)
		: this(
			() => new ToastNotificationScheduleSnapshot(
				ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
				loadRecords()),
			setEnabled)
	{
		ArgumentNullException.ThrowIfNull(loadRecords);
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
				var enabled = ShouldEnable(_loadState());
				_setEnabled(enabled);
				if (enabled == ShouldEnable(_loadState()))
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
