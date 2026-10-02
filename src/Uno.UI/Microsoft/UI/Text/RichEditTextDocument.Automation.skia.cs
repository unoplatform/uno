#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text;

internal enum RichEditTextObjectKind
{
	Image,
	Link,
}

internal readonly record struct RichEditTextObjectInfo(
	RichEditTextObjectIdentity Identity,
	RichEditTextObjectKind Kind,
	int Start,
	int End,
	string Name,
	string? Link,
	string? LinkAnchor,
	InlineImageState? Image);

public partial class RichEditTextDocument
{
	internal IReadOnlyList<RichEditTextObjectInfo> GetAutomationTextObjects()
	{
		SyncRunsToLength(_textBuffer.Length);
		if (_runs.Count == 0)
		{
			return Array.Empty<RichEditTextObjectInfo>();
		}

		var objects = new List<RichEditTextObjectInfo>();
		var usedImageIdentities = new HashSet<RichEditTextObjectIdentity>();
		for (var runIndex = 0; runIndex < _runs.Count; runIndex++)
		{
			var run = _runs[runIndex];
			if (run.Format.InlineImage is not { } image)
			{
				continue;
			}

			var runStart = GetRunStart(runIndex);
			var runEnd = _runs.GetEnd(runIndex);
			for (var position = runStart; position < runEnd; position++)
			{
				if (_textBuffer[position] == '\ufffc')
				{
					var identity = run.Format.TextObjectIdentity;
					if (identity is null || !usedImageIdentities.Add(identity))
					{
						identity = new RichEditTextObjectIdentity();
						run.Format.TextObjectIdentity = identity;
						usedImageIdentities.Add(identity);
					}

					objects.Add(new RichEditTextObjectInfo(
						identity,
						RichEditTextObjectKind.Image,
						position,
						position + 1,
						image.AlternateText,
						null,
						null,
						image));
				}
			}
		}

		var usedLinkIdentities = new HashSet<RichEditTextObjectIdentity>();
		for (var runIndex = 0; runIndex < _runs.Count;)
		{
			var format = _runs[runIndex].Format;
			if (format.Link is not { } link)
			{
				runIndex++;
				continue;
			}

			var linkAnchor = format.LinkAnchor;
			var identity = format.TextObjectIdentity;
			var firstRun = runIndex;
			var lastRun = runIndex;
			while (lastRun + 1 < _runs.Count
				&& IsSameLinkObject(format, _runs[lastRun + 1].Format))
			{
				lastRun++;
			}
			if (identity is null || !usedLinkIdentities.Add(identity))
			{
				identity = new RichEditTextObjectIdentity();
				for (var identityRun = firstRun; identityRun <= lastRun; identityRun++)
				{
					_runs[identityRun].Format.TextObjectIdentity = identity;
				}
				usedLinkIdentities.Add(identity);
			}

			var start = GetRunStart(firstRun);
			var end = _runs.GetEnd(lastRun);
			objects.Add(new RichEditTextObjectInfo(
				identity,
				RichEditTextObjectKind.Link,
				start,
				end,
				GetTextInRange(start, end, TextGetOptions.UseObjectText),
				link,
				linkAnchor,
				null));
			runIndex = lastRun + 1;
		}

		objects.Sort(static (left, right) =>
		{
			var startComparison = left.Start.CompareTo(right.Start);
			return startComparison != 0
				? startComparison
				: left.Kind.CompareTo(right.Kind);
		});
		return objects;

		static bool IsSameLinkObject(CharacterFormatState left, CharacterFormatState right)
			=> left.Link is not null
				&& right.Link is not null
				&& CharacterFormatState.IsSameTextObject(left, right);
	}

	internal bool? GetAutomationLinkState(int start, int end)
	{
		start = Math.Clamp(start, 0, _textBuffer.Length);
		end = Math.Clamp(end, start, _textBuffer.Length);
		var objects = GetAutomationTextObjects();
		if (start == end)
		{
			foreach (var item in objects)
			{
				if (item.Kind == RichEditTextObjectKind.Link
					&& item.Start <= start
					&& start < item.End)
				{
					return true;
				}
			}

			return false;
		}

		var coveredUntil = start;
		var foundLink = false;
		foreach (var item in objects)
		{
			if (item.Kind != RichEditTextObjectKind.Link
				|| item.End <= start
				|| item.Start >= end)
			{
				continue;
			}

			foundLink = true;
			if (item.Start > coveredUntil)
			{
				return null;
			}

			coveredUntil = Math.Max(coveredUntil, Math.Min(end, item.End));
			if (coveredUntil == end)
			{
				return true;
			}
		}

		return foundLink ? null : false;
	}
}
