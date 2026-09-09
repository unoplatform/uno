#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private string _folderPath = null!;
	private string _filePath = null!;

	private static partial bool SupportsLocalityPlatform() => true;

	private partial Dictionary<string, string> LoadPlatform()
	{
		var settingsFolderPath = ApplicationData.Current.GetSettingsFolderPath();

		_folderPath = settingsFolderPath;
		_filePath = Path.Combine(settingsFolderPath, $"{_locality}.dat");

		return ReadFromFile();
	}

	private partial void SetSettingPlatform(string key, string value) => WriteToFile();

	private partial void RemoveSettingsPlatform(IReadOnlyCollection<string> keys) => WriteToFile();

	private Dictionary<string, string> ReadFromFile()
	{
		var settings = new Dictionary<string, string>();

		if (!File.Exists(_filePath))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"File {_filePath} does not exist, skipping reading settings");
			}

			return settings;
		}

		try
		{
			using var reader = new BinaryReader(File.OpenRead(_filePath));

			var count = reader.ReadInt32();

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Reading {count} settings values");
			}

			for (int i = 0; i < count; i++)
			{
				var key = reader.ReadString();
				var value = reader.ReadString();

				settings[key] = value;
			}

			return settings;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to read settings from {_filePath}, starting from an empty store", e);
			}

			QuarantineFile();

			// A part-read must not be published: the next write would persist it over the intact file.
			return new Dictionary<string, string>();
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

			var settings = Settings;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Writing {settings.Count} settings to {_filePath}");
			}

			// Written to a temporary file and swapped in, so that a crash or a full disk part-way through
			// leaves the previous store intact instead of a truncated one.
			var temporaryPath = _filePath + ".tmp";

			using (var writer = new BinaryWriter(File.Create(temporaryPath)))
			{
				writer.Write(settings.Count);

				foreach (var pair in settings)
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
