using System.Collections.Generic;

namespace Windows.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// A container of properties that Skia needs for draw.
/// </summary>
partial interface ISegmentsElement
{
	TextWrapping TextWrapping { get; }

	TextAlignment TextAlignment { get; set; }

	FlowDirection FlowDirection { get; }

	double LineHeight { get; set; }

	int MaxLines { get; }

	LineStackingStrategy LineStackingStrategy { get; set; }
}
