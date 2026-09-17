#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_LegacyToastNotifications
{
	[TestMethod]
	[DataRow(ToastTemplateType.ToastImageAndText01, 1, 1)]
	[DataRow(ToastTemplateType.ToastImageAndText02, 2, 1)]
	[DataRow(ToastTemplateType.ToastImageAndText03, 2, 1)]
	[DataRow(ToastTemplateType.ToastImageAndText04, 3, 1)]
	[DataRow(ToastTemplateType.ToastText01, 1, 0)]
	[DataRow(ToastTemplateType.ToastText02, 2, 0)]
	[DataRow(ToastTemplateType.ToastText03, 2, 0)]
	[DataRow(ToastTemplateType.ToastText04, 3, 0)]
	public void When_Template_Content_Is_Created_Legacy_Shape_Is_Returned(ToastTemplateType type, int textCount, int imageCount)
	{
		var content = ToastNotificationManager.GetTemplateContent(type);

		var document = XDocument.Parse(content.GetXml());
		var binding = document.Root!.Element("visual")!.Element("binding")!;
		Assert.AreEqual(type.ToString(), binding.Attribute("template")?.Value);
		Assert.AreEqual(textCount, binding.Elements("text").Count());
		Assert.AreEqual(imageCount, binding.Elements("image").Count());
	}

	[TestMethod]
	public void When_Legacy_Toast_Is_Shown_AppNotification_Pipeline_Is_Used()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		var expiration = DateTimeOffset.UtcNow.AddHours(1);
		var toast = CreateToast("tag", "group");
		toast.ExpirationTime = expiration;
		toast.ExpiresOnReboot = true;
		toast.Priority = ToastNotificationPriority.High;
		toast.SuppressPopup = true;

		notifier.Show(toast);

		var shown = backend.Shown.Single();
		Assert.AreEqual(1u, toast.AppNotificationId);
		Assert.AreEqual("tag", shown.Tag);
		Assert.AreEqual("group", shown.Group);
		Assert.AreEqual(expiration, shown.Expiration);
		Assert.IsTrue(shown.ExpiresOnReboot);
		Assert.AreEqual(AppNotificationPriority.High, shown.Priority);
		Assert.IsTrue(shown.SuppressDisplay);
		Assert.AreEqual("Title", shown.Payload.Title?.Content);
		Assert.AreEqual("Body", shown.Payload.Body?.Content);

	}

	[TestMethod]
	public void When_Manager_Is_Unregistered_Legacy_Show_Can_Initialize_Posting_Again()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		manager.Register();
		manager.Unregister();

		notifier.Show(CreateToast("tag", string.Empty));

		Assert.AreEqual(1, backend.Shown.Count);
	}

	[TestMethod]
	public void When_Legacy_Tag_And_Group_Match_Existing_Toast_It_Is_Replaced()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		var history = new ToastNotificationHistory(manager);
		var first = CreateToast("tag", "group");
		var replacement = CreateToast("tag", "group");

		notifier.Show(first);
		notifier.Show(replacement);

		Assert.AreEqual(first.AppNotificationId, replacement.AppNotificationId);
		Assert.AreEqual(1, backend.Shown.Count);
		Assert.AreEqual(1, backend.Updated.Count);
		Assert.AreEqual(1, history.GetHistory().Count);
	}

	[TestMethod]
	public void When_Legacy_Replacement_Is_Rejected_Previous_Identity_Is_Preserved()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		var toast = CreateToast("tag", "group");
		notifier.Show(toast);
		var originalId = toast.AppNotificationId;
		backend.AcceptUpdate = false;

		notifier.Show(toast);

		Assert.AreEqual(originalId, toast.AppNotificationId);
		Assert.AreEqual(1, backend.Updated.Count);
	}

	[TestMethod]
	public void When_Legacy_History_Is_Queried_Toast_Identity_And_Content_RoundTrip()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		var history = new ToastNotificationHistory(manager);
		var toast = CreateToast("tag", "group");
		notifier.Show(toast);

		var restored = history.GetHistory().Single();

		Assert.AreEqual(toast.AppNotificationId, restored.AppNotificationId);
		Assert.AreEqual("tag", restored.Tag);
		Assert.AreEqual("group", restored.Group);
		StringAssert.Contains(restored.Content.GetXml(), "template=\"ToastText02\"");
		StringAssert.Contains(restored.Content.GetXml(), "Title");
	}

	[TestMethod]
	public void When_Legacy_History_Contains_An_Untagged_Toast_It_RoundTrips()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		var history = new ToastNotificationHistory(manager);
		var content = new XmlDocument();
		content.LoadXml("<toast><visual><binding template='ToastText01'><text id='1'>Body</text></binding></visual></toast>");

		notifier.Show(new ToastNotification(content));

		var restored = history.GetHistory().Single();
		Assert.AreEqual(string.Empty, restored.Tag);
		StringAssert.Contains(restored.Content.GetXml(), "Body");
	}

	[TestMethod]
	public void When_Legacy_History_Selectors_Are_Used_Matching_Records_Are_Removed()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		var history = new ToastNotificationHistory(manager);
		var first = CreateToast("first", "group");
		var second = CreateToast("second", "group");
		var third = CreateToast("third", "other");
		notifier.Show(first);
		notifier.Show(second);
		notifier.Show(third);

		history.Remove("first", "group");
		history.RemoveGroup("group");
		history.Clear();

		CollectionAssert.AreEqual(
			new[] { first.AppNotificationId, second.AppNotificationId, third.AppNotificationId },
			backend.Removed.Select(record => record.Id).ToArray());
		Assert.AreEqual(0, history.GetHistory().Count);
	}

	[TestMethod]
	public void When_Legacy_Tag_Only_History_Remove_Is_Used_Only_The_Default_Group_Is_Removed()
	{
		var backend = new LegacyTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notifier = new ToastNotifier(manager);
		var history = new ToastNotificationHistory(manager);
		var defaultGroup = CreateToast("tag", string.Empty);
		var namedGroup = CreateToast("tag", "named");
		notifier.Show(defaultGroup);
		notifier.Show(namedGroup);

		history.Remove("tag");

		CollectionAssert.AreEqual(new[] { defaultGroup.AppNotificationId }, backend.Removed.Select(record => record.Id).ToArray());
		CollectionAssert.AreEqual(new[] { namedGroup.AppNotificationId }, history.GetHistory().Select(toast => toast.AppNotificationId).ToArray());
	}

	[TestMethod]
	public void When_AppNotification_Setting_Is_Read_Legacy_Setting_Is_Mapped()
	{
		var backend = new LegacyTestBackend { Setting = AppNotificationSetting.DisabledByManifest };
		var notifier = new ToastNotifier(new AppNotificationManager(backend));

		Assert.AreEqual(NotificationSetting.DisabledByManifest, notifier.Setting);
	}

	[TestMethod]
	public void When_Legacy_Surface_Is_Inspected_Implemented_Members_Lose_NotImplemented_Marker()
	{
		var show = typeof(ToastNotifier).GetMethod(nameof(ToastNotifier.Show))!;
		var hide = typeof(ToastNotifier).GetMethod(nameof(ToastNotifier.Hide))!;
		var schedule = typeof(ToastNotifier).GetMethod(nameof(ToastNotifier.AddToSchedule))!;
		var update = typeof(ToastNotifier).GetMethods().Single(method => method.Name == nameof(ToastNotifier.Update) && method.GetParameters().Length == 2);
		var crossApp = typeof(ToastNotificationManager).GetMethod(nameof(ToastNotificationManager.CreateToastNotifier), new[] { typeof(string) })!;

		Assert.IsFalse(HasNotImplementedAttribute(show));
		Assert.IsTrue(HasNotImplementedAttribute(hide));
		Assert.IsFalse(HasNotImplementedAttribute(schedule));
		Assert.IsTrue(HasNotImplementedAttribute(update));
		Assert.IsTrue(HasNotImplementedAttribute(crossApp));
	}

	[TestMethod]
	public void When_Legacy_Arguments_Are_Invalid_They_Are_Rejected_At_The_Boundary()
	{
		Assert.ThrowsExactly<ArgumentException>(() => new ToastNotification(null!));
		var toast = CreateToast("tag", "group");
		Assert.ThrowsExactly<ArgumentException>(() => toast.Tag = string.Empty);
		Assert.ThrowsExactly<ArgumentNullException>(() => toast.Tag = null!);
		Assert.ThrowsExactly<ArgumentException>(() => toast.Tag = new string('t', 65));
		Assert.ThrowsExactly<ArgumentNullException>(() => toast.Group = null!);
		Assert.ThrowsExactly<ArgumentException>(() => toast.Group = new string('g', 65));

		var history = new ToastNotificationHistory(new AppNotificationManager(new LegacyTestBackend()));
		Assert.ThrowsExactly<ArgumentException>(() => history.Remove(string.Empty));
		Assert.ThrowsExactly<ArgumentException>(() => history.RemoveGroup(string.Empty));
		history.Remove("tag", string.Empty);
	}

	private static ToastNotification CreateToast(string tag, string group)
	{
		var content = new XmlDocument();
		content.LoadXml("<toast><visual><binding template='ToastText02'><text id='1'>Title</text><text id='2'>Body</text></binding></visual></toast>");
		return new ToastNotification(content)
		{
			Tag = tag,
			Group = group,
		};
	}

	private static bool HasNotImplementedAttribute(MemberInfo member)
		=> member.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "Uno.NotImplementedAttribute");

	private sealed class LegacyTestBackend : IAppNotificationManagerBackend
	{
		public bool IsSupported => true;

		public AppNotificationSetting Setting { get; set; } = AppNotificationSetting.Enabled;

		public string? BootIdentifier => "boot";

		public List<AppNotificationEnvelope> Shown { get; } = new();

		public List<AppNotificationStateRecord> Removed { get; } = new();

		public List<AppNotificationStateRecord> Updated { get; } = new();

		public bool AcceptUpdate { get; set; } = true;

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

		public bool TryShow(AppNotificationEnvelope notification)
		{
			Shown.Add(notification);
			return true;
		}

		public bool TryUpdate(AppNotificationStateRecord notification)
		{
			Updated.Add(notification);
			return AcceptUpdate;
		}

		public void Remove(AppNotificationStateRecord notification) => Removed.Add(notification);

		public void RemoveAll()
		{
		}

		public IReadOnlyCollection<uint>? GetActiveNotificationIds() => null;
	}
}
