// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextAdapter_Partial.cpp, tag winui3/release/1.5-stable
//
// Minimal ITextProvider implementation that exposes the owning element's plain
// text as a single document range. Sufficient for Narrator read-out and Inspect
// pattern discovery. Win32 projects this adapter through Text, Text2, and the
// platform-only TextEdit resolver, matching WinUI's windowless RichEdit split.

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace DirectUI;

internal sealed class TextAdapter : ITextProvider, ITextProvider2, ITextEditProvider
{
	private readonly AutomationPeer _ownerPeer;
	private readonly FrameworkElement _owner;

	public TextAdapter(TextBlock owner)
		: this(owner, owner.GetOrCreateAutomationPeer()!)
	{
	}

	internal TextAdapter(FrameworkElement owner, AutomationPeer ownerPeer)
	{
		_owner = owner;
		_ownerPeer = ownerPeer;
	}

	public FrameworkElement Owner => _owner;

	/// <summary>
	/// The text content surfaced through the Text pattern. Differs from
	/// <c>FrameworkElement.GetPlainText()</c>, which Uno overrides on TextBox to
	/// return Header/PlaceholderText — that's the wrong source for TextPattern.
	/// PasswordBox returns a masked string so the actual password never leaves
	/// the control via UIA.
	/// </summary>
	internal static string GetEffectiveText(FrameworkElement owner) => owner switch
	{
		PasswordBox passwordBox => new string('•', passwordBox.Password?.Length ?? 0),
		TextBox textBox => textBox.Text ?? string.Empty,
		TextBlock textBlock => textBlock.Text ?? string.Empty,
		RichEditBox richEditBox => TryGetRichEditPlainText(richEditBox),
		_ => owner.GetPlainText() ?? string.Empty,
	};

	private static string TryGetRichEditPlainText(RichEditBox richEditBox)
	{
		string text = string.Empty;
		try
		{
			richEditBox.Document?.GetText(Microsoft.UI.Text.TextGetOptions.None, out text);
			return text is { Length: > 0 } && text[^1] == '\r'
				? text[..^1]
				: text ?? string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	internal static int GetEffectiveTextLength(FrameworkElement owner)
	{
#if __SKIA__
		if (owner is RichEditBox richEditBox)
		{
			return richEditBox.Document.TextLength;
		}
#endif

		return GetEffectiveText(owner).Length;
	}

	public ITextRangeProvider DocumentRange
		=> new TextRangeAdapter(_ownerPeer, _owner, 0, GetEffectiveTextLength(_owner));

	public SupportedTextSelection SupportedTextSelection
		=> _owner is TextBox or RichEditBox ? SupportedTextSelection.Single : SupportedTextSelection.None;

	public ITextRangeProvider[] GetSelection()
	{
		if (_owner is TextBox textBox)
		{
			var start = textBox.SelectionStart;
			var length = textBox.SelectionLength;
			return new ITextRangeProvider[]
			{
				new TextRangeAdapter(_ownerPeer, _owner, start, start + length),
			};
		}

		if (_owner is RichEditBox richEditBox)
		{
			var selection = richEditBox.Document.Selection;
			return new ITextRangeProvider[]
			{
				new TextRangeAdapter(_ownerPeer, _owner, selection.StartPosition, selection.EndPosition),
			};
		}

		return Array.Empty<ITextRangeProvider>();
	}

	public ITextRangeProvider[] GetVisibleRanges()
	{
#if __SKIA__
		if (_owner is RichEditBox richEditBox
			&& richEditBox.Document.TryGetVisibleRange(out var start, out var end))
		{
			return new ITextRangeProvider[]
			{
				new TextRangeAdapter(_ownerPeer, _owner, start, end),
			};
		}
#endif

		return new ITextRangeProvider[] { DocumentRange };
	}

	public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
	{
#if __SKIA__
		if (_ownerPeer is RichEditBoxAutomationPeer peer
			&& childElement.AutomationPeer is { } childPeer
			&& peer.TryGetTextObjectRange(childPeer, out var start, out var end))
		{
			if (childPeer is RichEditBoxTextObjectAutomationPeer textObjectPeer
				&& textObjectPeer.CreateTextRangeProvider() is { } childRange)
			{
				return childRange;
			}

			return new TextRangeAdapter(
				_ownerPeer,
				_owner,
				start,
				end,
				useObjectText: childPeer is RichEditBoxImageAutomationPeer);
		}
#endif

		return null!;
	}

	public ITextRangeProvider RangeFromPoint(Point screenLocation)
	{
		if (_owner is RichEditBox richEditBox)
		{
			var range = richEditBox.Document.GetRangeFromPoint(screenLocation, Microsoft.UI.Text.PointOptions.None);
			return new TextRangeAdapter(_ownerPeer, _owner, range.StartPosition, range.EndPosition);
		}

		return DocumentRange;
	}

	public ITextRangeProvider RangeFromAnnotation(IRawElementProviderSimple annotationElement)
	{
#if __SKIA__
		if (_ownerPeer is RichEditBoxAutomationPeer peer
			&& annotationElement.AutomationPeer is RichEditBoxSpellingErrorAutomationPeer spellingErrorPeer
			&& peer.TryGetSpellingAnnotationRange(spellingErrorPeer, out var start, out var end))
		{
			return spellingErrorPeer.CreateTextRangeProvider()
				?? new TextRangeAdapter(_ownerPeer, _owner, start, end);
		}
#endif

		return null!;
	}

	public ITextRangeProvider GetCaretRange(out bool isActive)
	{
		isActive = _owner.FocusState != FocusState.Unfocused;

		if (_owner is Microsoft.UI.Xaml.Controls.TextBox textBox)
		{
			var caret = textBox.SelectionStart + textBox.SelectionLength;
			return new TextRangeAdapter(_ownerPeer, _owner, caret, caret);
		}

		if (_owner is RichEditBox richEditBox)
		{
			var selection = richEditBox.Document.Selection;
			var caret = selection.Options.HasFlag(Microsoft.UI.Text.SelectionOptions.StartActive)
				? selection.StartPosition
				: selection.EndPosition;
			return new TextRangeAdapter(_ownerPeer, _owner, caret, caret);
		}

		return new TextRangeAdapter(_ownerPeer, _owner, 0, 0);
	}

	public ITextRangeProvider GetActiveComposition()
	{
#if __SKIA__
		if (_owner is RichEditBox richEditBox
			&& richEditBox.TryGetAccessibilityCompositionRange(conversionTarget: false, out var start, out var end))
		{
			return new TextRangeAdapter(_ownerPeer, _owner, start, end);
		}
#endif

		return null!;
	}

	public ITextRangeProvider GetConversionTarget()
	{
#if __SKIA__
		if (_owner is RichEditBox richEditBox
			&& richEditBox.TryGetAccessibilityCompositionRange(conversionTarget: true, out var start, out var end))
		{
			return new TextRangeAdapter(_ownerPeer, _owner, start, end);
		}
#endif

		return null!;
	}
}
