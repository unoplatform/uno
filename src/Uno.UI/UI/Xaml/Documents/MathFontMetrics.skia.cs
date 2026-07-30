#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents;

internal sealed class MathFontMetrics
{
	private const int MaxMathTableBytes = 1024 * 1024;
	private static readonly ConditionalWeakTable<SKTypeface, RawMathData> _cache = new();
	private readonly RawMathData _rawData;
	private readonly float _em;
	private readonly int _unitsPerEm;

	private MathFontMetrics(
		RawMathData rawData,
		float em,
		int unitsPerEm,
		bool usesOpenTypeMath,
		float scriptScale,
		float scriptScriptScale,
		float axisHeight,
		float fractionNumeratorShift,
		float fractionDenominatorShift,
		float fractionNumeratorGap,
		float fractionDenominatorGap,
		float fractionRuleThickness,
		float superscriptShift,
		float superscriptBottom,
		float subscriptShift,
		float subscriptTop,
		float subSuperscriptGap,
		float spaceAfterScript,
		float radicalGap,
		float radicalRuleThickness,
		float radicalExtraAscender,
		float radicalKernBeforeDegree,
		float radicalKernAfterDegree,
		float radicalDegreeRaisePercent)
	{
		_rawData = rawData;
		_em = em;
		_unitsPerEm = unitsPerEm;
		UsesOpenTypeMath = usesOpenTypeMath;
		ScriptScale = scriptScale;
		ScriptScriptScale = scriptScriptScale;
		AxisHeight = axisHeight;
		FractionNumeratorShift = fractionNumeratorShift;
		FractionDenominatorShift = fractionDenominatorShift;
		FractionNumeratorGap = fractionNumeratorGap;
		FractionDenominatorGap = fractionDenominatorGap;
		FractionRuleThickness = fractionRuleThickness;
		SuperscriptShift = superscriptShift;
		SuperscriptBottom = superscriptBottom;
		SubscriptShift = subscriptShift;
		SubscriptTop = subscriptTop;
		SubSuperscriptGap = subSuperscriptGap;
		SpaceAfterScript = spaceAfterScript;
		RadicalGap = radicalGap;
		RadicalRuleThickness = radicalRuleThickness;
		RadicalExtraAscender = radicalExtraAscender;
		RadicalKernBeforeDegree = radicalKernBeforeDegree;
		RadicalKernAfterDegree = radicalKernAfterDegree;
		RadicalDegreeRaisePercent = radicalDegreeRaisePercent;
	}

	internal bool UsesOpenTypeMath { get; }

	internal float ScriptScale { get; }

	internal float ScriptScriptScale { get; }

	internal float AxisHeight { get; }

	internal float FractionNumeratorShift { get; }

	internal float FractionDenominatorShift { get; }

	internal float FractionNumeratorGap { get; }

	internal float FractionDenominatorGap { get; }

	internal float FractionRuleThickness { get; }

	internal float SuperscriptShift { get; }

	internal float SuperscriptBottom { get; }

	internal float SubscriptShift { get; }

	internal float SubscriptTop { get; }

	internal float SubSuperscriptGap { get; }

	internal float SpaceAfterScript { get; }

	internal float RadicalGap { get; }

	internal float RadicalRuleThickness { get; }

	internal float RadicalExtraAscender { get; }

	internal float RadicalKernBeforeDegree { get; }

	internal float RadicalKernAfterDegree { get; }

	internal float RadicalDegreeRaisePercent { get; }

	internal bool TryGetVerticalGlyph(
		SKFont font,
		string text,
		float targetSize,
		out MathGlyphRun glyphRun,
		bool allowVariant = true)
	{
		glyphRun = default;
		if (_rawData.Table is not { } table || _rawData.VariantsOffset == 0 || string.IsNullOrEmpty(text))
		{
			return false;
		}

		Span<ushort> glyph = stackalloc ushort[1];
		font.GetGlyphs(text, glyph);
		if (glyph[0] == 0
			|| !_rawData.TryGetVerticalConstruction(glyph[0], out var construction))
		{
			return false;
		}

		var scale = _em / _unitsPerEm;
		var targetUnits = Math.Clamp(targetSize / scale, 1, ushort.MaxValue * 8f);
		var selectedVariant = default(RawGlyphVariant);
		foreach (var variant in construction.Variants)
		{
			if (variant.Glyph != 0
				&& variant.Advance >= targetUnits
				&& (selectedVariant.Glyph == 0 || variant.Advance < selectedVariant.Advance))
			{
				selectedVariant = variant;
			}
		}
		if (selectedVariant.Glyph != 0
			&& allowVariant)
		{
			glyphRun = new MathGlyphRun(
				new[] { new MathGlyphPart(selectedVariant.Glyph, 0) },
				selectedVariant.Advance * scale,
				IsAssembly: false);
			return true;
		}

		return TryBuildAssembly(construction, _rawData.MinimumConnectorOverlap, targetUnits, scale, out glyphRun);
	}

