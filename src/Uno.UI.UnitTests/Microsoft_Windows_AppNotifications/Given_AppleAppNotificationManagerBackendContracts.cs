#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleAppNotificationManagerBackendContracts
{
	[TestMethod]
	public async Task When_Apple_Progress_Is_Updated_Result_Is_Unsupported()
	{
		var backend = new TestAppleBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		manager.Register();
		var notification = CreateNotification();
		manager.Show(notification);

		var result = await manager.UpdateAsync(
			new Microsoft.Windows.AppNotifications.AppNotificationProgressData(1),
			notification.Tag);

		Assert.AreEqual(AppNotificationProgressResult.Unsupported, result);
		Assert.AreEqual(0, backend.UpdateCount);
	}

	[TestMethod]
	public async Task When_Awaited_Apple_RemoveAll_Is_Not_Acknowledged_Durable_History_Remains()
	{
		var backend = new TestAppleBackend { AcknowledgeRemoval = false };
		var persistence = new InMemoryAppNotificationStatePersistence();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var notification = CreateNotification();
		manager.Show(notification);

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => manager.RemoveAllAsync().AsTask());

		var durable = persistence.Load();
		Assert.AreEqual(1, backend.RemoveCount);
		Assert.AreEqual(1, durable.Records.Count);
		Assert.AreEqual(AppNotificationPostingState.Removing, durable.Records[0].PostingState);
	}

	private static AppNotification CreateNotification()
		=> new("<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>")
		{
			Tag = "tag",
		};

	private sealed class TestAppleBackend : IAppNotificationManagerBackend, IAppNotificationProgressUpdateCapability
	{
		public bool IsSupported => true;

		public AppNotificationSetting Setting => AppNotificationSetting.Enabled;

		public string? BootIdentifier => null;

		public bool SupportsProgressUpdates => AppleAppNotificationCapabilities.SupportsProgressUpdates;

		public bool AcknowledgeRemoval { get; set; } = true;

		public int UpdateCount { get; private set; }

		public int RemoveCount { get; private set; }

		public void Register()
		{
		}

		public void Register(string displayName, Uri iconUri)
		{
		}

		public void Unregister()
		{
		}

		public void UnregisterAll()
		{
		}

		public bool TryShow(AppNotificationEnvelope notification) => true;

		public bool TryUpdate(AppNotificationStateRecord notification)
		{
			UpdateCount++;
			return true;
		}

		public void Remove(AppNotificationStateRecord notification)
		{
			RemoveCount++;
			if (!AcknowledgeRemoval)
			{
				throw new InvalidOperationException("Native removal was not acknowledged.");
			}
		}

		public void RemoveAll()
		{
		}

		public IReadOnlyCollection<uint>? GetActiveNotificationIds() => null;
	}
}
