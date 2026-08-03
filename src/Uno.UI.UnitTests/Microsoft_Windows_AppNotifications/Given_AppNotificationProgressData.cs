#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationProgressData
{
	[TestMethod]
	public void When_Created_Defaults_Match_Windows_App_Sdk()
	{
		var progress = new AppNotificationProgressData(1);

		Assert.AreEqual(1u, progress.SequenceNumber);
		Assert.AreEqual(string.Empty, progress.Title);
		Assert.AreEqual(0d, progress.Value);
		Assert.AreEqual(string.Empty, progress.ValueStringOverride);
		Assert.AreEqual(string.Empty, progress.Status);
	}

	[TestMethod]
	public void When_Sequence_Number_Is_Zero_It_Throws()
	{
		Assert.ThrowsExactly<ArgumentException>(() => new AppNotificationProgressData(0));

		var progress = new AppNotificationProgressData(1);

		Assert.ThrowsExactly<ArgumentException>(() => progress.SequenceNumber = 0);
	}

	[TestMethod]
	public void When_Values_Are_Changed_They_Round_Trip()
	{
		var progress = new AppNotificationProgressData(1)
		{
			SequenceNumber = 2,
			Title = "title",
			Value = 1.25,
			ValueStringOverride = "5/4",
			Status = "status",
		};

		Assert.AreEqual(2u, progress.SequenceNumber);
		Assert.AreEqual("title", progress.Title);
		Assert.AreEqual(1.25, progress.Value);
		Assert.AreEqual("5/4", progress.ValueStringOverride);
		Assert.AreEqual("status", progress.Status);
	}
}
