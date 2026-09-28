#nullable enable

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Storage.Internal;

namespace Uno.UI.Tests.Windows_Storage;

[TestClass]
public class Given_LegacySettingsMigration
{
	[TestMethod]
	public void When_Legacy_Entries_Then_Moved_And_Native_Keys_Kept()
	{
		FakeStore source = new()
		{
			["Counter"] = "System.Int32:42",
			["Name"] = "System.String:Uno",
			["NativeKey"] = "plain native value",
			["NotAType"] = "Foo.Bar:baz",
		};
		FakeStore target = new();

		var migrated = LegacySettingsMigration.Migrate(source, target);

		Assert.AreEqual(2, migrated);
		CollectionAssert.AreEquivalent(new[] { "NativeKey", "NotAType" }, source.PersistedKeys);
		Assert.AreEqual("System.Int32:42", target.Persisted["Counter"]);
		Assert.AreEqual("System.String:Uno", target.Persisted["Name"]);
	}

	[TestMethod]
	public void When_Key_Exists_In_Target_Then_Target_Value_Wins()
	{
		FakeStore source = new() { ["Counter"] = "System.Int32:1" };
		FakeStore target = new() { ["Counter"] = "System.Int32:2" };

		var migrated = LegacySettingsMigration.Migrate(source, target);

		Assert.AreEqual(1, migrated);
		Assert.AreEqual("System.Int32:2", target.Persisted["Counter"]);
		Assert.AreEqual(0, source.PersistedKeys.Count);
	}

	[TestMethod]
	public void When_Nothing_To_Migrate_Then_Nothing_Is_Flushed()
	{
		FakeStore source = new() { ["NativeKey"] = "value" };
		FakeStore target = new();

		var migrated = LegacySettingsMigration.Migrate(source, target);

		Assert.AreEqual(0, migrated);
		Assert.AreEqual(0, source.SynchronizeCount);
		Assert.AreEqual(0, target.SynchronizeCount);
	}

	[TestMethod]
	public void When_Target_Flush_Fails_Then_Source_Is_Preserved()
	{
		FakeStore source = new() { ["Counter"] = "System.Int32:42" };
		FakeStore target = new() { FailSynchronize = true };

		Assert.ThrowsExactly<IOException>(() => LegacySettingsMigration.Migrate(source, target));

		Assert.AreEqual("System.Int32:42", source.Persisted["Counter"]);
		Assert.AreEqual(0, source.SynchronizeCount);
		Assert.IsFalse(target.Persisted.ContainsKey("Counter"));
	}

	[TestMethod]
	public void When_Target_Flush_Fails_Then_Retry_Completes()
	{
		FakeStore source = new() { ["Counter"] = "System.Int32:42" };
		FakeStore target = new() { FailSynchronize = true };

		Assert.ThrowsExactly<IOException>(() => LegacySettingsMigration.Migrate(source, target));

		target.FailSynchronize = false;
		var migrated = LegacySettingsMigration.Migrate(source, target);

		Assert.AreEqual(1, migrated);
		Assert.AreEqual(0, source.PersistedKeys.Count);
		Assert.AreEqual("System.Int32:42", target.Persisted["Counter"]);
	}

	[TestMethod]
	public void When_Source_Flush_Fails_Then_Retry_Keeps_Target_Value()
	{
		FakeStore source = new() { ["Counter"] = "System.Int32:1", FailSynchronize = true };
		FakeStore target = new();

		Assert.AreEqual(1, LegacySettingsMigration.Migrate(source, target));
		Assert.AreEqual("System.Int32:1", source.Persisted["Counter"]);

		source.Discard();
		target["Counter"] = "System.Int32:2";
		target.Synchronize();
		source.FailSynchronize = false;

		Assert.AreEqual(1, LegacySettingsMigration.Migrate(source, target));
		Assert.AreEqual(0, source.PersistedKeys.Count);
		Assert.AreEqual("System.Int32:2", target.Persisted["Counter"]);
	}

	/// <summary>
	/// Keeps unsaved changes apart from the persisted state, like a user defaults domain that may fail to flush.
	/// </summary>
	private sealed class FakeStore : ISettingsMigrationStore
	{
		private Dictionary<string, object> _pending = new();

		public Dictionary<string, object> Persisted { get; private set; } = new();

		public List<string> PersistedKeys => new(Persisted.Keys);

		public bool FailSynchronize { get; set; }

		public int SynchronizeCount { get; private set; }

		public object this[string key]
		{
			set
			{
				_pending[key] = value;
				Persisted[key] = value;
			}
		}

		public IEnumerable<KeyValuePair<string, object>> Entries => _pending;

		public bool ContainsKey(string key) => _pending.ContainsKey(key);

		public void SetValue(string key, object value) => _pending[key] = value;

		public void Remove(string key) => _pending.Remove(key);

		public bool Synchronize()
		{
			SynchronizeCount++;

			if (FailSynchronize)
			{
				return false;
			}

			Persisted = new(_pending);
			return true;
		}

		// Simulates a relaunch: unsaved changes are lost.
		public void Discard() => _pending = new(Persisted);
	}
}
