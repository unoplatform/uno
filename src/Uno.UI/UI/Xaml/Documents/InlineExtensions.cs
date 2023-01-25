using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Documents
{
	internal static partial class InlineExtensions
	{
		/// <summary>
		/// Gets the combined text of all the leaf Inlines given a root Inline
		/// </summary>
		/// <param name="inline">The root Inline from which descend the leaf Inlines to combine</param>
		/// <returns>The combined text of all the leaf Inlines (Run or LineBreak)</returns>
		internal static string GetText(this Inline inline)
		{
			switch (inline)
			{
				case Run run:
					return run.Text ?? string.Empty;
				case LineBreak lineBreak:
					return "\n";
				case Span span:
					return string.Concat(span.Inlines.Select(GetText));
				default:
					return string.Empty;
			}
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
