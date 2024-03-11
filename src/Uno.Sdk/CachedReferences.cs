using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

#nullable enable
namespace Uno.Sdk;

internal record CachedReferences(DateTimeOffset Updated, UnoFeature[] Features, PackageReference[] References)
{
	private const string CacheFileName = "implicit-packages.cache";
	private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.General)
	{
		WriteIndented = true
	};

	public bool NeedsUpdate(IEnumerable<UnoFeature> currentFeatures)
	{
		if (Updated.AddDays(1) < DateTimeOffset.Now
			|| Features.Length != currentFeatures.Count())
		{
			return true;
		}

		return currentFeatures.Any(x => !Features.Contains(x));
	}

	public void SaveCache(string intermediateOutput)
	{
		var json = JsonSerializer.Serialize(this, _options);
		File.WriteAllText(Path.Combine(intermediateOutput, CacheFileName), json);
	}

	public static CachedReferences Load(string intermediateOutput)
	{
		var path = Path.Combine(intermediateOutput, CacheFileName);
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
}
