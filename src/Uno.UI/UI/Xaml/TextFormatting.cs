using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Windows.UI.Text;
using FontWeights = Microsoft.UI.Text.FontWeights;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Per-element text formatting state. Stores the effective value of all text
/// formatting properties (local if set, inherited from parent otherwise).
/// </summary>
/// <remarks>
/// Mirrors WinUI's <c>TextFormatting</c> class (corep.h:436-513).
///
/// In WinUI, text properties (Foreground, FontSize, FontFamily, etc.) are NOT
/// stored in the DP system. Instead, they are stored in this struct, which is
/// attached to every UIElement and TextElement. The struct uses a generation
/// counter to detect staleness: when any text property changes anywhere in the
/// tree, a global counter is incremented. Before reading a text property,
/// <see cref="UIElement.EnsureTextFormatting"/> checks <see cref="IsOld"/> and
/// calls <see cref="UIElement.PullInheritedTextFormatting"/> to refresh values
/// from the parent if needed.
/// </remarks>
internal sealed class TextFormatting
{
	// -----------------------------------------------------------------------
	//  Property fields — mirrors corep.h:487-504
	// -----------------------------------------------------------------------

	internal Brush Foreground;
	internal FontFamily FontFamily;
	internal double FontSize = 14.0;
	internal FontWeight FontWeight = FontWeights.Normal;
	internal FontStyle FontStyle = FontStyle.Normal;
	internal FontStretch FontStretch = FontStretch.Normal;
	internal int CharacterSpacing;
	internal TextDecorations TextDecorations = TextDecorations.None;
	internal FlowDirection FlowDirection = FlowDirection.LeftToRight;
	internal string Language;
	internal bool IsTextScaleFactorEnabled = true;

	// -----------------------------------------------------------------------
	//  Freeze flag — corep.h:504
	// -----------------------------------------------------------------------

	/// <summary>
	/// When <see langword="true"/>, Foreground is not pulled from parent.
	/// Used at theme boundaries (<c>RequestedTheme != Default</c>) to prevent
	/// ancestor foreground from cascading into a differently-themed subtree.
	/// </summary>
	internal bool FreezeForeground;

	// -----------------------------------------------------------------------
	//  Generation counter — corep.h:489-491
	// -----------------------------------------------------------------------

	private uint _generationCounter;

	/// <summary>
	/// Returns <see langword="true"/> when the cached values are stale
	/// (the global counter has been incremented since the last pull).
	/// MUX ref: <c>TextFormatting::IsOld()</c> — corep.h:455-458.
	/// </summary>
	internal bool IsOld => _generationCounter != GlobalTextFormattingCounter.Value;

	/// <summary>
	/// Marks this instance as up-to-date with the current global counter.
	/// MUX ref: <c>TextFormatting::SetIsUpToDate()</c> — corep.h:460-463.
	/// </summary>
	internal void SetIsUpToDate() => _generationCounter = GlobalTextFormattingCounter.Value;

	// -----------------------------------------------------------------------
	//  Field access by property name
	// -----------------------------------------------------------------------

	/// <summary>
	/// Gets the value of a text formatting field by property name.
	/// </summary>
	internal object GetFieldValue(string propertyName) => propertyName switch
	{
		"Foreground" => Foreground,
		"FontFamily" => FontFamily,
		"FontSize" => FontSize,
		"FontWeight" => FontWeight,
		"FontStyle" => FontStyle,
		"FontStretch" => FontStretch,
		"CharacterSpacing" => CharacterSpacing,
		"TextDecorations" => TextDecorations,
		"FlowDirection" => FlowDirection,
		"Language" => Language,
		"IsTextScaleFactorEnabled" => IsTextScaleFactorEnabled,
		_ => null
	};

	/// <summary>
	/// Sets the value of a text formatting field by property name.
	/// </summary>
	internal void SetFieldValue(string propertyName, object value)
	{
		switch (propertyName)
		{
			case "Foreground":
				Foreground = (Brush)value;
				break;
			case "FontFamily":
				FontFamily = (FontFamily)value;
				break;
			case "FontSize":
				FontSize = (double)value;
				break;
			case "FontWeight":
				FontWeight = (FontWeight)value;
				break;
			case "FontStyle":
				FontStyle = (FontStyle)value;
				break;
			case "FontStretch":
				FontStretch = (FontStretch)value;
				break;
			case "CharacterSpacing":
				CharacterSpacing = (int)value;
				break;
			case "TextDecorations":
				TextDecorations = (TextDecorations)value;
				break;
			case "FlowDirection":
				FlowDirection = (FlowDirection)value;
				break;
			case "Language":
				Language = (string)value;
				break;
			case "IsTextScaleFactorEnabled":
				IsTextScaleFactorEnabled = (bool)value;
				break;
		}
	}

	// -----------------------------------------------------------------------
	//  Factory
	// -----------------------------------------------------------------------

	/// <summary>
	/// Creates a new <see cref="TextFormatting"/> initialized to default values.
	/// MUX ref: <c>TextFormatting::CreateDefault()</c> — TextFormatting.cpp:113.
	/// </summary>
	internal static TextFormatting CreateDefault() => new();
}
