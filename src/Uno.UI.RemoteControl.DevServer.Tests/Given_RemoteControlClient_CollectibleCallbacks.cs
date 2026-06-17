using System;
using System.Collections.Generic;
using Uno.UI.RemoteControl;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class Given_RemoteControlClient_CollectibleCallbacks
{
	// Synthetic "collectibility" predicate: a real collectible ALC cannot be staged in a plain unit
	// test, so we classify by method name instead and verify the invocation-list filtering itself.
	private static readonly Func<Action<RemoteControlClient>, bool> IsCollectible =
		static a => a.Method.Name.StartsWith("Drop", StringComparison.Ordinal);

	private static void Keep1(RemoteControlClient _)
	{
	}

	private static void Keep2(RemoteControlClient _)
	{
	}

	private static void Drop1(RemoteControlClient _)
	{
	}

	private static void Drop2(RemoteControlClient _)
	{
	}

	[TestMethod]
	public void FilterCollectibleInvocations_WhenSingleNonCollectible_ReturnsSameInstance()
	{
		Action<RemoteControlClient> action = Keep1;

		var result = RemoteControlClient.FilterCollectibleInvocations(action, IsCollectible);

		result.Should().BeSameAs(action);
	}

	[TestMethod]
	public void FilterCollectibleInvocations_WhenSingleCollectible_ReturnsNull()
	{
		Action<RemoteControlClient> action = Drop1;

		var result = RemoteControlClient.FilterCollectibleInvocations(action, IsCollectible);

		result.Should().BeNull();
	}

	[TestMethod]
	public void FilterCollectibleInvocations_WhenAllEntriesCollectible_ReturnsNull()
	{
		Action<RemoteControlClient> action = Drop1;
		action += Drop2;

		var result = RemoteControlClient.FilterCollectibleInvocations(action, IsCollectible);

		result.Should().BeNull();
	}

	[TestMethod]
	public void FilterCollectibleInvocations_WhenNoEntryCollectible_ReturnsSameInstance()
	{
		Action<RemoteControlClient> action = Keep1;
		action += Keep2;

		var result = RemoteControlClient.FilterCollectibleInvocations(action, IsCollectible);

		result.Should().BeSameAs(action);
	}

	[TestMethod]
	public void FilterCollectibleInvocations_WhenMulticastMixed_KeepsOnlyNonCollectibleEntries()
	{
		// A combined delegate whose Target/Method reflect only one entry — the whole thing must not be
		// dropped (nor kept) wholesale; only the collectible entry should be removed.
		Action<RemoteControlClient> action = Keep1;
		action += Drop1;
		action += Keep2;

		var result = RemoteControlClient.FilterCollectibleInvocations(action, IsCollectible);

		result.Should().NotBeNull();
		var keptMethods = new List<string>();
		foreach (var entry in result!.GetInvocationList())
		{
			keptMethods.Add(entry.Method.Name);
		}

		keptMethods.Should().Equal(nameof(Keep1), nameof(Keep2));
	}
}
