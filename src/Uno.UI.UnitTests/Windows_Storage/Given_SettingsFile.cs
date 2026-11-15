#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Storage.Internal;

namespace Uno.UI.Tests.Windows_Storage;

// Each SettingsFile instance stands in for one process of the app: they share nothing but the file.
[UITestClass]
public class Given_SettingsFile
{
	private string _folder = null!;
	private string _path = null!;

	[TestInitialize]
	public void Initialize()
	{
		_folder = Path.Combine(Path.GetTempPath(), "uno-settings-" + Guid.NewGuid().ToString("N"));
		_path = Path.Combine(_folder, "Local.dat");
	}

	[TestCleanup]
	public void Cleanup()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	[TestMethod]
	public void When_Two_Instances_Write_Different_Keys_Then_Both_Are_Kept()
	{
		var a = new SettingsFile(_path);
		var b = new SettingsFile(_path);

		a.Set("fromA", "1");
		b.Set("fromB", "1");

		var reopened = new SettingsFile(_path);
		Assert.AreEqual("1", reopened.Values["fromA"]);
		Assert.AreEqual("1", reopened.Values["fromB"]);
	}

	[TestMethod]
	public void When_Other_Instance_Removed_A_Key_Then_A_Later_Write_Does_Not_Bring_It_Back()
	{
		new SettingsFile(_path).Set("shared", "1");
		var a = new SettingsFile(_path);
		var b = new SettingsFile(_path);

		Assert.IsTrue(a.Remove("shared"));
		b.Set("other", "1");

		var reopened = new SettingsFile(_path);
		Assert.IsFalse(reopened.Values.ContainsKey("shared"));
		Assert.AreEqual("1", reopened.Values["other"]);
	}

	[TestMethod]
	public void When_Other_Instance_Writes_Then_Reads_See_It()
	{
		var a = new SettingsFile(_path);
		var b = new SettingsFile(_path);

		a.Set("key", "1");
		Assert.AreEqual("1", b.Values["key"]);

		a.Set("key", "22");
		Assert.AreEqual("22", b.Values["key"]);

		a.Remove("key");
		Assert.IsFalse(b.Values.ContainsKey("key"));
	}

	[TestMethod]
	public void When_Removing_A_Key_Another_Instance_Added_Then_It_Is_Reported_Removed()
	{
		var a = new SettingsFile(_path);
		var b = new SettingsFile(_path);

		a.Set("key", "1");

		Assert.IsTrue(b.Remove("key"));
		Assert.IsFalse(b.Remove("key"));
	}

	[TestMethod]
	public async Task When_Instances_Write_Concurrently_Then_No_Write_Is_Lost()
	{
		const int perInstance = 40;
		var a = new SettingsFile(_path);
		var b = new SettingsFile(_path);

		await Task.WhenAll(
			Task.Run(() => { for (var i = 0; i < perInstance; i++) { a.Set($"a{i}", "1"); } }),
			Task.Run(() => { for (var i = 0; i < perInstance; i++) { b.Set($"b{i}", "1"); } }));

		Assert.HasCount(2 * perInstance, new SettingsFile(_path).Values);
	}

	[TestMethod]
	public void When_File_Is_Damaged_Then_It_Is_Moved_Aside_Instead_Of_Overwritten()
	{
		Directory.CreateDirectory(_folder);
		var damaged = new byte[] { 5, 0, 0, 0, 3, (byte)'k' };
		File.WriteAllBytes(_path, damaged);

		var settings = new SettingsFile(_path);
		Assert.HasCount(0, settings.Values);

		settings.Set("key", "1");

		CollectionAssert.AreEqual(damaged, File.ReadAllBytes(_path + ".corrupt"));
		Assert.AreEqual("1", new SettingsFile(_path).Values["key"]);
	}

	[TestMethod]
	public void When_File_Is_Damaged_After_Loading_Then_It_Is_Rebuilt_From_The_Last_Good_Copy()
	{
		var settings = new SettingsFile(_path);
		settings.Set("kept", "1");
		File.WriteAllBytes(_path, new byte[] { 9, 0, 0, 0 });

		settings.Set("added", "1");

		var reopened = new SettingsFile(_path);
		Assert.AreEqual("1", reopened.Values["kept"]);
		Assert.AreEqual("1", reopened.Values["added"]);
	}

	[TestMethod]
	public void When_Count_Is_Larger_Than_The_File_Then_It_Is_Treated_As_Damaged()
	{
		Directory.CreateDirectory(_folder);
		File.WriteAllBytes(_path, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F, 1, (byte)'k', 1, (byte)'v' });

		var settings = new SettingsFile(_path);
		Assert.HasCount(0, settings.Values);