	internal static bool HasOpenTypeMathTable(SKTypeface typeface)
	{
		ArgumentNullException.ThrowIfNull(typeface);
		var size = typeface.GetTableSize(new Tag('M', 'A', 'T', 'H'));
		return size >= 10 && size <= MaxMathTableBytes;
	}

	internal static bool TryReadVerticalConstructionForTesting(
		byte[] table,
		ushort glyph,
		out int variantCount,
		out int partCount)
	{
		variantCount = 0;
		partCount = 0;
		if (table is null || table.Length < 10)
		{
			return false;
		}

		var variantsOffset = TryReadUInt16(table, 8, out var offset) ? offset : 0;
		if (variantsOffset == 0
			|| !TryReadVerticalConstruction(table, variantsOffset, glyph, out var construction))
		{
			return false;
		}

		variantCount = construction.Variants.Length;
		partCount = construction.Parts.Length;
		return true;
	}

	internal static MathFontMetrics Create(FontDetails font)
	{
		var em = Math.Max(1, font.SKFontSize);
		var rawData = _cache.GetValue(font.SKFont.Typeface, ReadMathData);
		var constants = rawData.Constants;
		var unitsPerEm = Math.Max(1, font.SKFont.Typeface.UnitsPerEm);

		float Unit(OpenTypeMathConstant constant, float fallback)
		{
			if (!constants.TryGet(constant, out var value))
			{
				return fallback;
			}

			return Math.Clamp(value * em / unitsPerEm, -em * 4, em * 4);
		}

		float PositiveUnit(OpenTypeMathConstant constant, float fallback, float minimum = 0)
			=> Math.Max(minimum, Unit(constant, fallback));

		var scriptScale = constants.TryGet(OpenTypeMathConstant.ScriptPercentScaleDown, out var scriptPercent)
			? Math.Clamp(scriptPercent / 100f, 0.4f, 1f)
			: 0.7f;
		var scriptScriptScale = constants.TryGet(OpenTypeMathConstant.ScriptScriptPercentScaleDown, out var scriptScriptPercent)
			? Math.Clamp(scriptScriptPercent / 100f, 0.3f, scriptScale)
			: 0.5f;
		var degreeRaise = constants.TryGet(OpenTypeMathConstant.RadicalDegreeBottomRaisePercent, out var degreePercent)
			? Math.Clamp(degreePercent, 0, 100)
			: 60;

		return new MathFontMetrics(
			rawData,
			em,
			unitsPerEm,
			constants.HasData,
			scriptScale,
			scriptScriptScale,
			PositiveUnit(OpenTypeMathConstant.AxisHeight, em * 0.25f),
			PositiveUnit(OpenTypeMathConstant.FractionNumeratorShiftUp, em * 0.65f),
			PositiveUnit(OpenTypeMathConstant.FractionDenominatorShiftDown, em * 0.65f),
			PositiveUnit(OpenTypeMathConstant.FractionNumeratorGapMin, em * 0.15f),
			PositiveUnit(OpenTypeMathConstant.FractionDenominatorGapMin, em * 0.15f),
			PositiveUnit(OpenTypeMathConstant.FractionRuleThickness, em * 0.055f, 1),
			PositiveUnit(OpenTypeMathConstant.SuperscriptShiftUp, em * 0.55f),
			PositiveUnit(OpenTypeMathConstant.SuperscriptBottomMin, em * 0.2f),
			PositiveUnit(OpenTypeMathConstant.SubscriptShiftDown, em * 0.25f),
			PositiveUnit(OpenTypeMathConstant.SubscriptTopMax, em * 0.35f),
			PositiveUnit(OpenTypeMathConstant.SubSuperscriptGapMin, em * 0.2f),
			PositiveUnit(OpenTypeMathConstant.SpaceAfterScript, em * 0.05f),
			PositiveUnit(OpenTypeMathConstant.RadicalVerticalGap, em * 0.1f),
			PositiveUnit(OpenTypeMathConstant.RadicalRuleThickness, em * 0.055f, 1),
			PositiveUnit(OpenTypeMathConstant.RadicalExtraAscender, em * 0.05f),
			Unit(OpenTypeMathConstant.RadicalKernBeforeDegree, em * 0.05f),
			Unit(OpenTypeMathConstant.RadicalKernAfterDegree, -em * 0.05f),
			degreeRaise);
	}

