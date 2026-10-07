// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SortIndicator\SortIndicator.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Private.Controls;

partial class SortIndicator
{
	private const string s_NoSortStateName = "NoSort";
	private const string s_AscendingStateName = "Ascending";
	private const string s_DescendingStateName = "Descending";
	private const string s_LayoutRootPartName = "LayoutRoot";
	private const string s_GlyphIconPartName = "GlyphIcon";

	// Segoe Fluent Icons ScrollChevronUpLegacy / ScrollChevronDownLegacy -- chevrons, not the
	// SortUp/SortDown glyphs, because a chevron reads correctly at header scale.
	private const string s_AscendingGlyph = "\uE96D";
	private const string s_DescendingGlyph = "\uE96E";

	public SortIndicator()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_SortIndicator);

		this.SetTabularDefaultStyleKey();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// Opacity/Glyph are driven imperatively: WinUI 3 VisualState.Setters can fail to re-apply
		// Opacity on NoSort -> Ascending (microsoft-ui-xaml#6203), leaving the chevron invisible.
		m_layoutRoot = GetTemplateChild(s_LayoutRootPartName) as FrameworkElement;
		m_glyphIcon = GetTemplateChild(s_GlyphIconPartName) as FontIcon;

		// Missing either part renders nothing with no other symptom; catch it in chk.
		MUX_ASSERT(m_layoutRoot != null);
		MUX_ASSERT(m_glyphIcon != null);

		UpdateVisualState(false /* useTransitions */);
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new SortIndicatorAutomationPeer(this);

	private void OnDirectionPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateVisualState(true /* useTransitions */);
	}

	private new void UpdateVisualState(bool useTransitions)
	{
		var direction = Direction;

		bool hasSort = default;
		string glyph = s_AscendingGlyph;
		string directionStateName = string.Empty;
		switch (direction)
		{
			case SortIndicatorDirection.Ascending:
				hasSort = true;
				glyph = s_AscendingGlyph;
				directionStateName = s_AscendingStateName;
				break;
			case SortIndicatorDirection.Descending:
				hasSort = true;
				glyph = s_DescendingGlyph;
				directionStateName = s_DescendingStateName;
				break;
			case SortIndicatorDirection.None:
			default:
				hasSort = false;
				glyph = s_AscendingGlyph;
				directionStateName = s_NoSortStateName;
				break;
		}

		// GoToState first, so the imperative writes below win over any consumer VSM Setter.
		VisualStateManager.GoToState(this, directionStateName, useTransitions);

		if (m_layoutRoot is { } layoutRoot)
		{
			layoutRoot.Opacity = hasSort ? 1.0 : 0.0;
		}
		if (m_glyphIcon is { } glyphIcon)
		{
			// Opacity above hides it when None; still set a glyph so debugger inspection is sane.
			glyphIcon.Glyph = glyph;
		}
	}
}
