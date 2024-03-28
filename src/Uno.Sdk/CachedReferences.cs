using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable
namespace Uno.Sdk;

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

	public bool NeedsUpdate(IEnumerable<UnoFeature> currentFeatures, string unoVersion)
	{
		if (Updated.AddDays(1) < DateTimeOffset.Now
			|| Features.Length != currentFeatures.Count())
		{
			return true;
		}

		var unoReference = References.SingleOrDefault(r => r.PackageId == "Uno.WinUI");
		if (unoReference is null || unoReference.Version != unoVersion)
		{
			return true;
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
