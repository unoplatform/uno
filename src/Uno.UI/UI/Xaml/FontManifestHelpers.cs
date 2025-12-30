#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI.Text;

namespace Uno.UI.Xaml.Media;

internal static class FontManifestHelpers
{
	internal enum FontMatchingResult
	{
		CandidateIsBetter,
		CandidateIsWorse,
		Equivalent,
	}

	internal static FontManifest? DeserializeManifest(Stream manifestStream)
		=> JsonSerializer.Deserialize<FontManifest>(manifestStream, FontManifestSerializerContext.Default.FontManifest);

	internal static string GetFamilyNameFromManifest(Stream jsonStream, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		var manifest = DeserializeManifest(jsonStream);
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
			var stretchResult = IsBetterStretch(candidateMatch.FontStretch, bestSoFar.FontStretch, stretch);
			if (stretchResult == FontMatchingResult.CandidateIsBetter)
			{
				bestSoFar = candidateMatch;
			}
			else if (stretchResult == FontMatchingResult.Equivalent)
			{
				var styleResult = IsBetterStyle(candidateMatch.FontStyle, bestSoFar.FontStyle, style);
				if (styleResult == FontMatchingResult.CandidateIsBetter)
				{
					bestSoFar = candidateMatch;
				}
				else if (styleResult == FontMatchingResult.Equivalent)
				{
					var weightResult = IsBetterWeight(candidateMatch.FontWeight, bestSoFar.FontWeight, weight.Weight);
					if (weightResult == FontMatchingResult.CandidateIsBetter)
					{
						bestSoFar = candidateMatch;
					}
				}
			}
		}

