#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents;

internal sealed class MathFontMetrics
{
	private const int MaxMathTableBytes = 1024 * 1024;
	private static readonly ConditionalWeakTable<SKTypeface, RawMathConstants> _cache = new();

	private MathFontMetrics(
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

	internal static MathFontMetrics Create(FontDetails font)
	{
		var em = Math.Max(1, font.SKFontSize);
		var constants = _cache.GetValue(font.SKFont.Typeface, ReadMathConstants);
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

	private static RawMathConstants ReadMathConstants(SKTypeface typeface)
	{
		var tag = new Tag('M', 'A', 'T', 'H');
		var size = typeface.GetTableSize(tag);
		if (size < 12 || size > MaxMathTableBytes)
		{
			return RawMathConstants.Empty;
		}

		var pointer = Marshal.AllocHGlobal(size);
		try
		{
			if (!typeface.TryGetTableData(tag, 0, size, pointer))
			{
				return RawMathConstants.Empty;
			}

			var data = new byte[size];
			Marshal.Copy(pointer, data, 0, size);
			var constantsOffset = ReadUInt16(data, 4);
			if (constantsOffset <= 0 || constantsOffset + 214 > data.Length)
			{
				return RawMathConstants.Empty;
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
			return new RawMathConstants(values);
		}
		catch (ArgumentOutOfRangeException)
		{
			return RawMathConstants.Empty;
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
