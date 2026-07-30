// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextAdapter_Partial.cpp, tag winui3/release/1.5-stable
//
// Minimal ITextProvider implementation that exposes the owning element's plain
// text as a single document range. Sufficient for Narrator read-out and Inspect
// pattern discovery. ITextProvider2 / ITextEditProvider are not implemented —
// WinUI 3 sources those from a windowless RichEdit which Uno's Skia stack
// doesn't host.

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
		// RichEditTextDocument.GetText is NotImplemented on some Uno platforms — fall
		// back to GetPlainText (Header / PlaceholderText) so Narrator gets a usable
		// announcement instead of silence.
		string text = string.Empty;
		try
		{
			richEditBox.Document?.GetText(Microsoft.UI.Text.TextGetOptions.None, out text);
		}
		catch
		{
		}

		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}

		return richEditBox.GetPlainText() ?? string.Empty;
	}

	public ITextRangeProvider DocumentRange
		=> new TextRangeAdapter(_ownerPeer, _owner, 0, GetEffectiveText(_owner).Length);

	public SupportedTextSelection SupportedTextSelection
		=> _owner is TextBox ? SupportedTextSelection.Single : SupportedTextSelection.None;

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

		return Array.Empty<ITextRangeProvider>();
	}

	public ITextRangeProvider[] GetVisibleRanges()
		=> new ITextRangeProvider[] { DocumentRange };

	public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
		=> DocumentRange;

	public ITextRangeProvider RangeFromPoint(Point screenLocation)
		=> DocumentRange;

	// ITextProvider2 — minimal stubs so Inspect lists the pattern. Annotations and
	// caret-range queries return a document-wide range; clients that need precise
	// caret tracking will need a richer implementation (see WinUI's windowless
	// RichEdit, which Uno's Skia stack doesn't host).

	public ITextRangeProvider RangeFromAnnotation(IRawElementProviderSimple annotationElement)
		=> DocumentRange;

	public ITextRangeProvider GetCaretRange(out bool isActive)
	{
		isActive = _owner is Microsoft.UI.Xaml.Controls.TextBox;

		if (_owner is Microsoft.UI.Xaml.Controls.TextBox textBox)
		{
			var caret = textBox.SelectionStart + textBox.SelectionLength;
			return new TextRangeAdapter(_ownerPeer, _owner, caret, caret);
		}

		return new TextRangeAdapter(_ownerPeer, _owner, 0, 0);
	}

	// ITextEditProvider — minimal stubs. Active composition and conversion target are
	// IME-driven state Uno doesn't currently track end-to-end on Skia.

	public ITextRangeProvider GetActiveComposition()
		=> new TextRangeAdapter(_ownerPeer, _owner, 0, 0);

	public ITextRangeProvider GetConversionTarget()
		=> new TextRangeAdapter(_ownerPeer, _owner, 0, 0);
}