	private static RawMathData ReadMathData(SKTypeface typeface)
	{
		var tag = new Tag('M', 'A', 'T', 'H');
		var size = typeface.GetTableSize(tag);
		if (size < 10 || size > MaxMathTableBytes)
		{
			return RawMathData.Empty;
		}

		var pointer = Marshal.AllocHGlobal(size);
		try
		{
			if (!typeface.TryGetTableData(tag, 0, size, pointer))
			{
				return RawMathData.Empty;
			}

			var data = new byte[size];
			Marshal.Copy(pointer, data, 0, size);
			var constantsOffset = ReadUInt16(data, 4);
			if (constantsOffset <= 0 || constantsOffset + 214 > data.Length)
			{
				return RawMathData.Empty;
			}

			var values = new int[56];
			values[(int)OpenTypeMathConstant.ScriptPercentScaleDown] = ReadInt16(data, constantsOffset);
			values[(int)OpenTypeMathConstant.ScriptScriptPercentScaleDown] = ReadInt16(data, constantsOffset + 2);
			values[(int)OpenTypeMathConstant.DelimitedSubFormulaMinHeight] = ReadUInt16(data, constantsOffset + 4);
			values[(int)OpenTypeMathConstant.DisplayOperatorMinHeight] = ReadUInt16(data, constantsOffset + 6);
			for (var index = (int)OpenTypeMathConstant.MathLeading;
				index <= (int)OpenTypeMathConstant.RadicalKernAfterDegree;
				index++)
			{
				values[index] = ReadInt16(data, constantsOffset + 8 + (index - 4) * 4);
			}
			values[(int)OpenTypeMathConstant.RadicalDegreeBottomRaisePercent] = ReadUInt16(data, constantsOffset + 212);
			var variantsOffset = ReadUInt16(data, 8);
			if (variantsOffset != 0 && variantsOffset + 10 > data.Length)
			{
				variantsOffset = 0;
			}
			var connectorOverlap = variantsOffset != 0 ? ReadUInt16(data, variantsOffset) : (ushort)0;
			return new RawMathData(new RawMathConstants(values), data, variantsOffset, connectorOverlap);
		}
		catch (ArgumentOutOfRangeException)
		{
			return RawMathData.Empty;
		}
		finally
		{
			Marshal.FreeHGlobal(pointer);
		}
	}

	private static short ReadInt16(byte[] data, int offset)
		=> unchecked((short)ReadUInt16(data, offset));

	private static ushort ReadUInt16(byte[] data, int offset)
	{
		if (offset < 0 || offset + 1 >= data.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(offset));
		}

