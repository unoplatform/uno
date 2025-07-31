// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControl.h, tag winui3/release/1.7.3, commit 65718e2813a90f

#nullable enable

using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

internal enum RatingControlStates
{
	Disabled = 0,
	Set = 1,
	PointerOverSet = 2,
	PointerOverPlaceholder = 3, // Also functions as the pointer over unset state at the moment
	Placeholder = 4,
	Unset = 5,
	Null = 6
}

internal enum RatingInfoType
{
	None,
	Font,
	Image
}

partial class RatingControl
{
	private bool IsItemInfoPresentAndFontInfo() => m_infoType == RatingInfoType.Font;

	private bool IsItemInfoPresentAndImageInfo() => m_infoType == RatingInfoType.Image;

	// Private members
	private StackPanel? m_captionStackPanel;
	private TextBlock? m_captionTextBlock;

	private CompositionPropertySet? m_sharedPointerPropertySet;

	private StackPanel? m_backgroundStackPanel;
	private StackPanel? m_foregroundStackPanel;

	private TranslateTransform? m_backgroundStackPanelTranslateTransform;
	private TranslateTransform? m_foregroundStackPanelTranslateTransform;

	private bool m_isPointerOver;
	private bool m_isPointerDown;
	private bool m_hasPointerCapture;
	private double m_mousePercentage;
	private double m_firstItemOffset;

	private RatingInfoType m_infoType = RatingInfoType.Font;

	// Holds the value of the Rating control at the moment of engagement,
	// used to handle cancel-disengagements where we reset the value.
	private double m_preEngagementValue;
	private bool m_disengagedWithA;
	private bool m_shouldDiscardValue = true;

	private long m_fontFamilyChangedToken;

	private DispatcherHelper? m_dispatcherHelper;

	private const double c_defaultFontSizeForRendering = 32.0;
	private const double c_defaultItemSpacing = 8.0;
	private const double c_defaultCaptionTopMargin = -6.0;

	private bool m_resourcesLoaded;
	private double m_fontSizeForRendering = c_defaultFontSizeForRendering;
	private double m_itemSpacing = c_defaultItemSpacing;
	private double m_captionTopMargin = c_defaultCaptionTopMargin;

	private double m_scaledFontSizeForRendering = -1.0;
}
