// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlock.cpp, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using System.Globalization;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.Dispatching;
using Windows.Globalization;

namespace Microsoft.UI.Xaml.Controls;

partial class TextBlock
{
	// TextBlock.cpp, line 452
	//------------------------------------------------------------------------
	//
	//  Method:   CTextBlock::UpdateFastPathOptOutConditions
	//
	//  Synopsis: When some properties are changed, we need to update the internal TextBlock flags.
	//  These flags are used to determine whether we can use the fast path(IDWriteTextLayout) or not.
	//
	//------------------------------------------------------------------------
	private void UpdateFastPathOptOutConditions(DependencyProperty pDP)
	{
		if (pDP == CharacterSpacingProperty)
		{
			SetFastPathOptOutConditions(FastPathOptOutConditions.CharacterSpacing_Not_Default, CharacterSpacing != 0);
		}
		else if (pDP == TextTrimmingProperty)
		{
			SetFastPathOptOutConditions(FastPathOptOutConditions.TextTrimming_Is_Clip, TextTrimming == TextTrimming.Clip);
		}
	}

	/// <summary>
	/// Gets the distance between the top of the <see cref="TextBlock"/> and the
	/// baseline of its first line of text.
	/// </summary>
	public double BaselineOffset
	{
		get
		{
			GetBaselineOffset(out var baselineOffset);
			return baselineOffset;
		}
	}

	// CTextBlock::GetBaselineOffset
	//
	// Synopsis: Gets distance to baseline of first line from top of control.
	//
	// A plain TextBlock takes WinUI's default TextMode::DWriteLayout branch, which returns the first
	// line's baseline straight from the layout metrics — measured from the content box, so padding is
	// NOT included (WinUI adds padding separately, e.g. in GetActualHeight). Uno's kept ParsedText
	// engine measures the same content box, so FirstLineBaseline matches directly (and returns 0
	// before the first measure, matching WinUI's m_hasBeenMeasured guard).
	internal void GetBaselineOffset(out float pBaselineOffset)
		=> pBaselineOffset = ParsedText.FirstLineBaseline;

	// TextBlock.cpp, line 885
	// Called when the BlockCollection is dirtied.
	// TODO Uno: Only the fast path opt-out is ported; invalidation goes through InvalidateTextBlock.
	internal void OnContentChanged()
	{
		// The Text setter rebuilds the inlines and then clears HasComplexContent.
		if (!_skipInlinesChangedTextSetter && _inlines is { Count: > 0 })
		{
			SetFastPathOptOutConditions(FastPathOptOutConditions.HasComplexContent);
		}
	}

	// TextBlock.cpp, line 1935
	//------------------------------------------------------------------------
	//
	//  Method:  UpdateIsTextTrimmedFastPath
	//
	//  Synopsis: Update whether the text of the block has been trimmed or
	//            not and raise an event if the state of trimming has changed.
	//
	//------------------------------------------------------------------------
	private void UpdateIsTextTrimmedFastPath()
	{
		var last_isTextTrimmed = IsTextTrimmed;

		// DWrite's isTrimmed on the last line. An empty text block cannot be trimmed.
		var isTextTrimmed = ParsedText is UnicodeText text && text.IsLastLineTrimmed;

		if (last_isTextTrimmed != isTextTrimmed)
		{
			RaiseIsTextTrimmedChangedEvent();

			IsTextTrimmed = isTextTrimmed;
		}
	}

	// TextBlock.cpp, line 1979
	//------------------------------------------------------------------------
	//
	//  Method:  UpdateIsTextTrimmedSlowPath
	//
	//  Synopsis: Update whether the text of the block has been trimmed or
	//            not and raise an event if the state of trimming has changed.
	//
	//------------------------------------------------------------------------
	private void UpdateIsTextTrimmedSlowPath(double collapsingWidth, double lineWidth)
	{
		// ParagraphNode::GetHasTrimmedLine. UnicodeText collapses ellipsis lines itself; a Clip line is only compared here.
		var isTrimmed = ParsedText is UnicodeText text
			&& (text.HasTrimmedLine || (TextTrimming == TextTrimming.Clip && IsTrimmingNeeded(collapsingWidth, lineWidth)));

		var last_isTextTrimmed = IsTextTrimmed;
		if (last_isTextTrimmed != isTrimmed)
		{
			RaiseIsTextTrimmedChangedEvent();

			IsTextTrimmed = isTrimmed;
		}
	}