		return (ushort)((data[offset] << 8) | data[offset + 1]);
	}

	private static bool TryReadUInt16(byte[] data, int offset, out ushort value)
	{
		if ((uint)offset >= (uint)data.Length || offset + 1 >= data.Length)
		{
			value = 0;
			return false;
		}

		value = (ushort)((data[offset] << 8) | data[offset + 1]);
		return true;
	}

	private static bool TryReadVerticalConstruction(
		byte[] table,
		int variantsOffset,
		ushort glyph,
		out RawGlyphConstruction construction)
	{
		construction = default;
		if (!TryReadUInt16(table, variantsOffset + 2, out var coverageOffset)
			|| !TryReadUInt16(table, variantsOffset + 6, out var glyphCount)
			|| glyphCount > 4096
			|| coverageOffset == 0
			|| !TryGetCoverageIndex(table, variantsOffset + coverageOffset, glyph, out var coverageIndex)
			|| coverageIndex >= glyphCount
			|| !TryReadUInt16(table, variantsOffset + 10 + coverageIndex * 2, out var constructionOffset)
			|| constructionOffset == 0)
		{
			return false;
		}

		var offset = variantsOffset + constructionOffset;
		if (!TryReadUInt16(table, offset, out var assemblyOffset)
			|| !TryReadUInt16(table, offset + 2, out var variantCount)
			|| variantCount > 1024
			|| offset + 4L + variantCount * 4L > table.Length)
		{
			return false;
		}

		var variants = new RawGlyphVariant[variantCount];
		for (var index = 0; index < variants.Length; index++)
		{
			var recordOffset = offset + 4 + index * 4;
			if (!TryReadUInt16(table, recordOffset, out var variantGlyph)
				|| !TryReadUInt16(table, recordOffset + 2, out var advance))
			{
				return false;
			}
			variants[index] = new RawGlyphVariant(variantGlyph, advance);
		}

		var parts = Array.Empty<RawGlyphPart>();
		if (assemblyOffset != 0)
		{
			var partOffset = offset + assemblyOffset;
			if (!TryReadUInt16(table, partOffset + 4, out var partCount)
				|| partCount is 0 or > 256
				|| partOffset + 6L + partCount * 10L > table.Length)
			{
				return false;
			}

			parts = new RawGlyphPart[partCount];
			for (var index = 0; index < parts.Length; index++)
			{
				var recordOffset = partOffset + 6 + index * 10;
				if (!TryReadUInt16(table, recordOffset, out var partGlyph)
					|| !TryReadUInt16(table, recordOffset + 2, out var startConnector)
					|| !TryReadUInt16(table, recordOffset + 4, out var endConnector)
					|| !TryReadUInt16(table, recordOffset + 6, out var fullAdvance)
					|| !TryReadUInt16(table, recordOffset + 8, out var flags)
					|| partGlyph == 0
					|| fullAdvance == 0)
				{
					return false;
				}
				parts[index] = new RawGlyphPart(
					partGlyph,
					startConnector,
					endConnector,
					fullAdvance,
					(flags & 1) != 0);
			}
		}

		construction = new RawGlyphConstruction(variants, parts);
		return variants.Length > 0 || parts.Length > 0;
	}

	private static bool TryGetCoverageIndex(byte[] table, int offset, ushort glyph, out int coverageIndex)
	{
		coverageIndex = -1;
		if (!TryReadUInt16(table, offset, out var format)
			|| !TryReadUInt16(table, offset + 2, out var count)
			|| count > 4096)
		{
			return false;
		}

		if (format == 1)
		{
			if (offset + 4L + count * 2L > table.Length)
			{
				return false;
			}
			for (var index = 0; index < count; index++)
			{
				if (!TryReadUInt16(table, offset + 4 + index * 2, out var covered))
				{
					return false;
				}
				if (covered == glyph)
				{
					coverageIndex = index;
					return true;
				}
			}
			return false;
		}

		if (format != 2 || offset + 4L + count * 6L > table.Length)
		{
			return false;
		}
		for (var index = 0; index < count; index++)
		{
			var rangeOffset = offset + 4 + index * 6;
			if (!TryReadUInt16(table, rangeOffset, out var start)
				|| !TryReadUInt16(table, rangeOffset + 2, out var end)
				|| !TryReadUInt16(table, rangeOffset + 4, out var startCoverageIndex)
				|| start > end)
			{
				return false;
			}
			if (glyph >= start && glyph <= end)
			{
				coverageIndex = startCoverageIndex + glyph - start;
				return true;
			}
		}
		return false;
	}

	private static bool TryBuildAssembly(
		RawGlyphConstruction construction,
		ushort minimumConnectorOverlap,
		float targetUnits,
		float scale,
		out MathGlyphRun glyphRun)
	{
		glyphRun = default;
		if (construction.Parts.Length is 0 or > 64)
		{
			return false;
		}

		var hasExtender = Array.FindIndex(construction.Parts, part => part.IsExtender) >= 0;
		var repeats = 0;
		List<RawGlyphPart> parts;
		var maximumAdvance = 0f;
		do
		{
			parts = new List<RawGlyphPart>(construction.Parts.Length + repeats);
			foreach (var part in construction.Parts)
			{
				var count = part.IsExtender ? repeats : 1;
				for (var index = 0; index < count; index++)
				{
					parts.Add(part);
				}
			}
			if (parts.Count == 0)
			{
				repeats++;
				continue;
			}
			maximumAdvance = GetAssemblyAdvance(parts, minimumConnectorOverlap, useMaximumOverlap: false);
			repeats++;
			if (!hasExtender)
			{
				break;
			}
		}
		while (maximumAdvance < targetUnits && parts.Count < 64);

		if (parts.Count == 0 || parts.Count > 64)
		{
			return false;
		}

		var minimumAdvance = GetAssemblyAdvance(parts, minimumConnectorOverlap, useMaximumOverlap: true);
		var advance = Math.Clamp(targetUnits, minimumAdvance, maximumAdvance);
		var remainingGrowth = advance - minimumAdvance;
		var placements = new MathGlyphPart[parts.Count];
		var position = 0f;
		for (var index = 0; index < parts.Count; index++)
		{
			// Vertical assembly records are stored bottom-to-top, while Skia's Y axis grows downward.
			placements[index] = new MathGlyphPart(parts[index].Glyph, -position * scale);
			if (index + 1 < parts.Count)
			{
				var maximumOverlap = Math.Min(parts[index].EndConnector, parts[index + 1].StartConnector);
				var minimumOverlap = Math.Min(maximumOverlap, minimumConnectorOverlap);
				var overlapReduction = Math.Min(remainingGrowth, maximumOverlap - minimumOverlap);
				position += parts[index].FullAdvance
					- (maximumOverlap - overlapReduction);
				remainingGrowth -= overlapReduction;
			}
		}
		glyphRun = new MathGlyphRun(placements, advance * scale, IsAssembly: true);
		return true;
	}

	private static float GetAssemblyAdvance(
		List<RawGlyphPart> parts,
		ushort minimumConnectorOverlap,
		bool useMaximumOverlap)
	{
		var advance = 0f;
		for (var index = 0; index < parts.Count; index++)
		{
			advance += parts[index].FullAdvance;
			if (index + 1 < parts.Count)
			{
				var maximumOverlap = Math.Min(parts[index].EndConnector, parts[index + 1].StartConnector);
				advance -= useMaximumOverlap
					? maximumOverlap
					: Math.Min(maximumOverlap, minimumConnectorOverlap);
			}
		}
		return advance;
	}

	internal readonly record struct MathGlyphRun(MathGlyphPart[] Parts, float Advance, bool IsAssembly);

	internal readonly record struct MathGlyphPart(ushort Glyph, float Offset);

	private readonly record struct RawGlyphConstruction(RawGlyphVariant[] Variants, RawGlyphPart[] Parts);

	private readonly record struct RawGlyphVariant(ushort Glyph, ushort Advance);

	private readonly record struct RawGlyphPart(
		ushort Glyph,
		ushort StartConnector,
		ushort EndConnector,
		ushort FullAdvance,
		bool IsExtender);

	private sealed class RawMathData
	{
		internal static RawMathData Empty { get; } = new(RawMathConstants.Empty, null, 0, 0);

		internal RawMathData(
			RawMathConstants constants,
			byte[]? table,
			ushort variantsOffset,
			ushort minimumConnectorOverlap)
		{
			Constants = constants;
			Table = table;
			VariantsOffset = variantsOffset;
			MinimumConnectorOverlap = minimumConnectorOverlap;
		}

		internal RawMathConstants Constants { get; }

		internal byte[]? Table { get; }

		internal ushort VariantsOffset { get; }

		internal ushort MinimumConnectorOverlap { get; }

		private readonly Dictionary<ushort, RawGlyphConstruction?> _verticalConstructions = new();

		internal bool TryGetVerticalConstruction(ushort glyph, out RawGlyphConstruction construction)
		{
			lock (_verticalConstructions)
			{
				if (!_verticalConstructions.TryGetValue(glyph, out var cached))
				{
					cached = Table is not null
						&& VariantsOffset != 0
						&& TryReadVerticalConstruction(Table, VariantsOffset, glyph, out var parsed)
							? parsed
							: null;
					_verticalConstructions.Add(glyph, cached);
				}

				if (cached is { } value)
				{
					construction = value;
					return true;
				}
			}

			construction = default;
			return false;
		}
	}

	private sealed class RawMathConstants
	{
		internal static RawMathConstants Empty { get; } = new(null);

		private readonly int[]? _values;

		internal RawMathConstants(int[]? values)
		{
			_values = values;
		}

		internal bool HasData => _values is not null;

		internal bool TryGet(OpenTypeMathConstant constant, out int value)
		{
			if (_values is not null && (uint)constant < (uint)_values.Length)
			{
				value = _values[(int)constant];
				return true;
			}

			value = 0;
			return false;
		}
	}
}
