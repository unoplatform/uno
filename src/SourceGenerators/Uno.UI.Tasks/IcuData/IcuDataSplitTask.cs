#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace Uno.UI.Tasks.IcuData
{
	/// <summary>
	/// Splits an ICU common data package (icudt.dat) into a startup package and a dictionaries package.
	/// </summary>
	/// <remarks>
	/// Uno only uses ICU for break iteration and bidi, which read nothing outside brkitr/ besides the
	/// small *.icu tables. The brkitr dictionaries (~2 MB compressed) are only consulted for Thai, Lao,
	/// Khmer, Myanmar and CJK text, so on WebAssembly they ship as a separate file loaded on demand.
	/// </remarks>
	public class IcuDataSplitTask_v0 : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string SourcePath { get; set; } = "";

		[Required]
		public string StartupOutputPath { get; set; } = "";

		[Required]
		public string DictionariesOutputPath { get; set; } = "";

		public override bool Execute()
		{
			try
			{
				var entries = ReadEntries(File.ReadAllBytes(SourcePath), out var header);

				WritePackage(StartupOutputPath, header, entries.Where(IsStartupEntry));
				WritePackage(DictionariesOutputPath, header, entries.Where(IsDictionary));

				return true;
			}
			catch (Exception e)
			{
				Log.LogError($"Failed to split the ICU data {SourcePath}: {e.Message}");
				return false;
			}
		}

		public static bool IsDictionary(IcuDataEntry entry) => entry.Name.EndsWith(".dict", StringComparison.Ordinal);

		public static bool IsStartupEntry(IcuDataEntry entry)
			=> !IsDictionary(entry) && (entry.Name.Contains("/brkitr/") || entry.Name.EndsWith(".icu", StringComparison.Ordinal));

		/// <summary>
		/// Reads the table of contents of a common data package: a data header, an entry count, then
		/// (name offset, data offset) pairs relative to the end of the header. Entries are sorted by name.
		/// </summary>
		public static IReadOnlyList<IcuDataEntry> ReadEntries(byte[] data, out byte[] header)
		{
			var headerSize = BitConverter.ToUInt16(data, 0);
			if (data[2] != 0xDA || data[3] != 0x27)
			{
				throw new InvalidDataException("Not an ICU data file (bad magic).");
			}

			header = Slice(data, 0, headerSize);
			var count = (int)BitConverter.ToUInt32(data, headerSize);
			var offsets = new (int Name, int Data)[count];
			for (var i = 0; i < count; i++)
			{
				offsets[i] = ((int)BitConverter.ToUInt32(data, headerSize + 4 + i * 8), (int)BitConverter.ToUInt32(data, headerSize + 8 + i * 8));
			}

			var entries = new List<IcuDataEntry>(count);
			for (var i = 0; i < count; i++)
			{
				var nameStart = headerSize + offsets[i].Name;
				var name = Encoding.ASCII.GetString(data, nameStart, Array.IndexOf(data, (byte)0, nameStart) - nameStart);
				var start = headerSize + offsets[i].Data;
				var end = i + 1 < count ? headerSize + offsets[i + 1].Data : data.Length;
				entries.Add(new IcuDataEntry(name, Slice(data, start, end - start)));
			}

			return entries;
		}

		public static void WritePackage(string path, byte[] header, IEnumerable<IcuDataEntry> entries)
		{
			var items = entries.ToArray();
			var names = items.Select(e => Encoding.ASCII.GetBytes(e.Name + "\0")).ToArray();
			var tocSize = 4 + items.Length * 8;
			var namesSize = names.Sum(n => n.Length);
			// Items are 16-byte aligned, as ICU requires for the data it maps in place.
			var dataStart = Align16(tocSize + namesSize);

			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
			using var output = new BinaryWriter(File.Create(path));
			output.Write(header);
			output.Write((uint)items.Length);

			int nameOffset = tocSize, dataOffset = dataStart;
			for (var i = 0; i < items.Length; i++)
			{
				output.Write((uint)nameOffset);
				output.Write((uint)dataOffset);
				nameOffset += names[i].Length;
				dataOffset += Align16(items[i].Data.Length);
			}

			foreach (var name in names)
			{
				output.Write(name);
			}

			output.Write(new byte[dataStart - tocSize - namesSize]);
			foreach (var item in items)
			{
				output.Write(item.Data);
				output.Write(new byte[Align16(item.Data.Length) - item.Data.Length]);
			}
		}

		private static int Align16(int value) => (value + 15) & ~15;

		private static byte[] Slice(byte[] data, int start, int length)
		{
			var slice = new byte[length];
			Array.Copy(data, start, slice, 0, length);
			return slice;
		}
	}

	public sealed class IcuDataEntry
	{
		public IcuDataEntry(string name, byte[] data)
		{
			Name = name;
			Data = data;
		}

		public string Name { get; }

		public byte[] Data { get; }
	}
}