		settings.Set("key", "1");
		Assert.IsTrue(File.Exists(_path + ".corrupt"));
		Assert.AreEqual("1", new SettingsFile(_path).Values["key"]);
	}

	[TestMethod]
	public void When_File_Has_Trailing_Bytes_From_An_Older_Writer_Then_It_Is_Read()
	{
		Directory.CreateDirectory(_folder);
		using (var writer = new BinaryWriter(File.Create(_path)))
		{
			writer.Write(1);
			writer.Write("key");
			writer.Write("System.String:value");
			writer.Write(Encoding.UTF8.GetBytes("leftover from a longer file"));
		}

		Assert.AreEqual("System.String:value", new SettingsFile(_path).Values["key"]);
	}

	[TestMethod]
	public async Task When_Another_Instance_Holds_The_Lock_Then_The_Write_Waits_For_It()
	{
		var settings = new SettingsFile(_path);
		settings.Set("before", "1");

		var write = default(Task);
		using (HoldLock(exclusive: true))
		{
			write = Task.Run(() => settings.Set("after", "1"));
			await Task.Delay(200);
			Assert.IsFalse(write.IsCompleted);
		}

		await write;
		Assert.AreEqual("1", new SettingsFile(_path).Values["after"]);
	}

	[TestMethod]
	public async Task When_Another_Instance_Is_Reading_Then_The_Write_Waits_For_It()
	{
		// On Windows the file can't be replaced while another process has it open, so readers hold a shared lock.
		var settings = new SettingsFile(_path);
		settings.Set("before", "1");

		var write = default(Task);
		using (HoldLock(exclusive: false))
		{
			write = Task.Run(() => settings.Set("after", "1"));
			await Task.Delay(200);
			Assert.IsFalse(write.IsCompleted);
		}

		await write;
		Assert.AreEqual("1", new SettingsFile(_path).Values["after"]);
	}

	[TestMethod]
	public void When_The_Lock_Times_Out_Then_The_Change_Survives_Other_Writes_And_Is_Saved_Later()
	{
		var b = new SettingsFile(_path, writeLockTimeout: TimeSpan.FromMilliseconds(100));
		var a = new SettingsFile(_path);

		using (HoldLock(exclusive: true))
		{
			b.Set("unsaved", "1");
		}

		Assert.IsFalse(new SettingsFile(_path).Values.ContainsKey("unsaved"));

		a.Set("fromA", "1");
		Assert.AreEqual("1", b.Values["unsaved"]);
		Assert.AreEqual("1", b.Values["fromA"]);

		b.Set("later", "1");

		var reopened = new SettingsFile(_path);
		Assert.AreEqual("1", reopened.Values["unsaved"]);
		Assert.AreEqual("1", reopened.Values["fromA"]);
		Assert.AreEqual("1", reopened.Values["later"]);
	}

	[TestMethod]
	public void When_A_Rewrite_Keeps_The_Same_Length_And_Time_Then_Readers_Still_See_It()
	{
		var a = new SettingsFile(_path);
		var b = new SettingsFile(_path);
		a.Set("key", "1");
		var lastWrite = File.GetLastWriteTimeUtc(_path);
		Assert.AreEqual("1", b.Values["key"]);

		// The same stamp a coarse file system would record for a second write within its resolution.
		a.Set("key", "2");
		File.SetLastWriteTimeUtc(_path, lastWrite);

		Assert.AreEqual("2", b.Values["key"]);
	}

	[TestMethod]
	public void When_A_Change_Makes_No_Difference_Then_The_File_Is_Not_Rewritten()
	{
		var settings = new SettingsFile(_path);

		Assert.IsFalse(settings.Remove("missing"));
		Assert.IsFalse(File.Exists(_path));

		settings.Set("key", "1");
		var lastWrite = File.GetLastWriteTimeUtc(_path);
		File.SetLastWriteTimeUtc(_path, lastWrite.AddHours(-1));

		settings.Set("key", "1");
		Assert.IsFalse(settings.Remove("missing"));

		Assert.AreEqual(lastWrite.AddHours(-1), File.GetLastWriteTimeUtc(_path));
	}

	[TestMethod]
	public void When_Cleared_Then_File_Holds_No_Values()
	{
		var a = new SettingsFile(_path);
		a.Set("one", "1");
		new SettingsFile(_path).Set("two", "2");

		a.Clear();

		Assert.HasCount(0, new SettingsFile(_path).Values);
	}

	[TestMethod]
	public void When_Writing_Then_No_Temporary_File_Is_Left_Behind()
	{
		var settings = new SettingsFile(_path);
		settings.Set("key", "1");
		settings.Remove("key");

		var leftovers = Directory.GetFiles(_folder).Select(Path.GetFileName).Where(name => name!.EndsWith(".tmp", StringComparison.Ordinal));
		Assert.IsFalse(leftovers.Any());
	}

	private FileStream HoldLock(bool exclusive)
	{
		Directory.CreateDirectory(_folder);
		return exclusive
			? new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
			: new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
	}
}