		return bestSoFar.FamilyName;
	}

	private static FontMatchingResult? GenericComparison(int candidate, int bestSoFar, int requested)
	{
		if (candidate == bestSoFar)
		{
			return FontMatchingResult.Equivalent;
		}
		else if (bestSoFar == requested)
		{
			return FontMatchingResult.CandidateIsWorse;
		}
		else if (candidate == requested)
		{
			return FontMatchingResult.CandidateIsBetter;
		}

		return null;
	}


	internal static FontMatchingResult IsBetterStretch(FontStretch candidate, FontStretch bestSoFar, FontStretch requestedStretch)
	{
		// https://www.w3.org/TR/css-fonts-3/#font-style-matching
		// SPEC: 'font-stretch' is tried first. If the matching set contains faces with width values
		// SPEC: matching the 'font-stretch' value, faces with other width values are removed from the
		// SPEC: matching set. If there is no face that exactly matches the width value the nearest width
		// SPEC: is used instead. If the value of 'font-stretch' is 'normal' or one of the condensed
		// SPEC: values, narrower width values are checked first, then wider values. If the value
		// SPEC: of 'font-stretch' is one of the expanded values, wider values are checked first, followed
		// SPEC: by narrower values. Once the closest matching width has been determined by this
		// SPEC: process, faces with other widths are removed from the matching set.

		var result = GenericComparison((int)candidate, (int)bestSoFar, (int)requestedStretch);
		if (result.HasValue)
		{
			return result.Value;
		}

		if (requestedStretch <= FontStretch.Normal)
		{
			// Prefer narrower
			if (candidate < requestedStretch && bestSoFar > requestedStretch)
			{
				// Candidate is narrower, while bestSoFar is wider
				// Candidate is better.
				return FontMatchingResult.CandidateIsBetter;
			}
			else if (candidate > requestedStretch && bestSoFar < requestedStretch)
			{
				// Candidate is wider, while bestSoFar is narrower.
				// bestSoFar was better.
				return FontMatchingResult.CandidateIsWorse;
			}
			else if (candidate > requestedStretch)
			{
				// Both candidate and bestSoFar are wider from requestedStretch
				Debug.Assert(bestSoFar > requestedStretch);
				return candidate < bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
			else
			{
				Debug.Assert(bestSoFar < requestedStretch);
				return candidate > bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
		}
		else
		{
			// Prefer wider
			if (candidate > requestedStretch && bestSoFar < requestedStretch)
			{
				// Candidate is wider, while bestSoFar is narrower
				// Candidate is better.
				return FontMatchingResult.CandidateIsBetter;
			}
			else if (candidate < requestedStretch && bestSoFar > requestedStretch)
			{
				// Candidate is narrower, while bestSoFar is wider.
				// bestSoFar was better.
				return FontMatchingResult.CandidateIsWorse;
			}
			else if (candidate > requestedStretch)
			{
				// Both candidate and bestSoFar are wider from requestedStretch
				Debug.Assert(bestSoFar > requestedStretch);
				return candidate < bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
			else
			{
				Debug.Assert(bestSoFar < requestedStretch);
				return candidate > bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
		}
	}

	internal static FontMatchingResult IsBetterStyle(FontStyle candidate, FontStyle bestSoFar, FontStyle requestedStyle)
	{
		// https://www.w3.org/TR/css-fonts-3/#font-style-matching
		// SPEC: 'font-style' is tried next. If the value of 'font-style' is 'italic', italic faces are checked first, then
		// SPEC: oblique, then normal faces. If the value is 'oblique', oblique faces are checked first, then italic
		// SPEC: faces and then normal faces. If the value is 'normal', normal faces are checked first, then oblique
		// SPEC: faces, then italic faces. Faces with other style values are excluded from the matching set.
		// SPEC: User agents are permitted to distinguish between italic and oblique faces within platform font families but this
		// SPEC: is not required, so all italic or oblique faces may be treated as italic faces. However, within font families
		// SPEC: defined via @font-face rules, italic and oblique faces must be distinguished using the value of
		// SPEC: the 'font-style' descriptor. For families that lack any italic or oblique faces, user agents may
		// SPEC: create artificial oblique faces, if this is permitted by the value of the 'font-synthesis' property.

		var result = GenericComparison((int)candidate, (int)bestSoFar, (int)requestedStyle);
		if (result.HasValue)
		{
			return result.Value;
		}

		if ((requestedStyle == FontStyle.Italic && candidate == FontStyle.Oblique) ||
			(requestedStyle == FontStyle.Oblique && candidate == FontStyle.Italic) ||
			(requestedStyle == FontStyle.Normal && candidate == FontStyle.Oblique))
		{
			return FontMatchingResult.CandidateIsBetter;
		}
		else
		{
			return FontMatchingResult.CandidateIsWorse;
		}
	}

	private static FontMatchingResult FontWeightRuleLessThan400(ushort candidate, ushort bestSoFar, ushort requestedWeight)
	{
		// SPEC: If the desired weight is less than 400, weights below the desired weight are checked in descending order followed by weights above the desired weight in ascending order until a match is found.
		if (candidate < requestedWeight && bestSoFar < requestedWeight)
		{
			return candidate > bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
		}
		else if (candidate > requestedWeight && bestSoFar > requestedWeight)
		{
			return candidate < bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
		}
		else
		{
			return candidate < requestedWeight ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
		}
	}

	internal static FontMatchingResult IsBetterWeight(ushort candidate, ushort bestSoFar, ushort requestedWeight)
	{
		// https://www.w3.org/TR/css-fonts-3/#font-style-matching
		// SPEC: 'font-weight' is matched next, so it will always reduce the matching set to a single font face.
		// SPEC: If bolder/lighter relative weights are used, the effective weight is calculated based on the inherited weight value, as described
		// SPEC: in the definition of the 'font-weight' property. Given the desired weight and the weights of faces in the matching set after the
		// SPEC: steps above, if the desired weight is available that face matches. Otherwise, a weight is chosen using the rules below:
		// SPEC: If the desired weight is less than 400, weights below the desired weight are checked in descending order followed by weights above the desired weight in ascending order until a match is found.
		// SPEC: If the desired weight is greater than 500, weights above the desired weight are checked in ascending order followed by weights below the desired weight in descending order until a match is found.
		// SPEC: If the desired weight is 400, 500 is checked first and then the rule for desired weights less than 400 is used.
		// SPEC: If the desired weight is 500, 400 is checked first and then the rule for desired weights less than 400 is used.

		var result = GenericComparison(candidate, bestSoFar, requestedWeight);
		if (result.HasValue)
		{
			return result.Value;
		}

		if (requestedWeight < 400)
		{
			return FontWeightRuleLessThan400(candidate, bestSoFar, requestedWeight);
		}
		else if (requestedWeight > 500)
		{
			if (candidate > requestedWeight && bestSoFar > requestedWeight)
			{
				return candidate < bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
			else if (candidate < requestedWeight && bestSoFar < requestedWeight)
			{
				return candidate > bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
			else
			{
				return candidate > requestedWeight ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
		}
		else if (requestedWeight == 400)
		{
			if (candidate == 500)
			{
				return FontMatchingResult.CandidateIsBetter;
			}
			else if (bestSoFar == 500)
			{
				return FontMatchingResult.CandidateIsWorse;
			}

			return FontWeightRuleLessThan400(candidate, bestSoFar, requestedWeight);
		}
		else if (requestedWeight == 500)
		{
			if (candidate == 400)
			{
				return FontMatchingResult.CandidateIsBetter;
			}
			else if (bestSoFar == 400)
			{
				return FontMatchingResult.CandidateIsWorse;
			}

			return FontWeightRuleLessThan400(candidate, bestSoFar, requestedWeight);
		}
		else
		{
			// Here, requestedWeight is somewhere between 400 and 500
			// The spec doesn't define the behavior for this case.
			// We do this as "best-effort"
			if (candidate < requestedWeight && bestSoFar > requestedWeight)
			{
				// If candidate is lighter, prefer it.
				return FontMatchingResult.CandidateIsBetter;
			}
			else if (candidate > requestedWeight && bestSoFar < requestedWeight)
			{
				// bestSoFar is lighter.
				// It is not better than the new candidate.
				return FontMatchingResult.CandidateIsWorse;
			}
			else if (candidate > requestedWeight)
			{
				return candidate < bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
			else
			{
				return candidate > bestSoFar ? FontMatchingResult.CandidateIsBetter : FontMatchingResult.CandidateIsWorse;
			}
		}
	}
}
