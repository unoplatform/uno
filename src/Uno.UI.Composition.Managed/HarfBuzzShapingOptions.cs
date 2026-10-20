#nullable enable

using HarfBuzzSharp;

namespace Uno.UI.Composition.Drawing;

// Shared by the HarfBuzz-based backends (Managed, and Skia through a linked compile item).
internal static class HarfBuzzShapingOptions
{
	private static readonly Feature[] _none = [];
	private static readonly Feature[] _noLigatures = [new Feature(new Tag('l', 'i', 'g', 'a'), 0)];

	public static void Apply(Buffer buffer, in ShapingOptions options)
	{
		if (!string.IsNullOrEmpty(options.Language))
		{
			buffer.Language = new Language(options.Language);
		}

		if (options.Script is { Length: 4 } script)
		{
			buffer.Script = Script.Parse(script);
		}
	}

	public static Feature[] GetFeatures(in ShapingOptions options)
	{
		if (!options.DisableKerning && !options.SmallCaps)
		{
			return options.DisableLigatures ? _noLigatures : _none;
		}

		var count = (options.DisableLigatures ? 1 : 0) + (options.DisableKerning ? 1 : 0) + (options.SmallCaps ? 1 : 0);
		var features = new Feature[count];
		var index = 0;
		if (options.DisableLigatures)
		{
			features[index++] = new Feature(new Tag('l', 'i', 'g', 'a'), 0);
		}
		if (options.DisableKerning)
		{
			features[index++] = new Feature(new Tag('k', 'e', 'r', 'n'), 0);
		}
		if (options.SmallCaps)
		{
			features[index++] = new Feature(new Tag('s', 'm', 'c', 'p'), 1);
		}

		return features;
	}
}
