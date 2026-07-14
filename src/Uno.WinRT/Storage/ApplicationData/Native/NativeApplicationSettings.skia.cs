#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private readonly Dictionary<string, string> _values = new();
	private string _folderPath = null!;
	private string _filePath = null!;

	private static partial bool SupportsLocalityPlatform() => true;

	partial void InitializePlatform()
	{
		var settingsFolderPath = ApplicationData.Current.GetSettingsFolderPath();

		_folderPath = settingsFolderPath;
		_filePath = Path.Combine(settingsFolderPath, $"{_locality}.dat");

		ReadFromFile();
	}

	private partial bool ContainsSettingPlatform(string key) => _values.ContainsKey(key);

	private partial bool RemoveSettingPlatform(string key)
	{
		var ret = _values.Remove(key);

		WriteToFile();

		return ret;
	}

	private partial IEnumerable<string> GetKeysPlatform() => _values.Keys;

	private partial bool TryGetSettingPlatform(string key, out string? value) => _values.TryGetValue(key, out value);

	private partial void SetSettingPlatform(string key, string value)
	{
		_values[key] = value;
		WriteToFile();
	}

	private void ReadFromFile()
	{
		if (!File.Exists(_filePath))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"File {_filePath} does not exist, skipping reading settings");
			}

			return;
		}

		try
		{
			// Read into a separate map: a failure part-way through must not leave the store holding a
			// partial set of settings, which the next write would then persist over the intact file.
			var values = new Dictionary<string, string>();

			using (var reader = new BinaryReader(File.OpenRead(_filePath)))
			{
				var count = reader.ReadInt32();

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Reading {count} settings values");
				}

				for (int i = 0; i < count; i++)
				{
					var key = reader.ReadString();
					var value = reader.ReadString();

					values[key] = value;
				}
			}

			_values.Clear();
			foreach (var pair in values)
			{
				_values[pair.Key] = pair.Value;
			}
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to read settings from {_filePath}, starting from an empty store", e);
			}

			QuarantineFile();
		}
	}

	/// <summary>
	/// Moves an unreadable store aside, so that the empty store this session starts from does not overwrite
	/// the only copy of the user's settings on the next write.
	/// </summary>
	private void QuarantineFile()
	{
		try
		{
			File.Move(_filePath, _filePath + ".corrupt", overwrite: true);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to move the unreadable settings file {_filePath} aside", e);
			}
		}
	}

	private void WriteToFile()
	{
		try
		{
			Directory.CreateDirectory(_folderPath);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Writing {_values.Count} settings to {_filePath}");
			}

			// Write to a temporary file and swap it in, so that a crash or a full disk part-way through
			// leaves the previous store intact instead of a truncated one.
			var temporaryPath = _filePath + ".tmp";

			using (var writer = new BinaryWriter(File.Create(temporaryPath)))
			{
				writer.Write(_values.Count);

				foreach (var pair in _values)
				{
					writer.Write(pair.Key);
					writer.Write(pair.Value ?? "");
				}
			}

			File.Move(temporaryPath, _filePath, overwrite: true);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to write settings to {_filePath}", e);
			}
		}
	}
}
