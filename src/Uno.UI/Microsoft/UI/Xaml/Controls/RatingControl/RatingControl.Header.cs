// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControl.h, commit b853109

#nullable enable

using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class RatingControl
{
	private bool IsItemInfoPresentAndFontInfo()
	{
		return m_infoType == RatingInfoType.Font;
	}
	private bool IsItemInfoPresentAndImageInfo()
	{
		return m_infoType == RatingInfoType.Image;
	}

	// Private members
	private TextBlock? m_captionTextBlock = null;

	private CompositionPropertySet? m_sharedPointerPropertySet = null;

	private StackPanel? m_backgroundStackPanel = null;
	private StackPanel? m_foregroundStackPanel = null;

	private bool m_isPointerOver = false;
	private bool m_isPointerDown = false;
	private double m_mousePercentage = 0.0;

	private RatingInfoType m_infoType = RatingInfoType.Font;

	// Holds the value of the Rating control at the moment of engagement,
	// used to handle cancel-disengagements where we reset the value.
	private double m_preEngagementValue = 0.0;
	private bool m_disengagedWithA = false;
	private bool m_shouldDiscardValue = true;

	private long m_fontFamilyChangedToken;

	private DispatcherHelper? m_dispatcherHelper = null;
}
