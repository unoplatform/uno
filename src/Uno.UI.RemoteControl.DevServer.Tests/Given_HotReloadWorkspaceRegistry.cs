extern alias RemoteServerCore;

using System;
using System.Threading;
using Microsoft.Extensions.Options;

using RemoteServerCore::Uno.UI.RemoteControl.Server;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public sealed class Given_HotReloadWorkspaceRegistry
{
	private sealed class FakeSlot : IHotReloadWorkspaceSlot
	{
		public int DisableCount;
		public string? LastReason;

		public void RequestDisable(string reason)
		{
			Interlocked.Increment(ref DisableCount);
			LastReason = reason;
		}
	}

	// Mutable IOptionsMonitor to exercise live capacity changes (IOptionsMonitor.OnChange).
	private sealed class MutableOptionsMonitor<T>(T value) : IOptionsMonitor<T>
	{
		private readonly List<Action<T, string?>> _listeners = new();
		public T CurrentValue { get; private set; } = value;
		public T Get(string? name) => CurrentValue;
		public IDisposable? OnChange(Action<T, string?> listener)
		{
			_listeners.Add(listener);
			return null;
		}
		public void Set(T value)
		{
			CurrentValue = value;
			foreach (var l in _listeners.ToArray())
			{
				l(value, null);
			}
		}
	}

	private static HotReloadWorkspaceRegistry CreateRegistry(int capacity)
		=> new(new MutableOptionsMonitor<HotReloadWorkspaceOptions>(new HotReloadWorkspaceOptions { MaxConcurrentWorkspaces = capacity }));

	[TestMethod]
	public void Registering_beyond_capacity_disables_oldest_first_and_keeps_the_newest()
	{
		var registry = CreateRegistry(2);
		var s1 = new FakeSlot();
		var s2 = new FakeSlot();
		var s3 = new FakeSlot();

		registry.Register(s1);
		registry.Register(s2);

		s1.DisableCount.Should().Be(0);
		s2.DisableCount.Should().Be(0);
		registry.Count.Should().Be(2);

		registry.Register(s3); // over capacity -> oldest (s1) is disabled

		s1.DisableCount.Should().Be(1, "the oldest workspace is disabled when capacity is exceeded");
		s1.LastReason.Should().Contain("2", "the reason mentions the capacity that was reached");
		s2.DisableCount.Should().Be(0);
		s3.DisableCount.Should().Be(0);
		registry.Count.Should().Be(2, "the registry keeps exactly Capacity active slots");
	}

	[TestMethod]
	public void Unregister_frees_a_slot_so_a_later_registration_does_not_evict()
	{
		var registry = CreateRegistry(1);
		var s1 = new FakeSlot();
		var s2 = new FakeSlot();

		registry.Register(s1);
		registry.Unregister(s1); // e.g. the connection closed
		registry.Register(s2);

		s1.DisableCount.Should().Be(0, "s1 was already unregistered and must not be disabled");
		s2.DisableCount.Should().Be(0, "the registry is under capacity after s1 left");
		registry.Count.Should().Be(1);
	}

	[TestMethod]
	public void Registering_the_same_slot_twice_is_idempotent()
	{
		var registry = CreateRegistry(2);
		var s1 = new FakeSlot();

		registry.Register(s1);
		registry.Register(s1);

		registry.Count.Should().Be(1);
		s1.DisableCount.Should().Be(0);
	}

	[TestMethod]
	public void Lowering_capacity_live_evicts_the_surplus_oldest_immediately()
	{
		var monitor = new MutableOptionsMonitor<HotReloadWorkspaceOptions>(new HotReloadWorkspaceOptions { MaxConcurrentWorkspaces = 3 });
		var registry = new HotReloadWorkspaceRegistry(monitor);
		var s1 = new FakeSlot();
		var s2 = new FakeSlot();
		var s3 = new FakeSlot();

		registry.Register(s1);
		registry.Register(s2);
		registry.Register(s3);
		registry.Count.Should().Be(3);
		(s1.DisableCount + s2.DisableCount + s3.DisableCount).Should().Be(0);

		// Live capacity drop 3 -> 1: the two oldest (s1, s2) must be evicted immediately,
		// without waiting for the next registration. See PR review / #24205.
		monitor.Set(new HotReloadWorkspaceOptions { MaxConcurrentWorkspaces = 1 });

		s1.DisableCount.Should().Be(1, "the oldest is evicted on a live capacity drop");
		s2.DisableCount.Should().Be(1, "the second oldest is evicted too");
		s3.DisableCount.Should().Be(0, "the newest is kept");
		registry.Count.Should().Be(1);
	}
}