	// ParagraphNode.cpp, line 1185 (ParagraphNode::TrimLineIfNecessary). The widest line decides for all of them,
	// since LayoutRound is monotonic.
	private bool IsTrimmingNeeded(double collapsingWidth, double lineWidth)
	{
		var trimmingNeeded = false;
		if (collapsingWidth < lineWidth)
		{
			if (GetUseLayoutRounding())
			{
				//
				// If we're using layout rounding it's possible we've been given a slightly smaller width,
				// so check to see if our layout-rounded line width is actually less than the arrange width
				// before trimming.  If we choose not to apply trimming here it will be because we're only
				// off by a fraction of a pixel and our measure width really does still fit the provided
				// arrange width.  This means that we will still bubble up to FrameworkElement that our
				// render size is technically larger than the arrange width, but FrameworkElement LayoutRound()s
				// the render size we return before deciding if it should clip, and the same width that was
				// returned in measure gets rounded down to the same as the arrange width and no layout clip
				// needs to be applied.
				//

				var layoutRoundedLineWidth = LayoutRound(lineWidth);
				if (collapsingWidth < layoutRoundedLineWidth)
				{
					trimmingNeeded = true;
				}
			}
			else
			{
				trimmingNeeded = true;
			}
		}

		return trimmingNeeded;
	}

	// TextBlock.cpp, line 2022
	//------------------------------------------------------------------------
	//
	//  Method:   CTextBlock::RaiseIsTextTrimmedChangedEvent
	//
	//  Synopsis: Raises event for IsTextTrimmedChanged.
	//
	//------------------------------------------------------------------------
	private void RaiseIsTextTrimmedChangedEvent()
	{
		// CEventManager::Raise skips senders without listeners, then posts the event (fRaiseSync is false).
		if (_isTextTrimmedChanged is null)
		{
			return;
		}

		NativeDispatcher.Main.Enqueue(() => _isTextTrimmedChanged?.Invoke(this, new()), NativeDispatcherPriority.Normal);
	}

	// TextBlock.cpp, line 2970
	private void SetFastPathOptOutConditions(FastPathOptOutConditions mask) => _fastPathOptOutConditions |= mask;

	private void ClearFastPathOptOutConditions(FastPathOptOutConditions mask) => _fastPathOptOutConditions &= ~mask;

	private void SetFastPathOptOutConditions(FastPathOptOutConditions mask, bool isSet)
	{
		if (isSet)
		{
			SetFastPathOptOutConditions(mask);
		}
		else
		{
			ClearFastPathOptOutConditions(mask);
		}
	}

	// TextBlock.cpp, line 2995
	// Determine the layout mode, either Normal(LS) or DWriteTextLayout.
	private void ResolveTextMode()
	{
		var canUseDWriteLayout = false;

		if (_fastPathOptOutConditions == 0)
		{
			// If all the above tests passed, we also need to make sure the language does not need number substitution,
			// because there is currently a behavior difference in DWTL for number substitution.
			// We need to check here rather than in UpdateFastPathOptOutConditions because the Language is an inherited property,
			// and we cannot rely on OnPropertyChanged notifications when the inherited value changes.
			var needNumberSubstitution = NeedNumberSubsitution();
			canUseDWriteLayout = !needNumberSubstitution;
		}

		if (_textMode == TextMode.Normal)
		{
			if (canUseDWriteLayout)
			{
				_textMode = TextMode.DWriteLayout;
			}
		}
		else
		{
			if (!canUseDWriteLayout)
			{
#if !HAS_UNO
				IFC_RETURN(DeferredCreateInlineCollection());
#endif
				_textMode = TextMode.Normal;
#if !HAS_UNO
				m_pTextLayout = nullptr;
				m_pTextDrawingContext.reset();
#endif
			}
		}
	}

	private static (string Language, bool IsNeeded)? _lastNumberSubstitution;

	// TextBlock.cpp, line 3040
	private bool NeedNumberSubsitution()
	{
		// TODO Uno: FrameworkElement.Language isn't inherited yet, so only a value set on this TextBlock is seen.
		// TextFormatting falls back to PALFontAndScriptServices::GetDefaultLanguageString.
		var language = GetValue(LanguageProperty) as string;
		if (string.IsNullOrEmpty(language))
		{
			language = ApplicationLanguages.Languages is { Count: > 0 } languages ? languages[0] : "en-US";
		}

		if (_lastNumberSubstitution is { } last && last.Language == language)
		{
			return last.IsNeeded;
		}

		// DWriteFontAndScriptServices::GetNumberSubstitution returns no substitution for an empty or unknown locale,
		// or one whose LOCALE_IDIGITSUBSTITUTION is None.
		bool isNeeded;
		try
		{
			isNeeded = !string.IsNullOrEmpty(language)
				&& CultureInfo.GetCultureInfo(language).NumberFormat.DigitSubstitution != DigitShapes.None;
		}
		catch (CultureNotFoundException)
		{
			isNeeded = false;
		}

		_lastNumberSubstitution = (language, isNeeded);
		return isNeeded;
	}
}
