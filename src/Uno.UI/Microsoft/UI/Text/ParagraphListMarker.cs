#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Text;

internal sealed class ParagraphListMarkerState
{
	internal readonly Dictionary<int, ParagraphListMarkerCounter> Counters = new();
	internal int PreviousLevel;
	internal bool PreviousWasList;

	internal ParagraphListMarkerState Clone()
	{
		var clone = new ParagraphListMarkerState
		{
			PreviousLevel = PreviousLevel,
			PreviousWasList = PreviousWasList,
		};
		foreach (var (level, counter) in Counters)
		{
			clone.Counters.Add(level, counter);
		}
		return clone;
	}
}

internal readonly record struct ParagraphListMarkerCounter(
	global::Microsoft.UI.Text.MarkerType Type,
	global::Microsoft.UI.Text.MarkerStyle Style,
	int ConfiguredStart,
	int Value);

internal static class ParagraphListMarker
{
	internal static string? GetNext(ParagraphFormatState format, ParagraphListMarkerState state, out bool hasList)
	{
		var listType = format.ListType;
		hasList = listType is not global::Microsoft.UI.Text.MarkerType.None
			and not global::Microsoft.UI.Text.MarkerType.Undefined
			&& format.ListLevelIndex >= 0;
		if (!hasList)
		{
			state.Counters.Clear();
			state.PreviousLevel = 0;
			state.PreviousWasList = false;
			return null;
		}

		var level = format.ListLevelIndex;
		var style = format.ListStyle == global::Microsoft.UI.Text.MarkerStyle.Undefined
			? global::Microsoft.UI.Text.MarkerStyle.Period
			: format.ListStyle;
		if (!state.PreviousWasList)
		{
			state.Counters.Clear();
		}
		else if (level <= state.PreviousLevel)
		{
			var deeperLevels = new List<int>();
			foreach (var existingLevel in state.Counters.Keys)
			{
				if (existingLevel > level)
				{
					deeperLevels.Add(existingLevel);
				}
			}
			foreach (var deeperLevel in deeperLevels)
			{
				state.Counters.Remove(deeperLevel);
			}
		}

		var configuredStart = format.ListStart;
		var firstValue = listType == global::Microsoft.UI.Text.MarkerType.UnicodeSequence
			? (global::Microsoft.UI.Xaml.Controls.RichEditBox.IsValidListMarkerUnicodeScalar(configuredStart) ? configuredStart : 0x2022)
			: configuredStart > 0 ? configuredStart : 1;
		var value = firstValue;
		if (state.Counters.TryGetValue(level, out var counter)
			&& counter.Type == listType
			&& counter.Style == style
			&& counter.ConfiguredStart == configuredStart)
		{
			value = counter.Value + 1;
		}

		state.Counters[level] = new ParagraphListMarkerCounter(listType, style, configuredStart, value);
		state.PreviousLevel = level;
		state.PreviousWasList = true;
		return global::Microsoft.UI.Xaml.Controls.RichEditBox.FormatListMarker(listType, style, value);
	}
}
