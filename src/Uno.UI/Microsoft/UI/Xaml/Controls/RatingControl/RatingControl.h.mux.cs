// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControl.h, tag winui3/release/1.5.3, commit 2a60e27c591846556fa9ec4d8f305afdf0f96dc1

#nullable enable

using Uno.UI.Helpers.WinUI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

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
	private TextBlock? m_captionTextBlock;

	private CompositionPropertySet? m_sharedPointerPropertySet;

	private StackPanel? m_backgroundStackPanel;
	private StackPanel? m_foregroundStackPanel;

	private bool m_isPointerOver;
	private bool m_isPointerDown;
	private bool m_hasPointerCapture;
	private double m_mousePercentage;

	private RatingInfoType m_infoType = RatingInfoType.Font;

	// Holds the value of the Rating control at the moment of engagement,
	// used to handle cancel-disengagements where we reset the value.
	private double m_preEngagementValue;
	private bool m_disengagedWithA;
	private bool m_shouldDiscardValue = true;

	private long m_fontFamilyChangedToken;

	private DispatcherHelper? m_dispatcherHelper;
}
