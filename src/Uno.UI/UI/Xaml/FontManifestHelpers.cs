#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI.Text;

namespace Uno.UI.Xaml.Media;

internal static class FontManifestHelpers
{
	private static readonly JsonSerializerOptions _options = new JsonSerializerOptions()
	{
		AllowTrailingCommas = true,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		ReadCommentHandling = JsonCommentHandling.Skip,
		Converters =
		{
			new JsonStringEnumConverter(),
		},
		TypeInfoResolver = FontManifestSerializerContext.Default,
	};

	internal static string GetFamilyNameFromManifest(Stream jsonStream, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		var manifest = JsonSerializer.Deserialize<FontManifest>(jsonStream, _options);
		return GetFamilyNameFromManifest(manifest, weight, style, stretch);
	}

	internal static string GetFamilyNameFromManifest(FontManifest? manifest, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		if (manifest?.Fonts is null || manifest.Fonts.Length == 0)
		{
			throw new ArgumentException("Font manifest file is incorrect.");
		}

		var bestSoFar = manifest.Fonts[0];
		for (int i = 1; i < manifest.Fonts.Length; i++)
		{
			var candidateMatch = manifest.Fonts[i];
			if (candidateMatch.FontWeight != bestSoFar.FontWeight && Math.Abs(candidateMatch.FontWeight - weight.Weight) < Math.Abs(bestSoFar.FontWeight - weight.Weight))
			{
				// candidateMatch is a better match than bestSoFar. So it's now our new bestSoFar.
				bestSoFar = candidateMatch;
			}
			else if (candidateMatch.FontStyle != bestSoFar.FontStyle && candidateMatch.FontStyle == style)
			{
				// The current bestSoFar
				bestSoFar = candidateMatch;
			}
			else if (stretch != FontStretch.Undefined && candidateMatch.FontStretch != bestSoFar.FontStretch && Math.Abs(candidateMatch.FontStretch - stretch) < Math.Abs(bestSoFar.FontStretch - stretch))
			{
				// candidateMatch is a better match than bestSoFar. So it's now our new bestSoFar.
				bestSoFar = candidateMatch;
			}
		}

		return bestSoFar.FamilyName;
	}

}
