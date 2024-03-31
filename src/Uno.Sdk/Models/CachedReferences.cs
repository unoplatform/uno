using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uno.Sdk.Services;

namespace Uno.Sdk.Models;

internal record CachedReferences(DateTimeOffset Updated, UnoFeature[] Features, PackageReference[] References)
{
	private const string CacheFileName = "implicit-packages.cache";
	private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.General)
	{
		Converters =
		{
			new JsonStringEnumConverter()
		},
		WriteIndented = true
	};

	public bool NeedsUpdate(IEnumerable<UnoFeature> currentFeatures, PackageManifest manifest)
	{
		if (Updated.AddDays(1) < DateTimeOffset.Now
			|| Features.Length != currentFeatures.Count())
		{
			return true;
		}

		foreach (var reference in References)
		{
			var version = manifest.GetPackageVersion(reference.PackageId);
			if (string.IsNullOrEmpty(version) || !reference.Version.Equals(version, StringComparison.InvariantCulture))
			{
				return true;
			}
		}

		return currentFeatures.Any(x => !Features.Contains(x));
	}

	public void SaveCache(string intermediateOutput)
	{
		try
		{
			var json = JsonSerializer.Serialize(this, _options);
			if (!Directory.Exists(intermediateOutput))
			{
				Directory.CreateDirectory(intermediateOutput);
			}

			var path = CacheFilePath(intermediateOutput);
			File.WriteAllText(path, json);
		}
		catch
		{
			// Suppress errors: If we have an issue saving this should not affect the build.
		}
	}

	public static CachedReferences Load(string intermediateOutput)
	{
		var path = CacheFilePath(intermediateOutput);
		if (!File.Exists(path))
		{
			// We return an Invalid Feature to ensure we force an update.
			return new CachedReferences(default, [UnoFeature.Invalid], []);
		}

		try
		{
			var json = File.ReadAllText(path);
			return JsonSerializer.Deserialize<CachedReferences>(json, _options) ??
				throw new InvalidOperationException();
		}
		catch
		{
			return new CachedReferences(default, [UnoFeature.Invalid], []);
		}
	}

	private static string CacheFilePath(string intermediateOutput) =>
		Path.Combine(intermediateOutput, CacheFileName);
}
