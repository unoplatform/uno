using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Documents
{
	internal static partial class InlineExtensions
	{
		/// <summary>
		/// Gets the combined text of all the leaf Inlines given a root Inline
		/// </summary>
		/// <param name="inline">The root Inline from which descend the leaf Inlines to combine</param>
		/// <returns>The combined text of all the leaf Inlines (Run or LineBreak)</returns>
		internal static string GetText(this Inline inline) => GetText(inline, useCrLf: false);

		internal static string GetPlainText(this Inline inline) => GetText(inline, useCrLf: true);

		private static string GetText(Inline inline, bool useCrLf)
		{
			switch (inline)
			{
				case Run run:
					return run.Text ?? string.Empty;
				case LineBreak:
					return useCrLf ? "\r\n" : "\n";
				case Span span:
					return string.Concat(span.Inlines.Select<Inline, string>(useCrLf ? GetPlainText : GetText));
				default:
					return string.Empty;
			}
		}

		internal static string GetSelectedText(IEnumerable<InlineCollection> inlineCollections, int start, int end, bool includeParagraphBreaks)
		{
			var builder = new StringBuilder();
			var flatIndex = 0;

			void AppendText(string text, int flatLength)
			{
				if (flatLength == text.Length)
				{
					var from = Math.Max(start, flatIndex) - flatIndex;
					var to = Math.Min(end, flatIndex + flatLength) - flatIndex;
					if (from < to)
					{
						builder.Append(text, from, to - from);
					}
				}
				else if (start <= flatIndex && flatIndex < end)
				{
					builder.Append(text);
				}

				flatIndex += flatLength;
			}

			void AppendInline(Inline inline)
			{
				switch (inline)
				{
					case Run or LineBreak:
						var text = GetPlainText(inline);
						AppendText(text, inline is LineBreak ? 1 : text.Length);
						break;
					case Span span:
						foreach (var child in span.Inlines)
						{
							AppendInline(child);
						}
						break;
				}
			}

			foreach (var inlines in inlineCollections)
			{
				foreach (var inline in inlines)
				{
					AppendInline(inline);
				}

				if (includeParagraphBreaks)
				{
					AppendText("\r\n", 2);
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Enumerate all Inline nodes using pre-order depth-first traversal given a root Inline
		/// </summary>
		/// <param name="root">The root Inline of the tree</param>
		/// <returns>All Inline nodes of the given tree (including the root), ordered using pre-order depth-first traversal</returns>
		internal static IEnumerable<Inline> Enumerate(this Inline root)
		{
			yield return root;
			if (root is Span span)
			{
				foreach (var child in span.Inlines.SelectMany(Enumerate))
				{
					yield return child;
				}
			}
		}

		/// <summary>
		/// Checks whether an Inline has a typographical effect within a given TextBlock
		/// </summary>
		/// <param name="inline">The Inline for which we want to check the typographical effectiveness</param>
		/// <param name="textBlock">The TextBlock within which we want to check the Inline's typographical effectiveness</param>
		/// <returns>Whether the Inline has a typographical effect within the given TextBlock</returns>
		internal static bool HasTypographicalEffectWithin(this Inline inline, TextBlock textBlock)
		{
			return inline is Run run
				&& run.Text != string.Empty
				&& !inline.IsTypographicallyEquivalentTo(textBlock);
		}

		/// <summary>
		/// Checks whether an Inline is typographically equivalent to a given TextBlock
		/// </summary>
		/// <param name="inline">The Inline we want to compare to the TextBlock</param>
		/// <param name="textBlock">The TextBlock we want to compare to the Inline</param>
		/// <returns>Whether the Inline is typographically equivalent to the given TextBlock</returns>
		internal static bool IsTypographicallyEquivalentTo(this Inline inline, TextBlock textBlock)
		{
			return inline != null
				&& inline.Foreground == textBlock.Foreground
				&& inline.FontSize == textBlock.FontSize
				&& inline.FontWeight == textBlock.FontWeight
				&& inline.FontStyle == textBlock.FontStyle
				&& inline.FontFamily == textBlock.FontFamily
				&& inline.FontSize == textBlock.FontSize
				&& inline.CharacterSpacing == textBlock.CharacterSpacing
				&& inline.BaseLineAlignment == BaseLineAlignment.Baseline
				&& inline.TextDecorations == textBlock.TextDecorations;
		}
	}
}
