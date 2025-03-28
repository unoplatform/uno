using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Text;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Documents.TextFormatting
{
	// TODO: Apply interface to Xaml.Documents.Block when that is implemented to enable the text engine to render Paragraph elements.

	/// <summary>
	/// Provides common text rendering properties for parent block elements that can contain inlines.
	/// </summary>
	internal interface IBlock
	{
		double LineHeight { get; }

		LineStackingStrategy LineStackingStrategy { get; }

		TextAlignment TextAlignment { get; }

		TextTrimming TextTrimming { get; }

		TextWrapping TextWrapping { get; }

		FlowDirection FlowDirection { get; }

		int MaxLines { get; }

		void Invalidate(bool updateText);

		string GetText();
	}
}
