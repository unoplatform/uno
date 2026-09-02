#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Windows.AppNotifications.Internal;

internal interface IAppNotificationManagerBackend
{
	bool IsSupported { get; }

	AppNotificationSetting Setting { get; }

	string? BootIdentifier { get; }

	void Register();

	void Register(string displayName, Uri iconUri);

	void Unregister();

	void UnregisterAll();

	bool TryShow(AppNotificationEnvelope notification);

	bool TryUpdate(AppNotificationStateRecord notification);

	void Remove(AppNotificationStateRecord notification);

	void RemoveAll();

	IReadOnlyCollection<uint>? GetActiveNotificationIds();
}

internal interface IDeferredAppNotificationManagerBackend
{
	bool DefersShowCompletion { get; }

	bool TryShow(AppNotificationEnvelope notification, string operationCorrelation);

	bool TryUpdate(AppNotificationStateRecord notification, string operationCorrelation);

	bool IsShowPending(uint id);

	Task WaitForPendingShowsAsync();

	void SetShowCompletedHandler(Action<string, uint, bool> handler);
}

internal interface IAsyncAppNotificationManagerBackend
{
	Task<bool> TryUpdateAsync(AppNotificationStateRecord notification);

	Task<bool> RemoveAsync(AppNotificationStateRecord notification);

	Task<bool> RemoveAllAsync();

	Task<IReadOnlyCollection<uint>?> GetActiveNotificationIdsAsync();
}

internal interface IAppNotificationProgressUpdateCapability
{
	bool SupportsProgressUpdates { get; }
}

internal interface IAppNotificationActiveIdRefreshCapability
{
	bool RequiresActiveIdsForStateChanges { get; }
}

internal sealed record AppNotificationDeliveryReceiptChanges(
	IReadOnlyList<string> Removed,
	IReadOnlyList<string> Added);

internal static class AppNotificationDeliveryReceiptRetention
{
	public static AppNotificationDeliveryReceiptChanges CreatePlan(
		IReadOnlyCollection<string> latest,
		IReadOnlyCollection<string> next,
		int maximumCount)
	{
		ArgumentNullException.ThrowIfNull(latest);
		ArgumentNullException.ThrowIfNull(next);
		ArgumentOutOfRangeException.ThrowIfNegative(maximumCount);

		var retained = next
			.Distinct(StringComparer.Ordinal)
			.TakeLast(maximumCount)
			.ToArray();
		var retainedSet = retained.ToHashSet(StringComparer.Ordinal);
		var latestSet = latest.ToHashSet(StringComparer.Ordinal);
		return new AppNotificationDeliveryReceiptChanges(
			latest.Where(receipt => !retainedSet.Contains(receipt)).Distinct(StringComparer.Ordinal).ToArray(),
			retained.Where(receipt => !latestSet.Contains(receipt)).ToArray());
	}
}
