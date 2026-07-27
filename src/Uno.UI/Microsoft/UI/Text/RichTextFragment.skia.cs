#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.UI.Text
{
	// A detached snapshot of text and its coalesced formatting runs. Run lengths cover Text exactly;
	// mutable formatting state is cloned when a fragment crosses into or out of document ownership.
	internal sealed class RichTextFragment
	{
		private readonly IReadOnlyList<FormatRun> _characterRuns;
		private readonly IReadOnlyList<ParagraphRun> _paragraphRuns;

		internal RichTextFragment(
			string text,
			IReadOnlyList<FormatRun> characterRuns,
			IReadOnlyList<ParagraphRun> paragraphRuns,
			ParagraphFormatState terminalParagraphState,
			bool hasExplicitTerminalParagraphState = true,
			bool preservesTerminalParagraphStateOnImport = false)
		{
			Text = text;
			_characterRuns = characterRuns;
			_paragraphRuns = paragraphRuns;
			TerminalParagraphState = terminalParagraphState;
			HasExplicitTerminalParagraphState = hasExplicitTerminalParagraphState;
			PreservesTerminalParagraphStateOnImport = preservesTerminalParagraphStateOnImport;
			AssertRunInvariants();
		}

		internal string Text { get; }

		internal IReadOnlyList<FormatRun> CharacterRuns => _characterRuns;

		internal IReadOnlyList<ParagraphRun> ParagraphRuns => _paragraphRuns;

		internal ParagraphFormatState TerminalParagraphState { get; }

		internal bool HasExplicitTerminalParagraphState { get; }

		internal bool PreservesTerminalParagraphStateOnImport { get; }

		internal CharacterFormatState GetCharacterFormatAt(int position)
			=> GetFormatAt(_characterRuns, position);

		internal ParagraphFormatState GetParagraphFormatAt(int position)
			=> GetFormatAt(_paragraphRuns, position);

		internal RichTextFragment WithTerminalParagraph(
			ParagraphFormatState terminalParagraphState,
			bool hasExplicitTerminalParagraphState = true)
			=> new(
				Text,
				_characterRuns,
				_paragraphRuns,
				terminalParagraphState,
				hasExplicitTerminalParagraphState,
				PreservesTerminalParagraphStateOnImport);

		internal RichTextFragment TransformCharacterFormats(Action<CharacterFormatState> transform)
		{
			var runs = new List<FormatRun>(_characterRuns.Count);
			foreach (var run in _characterRuns)
			{
				var format = run.Format.Clone();
				transform(format);
				AppendCharacterRun(runs, run.Length, format);
			}

			return new(
				Text,
				runs,
				_paragraphRuns,
				TerminalParagraphState,
				HasExplicitTerminalParagraphState,
				PreservesTerminalParagraphStateOnImport);
		}

		internal RichTextFragment Slice(
			int start,
			int length,
			ParagraphFormatState terminalParagraphState,
			bool hasExplicitTerminalParagraphState)
		{
			if ((uint)start > (uint)Text.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(start));
			}
			if ((uint)length > (uint)(Text.Length - start))
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			return new(
				Text.Substring(start, length),
				SliceCharacterRuns(_characterRuns, start, length),
				SliceParagraphRuns(_paragraphRuns, start, length),
				terminalParagraphState,
				hasExplicitTerminalParagraphState,
				PreservesTerminalParagraphStateOnImport);
		}

		internal bool AreRunInvariantsValid()
			=> AreRunsValid(_characterRuns, Text.Length)
				&& AreRunsValid(_paragraphRuns, Text.Length);

		internal static RichTextFragment CreateSingleRun(
			string text,
			CharacterFormatState characterFormat,
			ParagraphFormatState paragraphFormat,
			ParagraphFormatState? terminalParagraphState = null,
			bool hasExplicitTerminalParagraphState = false)
			=> new(
				text,
				text.Length == 0
					? Array.Empty<FormatRun>()
					: new[] { new FormatRun(text.Length, characterFormat.Clone()) },
				text.Length == 0
					? Array.Empty<ParagraphRun>()
					: new[] { new ParagraphRun(text.Length, paragraphFormat.Clone()) },
				(terminalParagraphState ?? paragraphFormat).Clone(),
				hasExplicitTerminalParagraphState);

		internal static RichTextFragment Empty()
			=> new(
				string.Empty,
				Array.Empty<FormatRun>(),
				Array.Empty<ParagraphRun>(),
				new ParagraphFormatState(),
				true);

		[Conditional("DEBUG")]
		private void AssertRunInvariants()
			=> Debug.Assert(
				AreRunInvariantsValid(),
				"RichTextFragment runs must be positive, coalesced, and cover Text exactly.");

		private static CharacterFormatState GetFormatAt(IReadOnlyList<FormatRun> runs, int position)
		{
			if ((uint)position >= (uint)SumLengths(runs))
			{
				throw new ArgumentOutOfRangeException(nameof(position));
			}

			var end = 0;
			foreach (var run in runs)
			{
				end += run.Length;
				if (position < end)
				{
					return run.Format;
				}
			}

			throw new InvalidOperationException("The character runs do not cover the requested position.");
		}

		private static ParagraphFormatState GetFormatAt(IReadOnlyList<ParagraphRun> runs, int position)
		{
			if ((uint)position >= (uint)SumLengths(runs))
			{
				throw new ArgumentOutOfRangeException(nameof(position));
			}

			var end = 0;
			foreach (var run in runs)
			{
				end += run.Length;
				if (position < end)
				{
					return run.Format;
				}
			}

			throw new InvalidOperationException("The paragraph runs do not cover the requested position.");
		}

		private static List<FormatRun> SliceCharacterRuns(
			IReadOnlyList<FormatRun> source,
			int start,
			int length)
		{
			var result = new List<FormatRun>();
			var sliceEnd = start + length;
			var runStart = 0;
			foreach (var run in source)
			{
				var runEnd = runStart + run.Length;
				var intersectionLength = Math.Min(sliceEnd, runEnd) - Math.Max(start, runStart);
				if (intersectionLength > 0)
				{
					AppendCharacterRun(result, intersectionLength, run.Format);
				}
				if (runEnd >= sliceEnd)
				{
					break;
				}
				runStart = runEnd;
			}

			return result;
		}

		private static List<ParagraphRun> SliceParagraphRuns(
			IReadOnlyList<ParagraphRun> source,
			int start,
			int length)
		{
			var result = new List<ParagraphRun>();
			var sliceEnd = start + length;
			var runStart = 0;
			foreach (var run in source)
			{
				var runEnd = runStart + run.Length;
				var intersectionLength = Math.Min(sliceEnd, runEnd) - Math.Max(start, runStart);
				if (intersectionLength > 0)
				{
					AppendParagraphRun(result, intersectionLength, run.Format);
				}
				if (runEnd >= sliceEnd)
				{
					break;
				}
				runStart = runEnd;
			}

			return result;
		}

		private static void AppendCharacterRun(List<FormatRun> runs, int length, CharacterFormatState format)
		{
			if (length <= 0)
			{
				return;
			}

			if (runs.Count > 0 && CharacterFormatState.CanCoalesce(runs[^1].Format, format))
			{
				runs[^1].Length += length;
			}
			else
			{
				runs.Add(new FormatRun(length, format));
			}
		}

		private static void AppendParagraphRun(List<ParagraphRun> runs, int length, ParagraphFormatState format)
		{
			if (length <= 0)
			{
				return;
			}

			if (runs.Count > 0 && runs[^1].Format.Equals(format))
			{
				runs[^1].Length += length;
			}
			else
			{
				runs.Add(new ParagraphRun(length, format));
			}
		}

		private static bool AreRunsValid(IReadOnlyList<FormatRun> runs, int expectedLength)
		{
			var length = 0;
			for (var i = 0; i < runs.Count; i++)
			{
				if (runs[i].Length <= 0
					|| runs[i].Format.InlineImage is not null && runs[i].Length != 1
					|| i > 0 && CharacterFormatState.CanCoalesce(runs[i - 1].Format, runs[i].Format))
				{
					return false;
				}
				length = checked(length + runs[i].Length);
			}

			return length == expectedLength;
		}

		private static bool AreRunsValid(IReadOnlyList<ParagraphRun> runs, int expectedLength)
		{
			var length = 0;
			for (var i = 0; i < runs.Count; i++)
			{
				if (runs[i].Length <= 0
					|| i > 0 && runs[i - 1].Format.Equals(runs[i].Format))
				{
					return false;
				}
				length = checked(length + runs[i].Length);
			}

			return length == expectedLength;
		}

		private static int SumLengths(IReadOnlyList<FormatRun> runs)
		{
			var length = 0;
			foreach (var run in runs)
			{
				length = checked(length + run.Length);
			}

			return length;
		}

		private static int SumLengths(IReadOnlyList<ParagraphRun> runs)
		{
			var length = 0;
			foreach (var run in runs)
			{
				length = checked(length + run.Length);
			}

			return length;
		}
	}
}
