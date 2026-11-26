#nullable enable

using System.Diagnostics;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// <see cref="TextFormatter"/> over <see cref="UnicodeText"/>, the engine TextBlock uses.
/// </summary>
/// <remarks>
/// WinUI's FormatLine produces one line at a time and threads an opaque continuation token between calls.
/// <see cref="UnicodeText"/> lays a whole paragraph out at once, so a FormatLine without a token from the same source
/// takes a layout from the node's <see cref="UnicodeTextRunCache"/> (or makes one), and each call vends one engine line
/// and a token for the next. A node continuing a paragraph on another page gets the previous page's token: it continues
/// that page's layout when it is as wide, and otherwise resumes a layout of its own where the token says the next line
/// starts.
/// </remarks>
internal sealed class UnicodeTextFormatter : TextFormatter
{
	// Stateless: the layouts live in the run cache and the tokens.
	public static UnicodeTextFormatter Instance { get; } = new();

	public override TextLine FormatLine(
		TextSource textSource,
		uint firstCharIndex,
		double wrappingWidth,
		TextParagraphProperties textParagraphProperties,
		TextLineBreak? previousLineBreak,
		TextRunCache? textRunCache)
	{
		var token = previousLineBreak as UnicodeTextLineBreak;
		var source = (IUnicodeParagraphSource)textSource;
		var cache = textRunCache as UnicodeTextRunCache;

		UnicodeParagraphLayout layout;
		int line;
		if (token is not null && ReferenceEquals(token.TextSource, textSource) && token.WrappingWidth == wrappingWidth)
		{
			layout = token.Layout;
			line = token.NextEngineLine;
		}
		else if (ContinuesSharedLayout(token, textSource, wrappingWidth, textParagraphProperties, cache, out var shared))
		{
			// A page as wide as the previous one continues its layout, which already holds the lines that follow: laying
			// the whole paragraph out again for every page would make a paragraph across N pages cost N layouts.
			layout = shared;
			line = token!.NextEngineLine;
		}
		else
		{
			var key = CreateKey(textSource, wrappingWidth, token?.NextEngineStart ?? 0, textParagraphProperties);

			if (cache?.Find(key) is { } cached)
			{
				// As WinUI formats object runs on every fresh format: the node removed its children before measuring.
				layout = cached.RefreshObjects(source);
				if (!ReferenceEquals(layout, cached))
				{
					cache.Add(layout);
				}
			}
			else
			{
				layout = UnicodeParagraphLayout.Create(key, source, textParagraphProperties);
				cache?.Add(layout);
			}

			line = layout.Text.FirstResumedLine;
			Debug.Assert(
				token is null || layout.ToNode(layout.Text.GetLineInfo(line).Start) == firstCharIndex,
				"A resumed layout starts where the previous page broke.");
		}

		return layout.GetLine(textSource, line);
	}

	// Whether this page continues the previous page's layout. It does when the objects it hosts still have the sizes that
	// layout was made with; a layout of its own then is no longer used. When one changed, the page's measure lays it out
	// on its own; outside measure (arrange, drawing, hit-tests) the page reads that layout, or, before its measure has run,
	// keeps the shared one, as RefreshObjects leaves a child's new size to the measure it scheduled.
	private static bool ContinuesSharedLayout(
		UnicodeTextLineBreak? token,
		TextSource textSource,
		double wrappingWidth,
		TextParagraphProperties properties,
		UnicodeTextRunCache? cache,
		[global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out UnicodeParagraphLayout? shared)
	{
		shared = null;
		if (token is null
			|| token.WrappingWidth != wrappingWidth
			|| token.Layout.Base != token.Layout
			|| token.Layout.LayoutKey != CreateKey(token.Layout.LayoutKey.Source, wrappingWidth, token.Layout.LayoutKey.ResumeIndex, properties))
		{
			return false;
		}

		var source = (IUnicodeParagraphSource)textSource;
		if (token.Layout.ContinueOn(source, token.NextEngineStart))
		{
			cache?.Remove(textSource);
		}
		else if (source.CanHostElements
			|| cache?.Find(CreateKey(textSource, wrappingWidth, token.NextEngineStart, properties)) is not null)
		{
			return false;
		}

		shared = token.Layout;
		return true;
	}

	private static UnicodeParagraphLayout.Key CreateKey(TextSource textSource, double wrappingWidth, int resumeIndex, TextParagraphProperties properties)
		=> new(
			textSource,
			wrappingWidth,
			resumeIndex,
			properties.TextWrapping,
			properties.GetFlags(TextParagraphProperties.Flags.Justify),
			properties.TextLineBounds,
			properties.FlowDirection,
			properties.GetFlags(TextParagraphProperties.Flags.DetermineTextReadingOrderFromContent));
}
