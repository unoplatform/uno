using AwesomeAssertions;
using Uno.UI.Tasks.IcuData;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_IcuDataSplitTask
{
	private static readonly IcuDataEntry[] _entries =
	[
		new("icudt77l/brkitr/line.brk", Bytes(37, 1)),
		new("icudt77l/brkitr/root.res", Bytes(20, 2)),
		new("icudt77l/brkitr/thaidict.dict", Bytes(50, 3)),
		new("icudt77l/cnvalias.icu", Bytes(9, 4)),
		new("icudt77l/en.res", Bytes(33, 5)),
		new("icudt77l/zh.res", Bytes(16, 6)),
	];

	[TestMethod]
	public void When_Split_Then_Startup_Keeps_Break_Rules_And_Tables_Only()
	{
		var (startup, dictionaries) = Split();

		startup.Select(e => e.Name).Should().Equal("icudt77l/brkitr/line.brk", "icudt77l/brkitr/root.res", "icudt77l/cnvalias.icu");
		dictionaries.Select(e => e.Name).Should().Equal("icudt77l/brkitr/thaidict.dict");
	}

	[TestMethod]
	public void When_Split_Then_Payloads_Survive()
	{
		var (startup, dictionaries) = Split();

		foreach (var entry in startup.Concat(dictionaries))
		{
			var original = _entries.Single(e => e.Name == entry.Name).Data;
			entry.Data.Take(original.Length).Should().Equal(original);
			entry.Data.Skip(original.Length).Should().OnlyContain(b => b == 0, "only alignment padding may follow the payload");
		}
	}

	[TestMethod]
	public void When_Split_Then_Items_Are_16_Byte_Aligned()
	{
		var folder = Directory.CreateTempSubdirectory("uno-icu-split-");
		try
		{
			var (startupPath, _) = RunTask(folder);
			var data = File.ReadAllBytes(startupPath);
			var headerSize = BitConverter.ToUInt16(data, 0);
			var count = BitConverter.ToUInt32(data, headerSize);
			for (var i = 0; i < count; i++)
			{
				var offset = headerSize + BitConverter.ToUInt32(data, headerSize + 8 + i * 8);
				(offset % 16).Should().Be(0, "ICU maps items in place and requires 16-byte alignment");
			}
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	[TestMethod]
	public void When_Outputs_Up_To_Date_Then_Not_Rewritten()
	{
		var folder = Directory.CreateTempSubdirectory("uno-icu-split-");
		try
		{
			var (startupPath, dictionariesPath) = RunTask(folder);
			var stamp = File.GetLastWriteTimeUtc(Path.Combine(folder.FullName, "icudt.dat")).AddMinutes(1);
			File.SetLastWriteTimeUtc(startupPath, stamp);
			File.SetLastWriteTimeUtc(dictionariesPath, stamp);

			new IcuDataSplitTask_v0
			{
				BuildEngine = new RecordingBuildEngine(),
				SourcePath = Path.Combine(folder.FullName, "icudt.dat"),
				StartupOutputPath = startupPath,
				DictionariesOutputPath = dictionariesPath,
			}.Execute().Should().BeTrue();

			File.GetLastWriteTimeUtc(startupPath).Should().Be(stamp, "a rewritten embedded resource recompiles the app");
			File.GetLastWriteTimeUtc(dictionariesPath).Should().Be(stamp);
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	private static (IReadOnlyList<IcuDataEntry> Startup, IReadOnlyList<IcuDataEntry> Dictionaries) Split()
	{
		var folder = Directory.CreateTempSubdirectory("uno-icu-split-");
		try
		{
			var (startupPath, dictionariesPath) = RunTask(folder);
			return (IcuDataSplitTask_v0.ReadEntries(File.ReadAllBytes(startupPath), out _), IcuDataSplitTask_v0.ReadEntries(File.ReadAllBytes(dictionariesPath), out _));
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	private static (string Startup, string Dictionaries) RunTask(DirectoryInfo folder)
	{
		var source = Path.Combine(folder.FullName, "icudt.dat");
		IcuDataSplitTask_v0.WritePackage(source, Header(), _entries);

		var task = new IcuDataSplitTask_v0
		{
			BuildEngine = new RecordingBuildEngine(),
			SourcePath = source,
			StartupOutputPath = Path.Combine(folder.FullName, "out", "icudt.dat"),
			DictionariesOutputPath = Path.Combine(folder.FullName, "out", "icudt-dict.dat"),
		};
		task.Execute().Should().BeTrue();

		return (task.StartupOutputPath, task.DictionariesOutputPath);
	}

	// Minimal common-data header: size, then the 0xDA27 magic ICU checks.
	private static byte[] Header()
	{
		var header = new byte[32];
		BitConverter.GetBytes((ushort)header.Length).CopyTo(header, 0);
		header[2] = 0xDA;
		header[3] = 0x27;
		return header;
	}

	private static byte[] Bytes(int length, byte seed) => Enumerable.Range(0, length).Select(i => (byte)(seed + i)).ToArray();
}
