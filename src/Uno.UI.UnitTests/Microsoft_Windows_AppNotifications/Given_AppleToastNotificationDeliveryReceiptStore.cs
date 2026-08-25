#nullable enable

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleToastNotificationDeliveryReceiptStore
{
	[TestMethod]
	public void When_Receipt_Is_Consumed_It_Is_Deleted()
	{
		var directory = CreateDirectory();
		var identifier = Guid.NewGuid().ToString("N");
		try
		{
			Assert.IsTrue(AppleToastNotificationDeliveryReceiptStore.TryPersist(identifier, directory));

			Assert.IsTrue(AppleToastNotificationDeliveryReceiptStore.TryConsume(identifier, directory));

			Assert.AreEqual(0, AppleToastNotificationDeliveryReceiptStore.GetIdentifiers(directory)!.Count);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	public void When_Startup_Cleanup_Runs_Only_Retained_Receipts_Remain()
	{
		var directory = CreateDirectory();
		var retained = Guid.NewGuid().ToString("N");
		var stale = Guid.NewGuid().ToString("N");
		try
		{
			Assert.IsTrue(AppleToastNotificationDeliveryReceiptStore.TryPersist(retained, directory));
			Assert.IsTrue(AppleToastNotificationDeliveryReceiptStore.TryPersist(stale, directory));
			File.WriteAllText(Path.Combine(directory, "invalid.receipt"), "stale");
			File.WriteAllText(Path.Combine(directory, "orphan.tmp"), "incomplete");

			Assert.IsTrue(AppleToastNotificationDeliveryReceiptStore.TryCleanup(new[] { retained }, directory));

			CollectionAssert.AreEqual(
				new[] { retained },
				AppleToastNotificationDeliveryReceiptStore.GetIdentifiers(directory)!.ToArray());
			Assert.AreEqual(1, Directory.GetFiles(directory, "*.receipt").Length);
			Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static string CreateDirectory()
	{
		var directory = Path.Combine(AppContext.BaseDirectory, "AppleReceiptTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return directory;
	}
}
