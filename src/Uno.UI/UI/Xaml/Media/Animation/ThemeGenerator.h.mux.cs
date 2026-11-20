// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ThemeGenerator.h, tag winui3/release/2.5.1, commit ba3a8d59e

//  Abstract:
//      Provides an API for ThemeGenerator implementation.
//
// TODO Uno: The debug-only ThemeAnimationSlowDownFactor (m_dbgSlowDownFactor) is not ported.

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

internal sealed class TimingFunctionDescription
{
	internal Point cp1 = new(0.0f, 0.0f);
	internal Point cp2 = new(0.0f, 0.0f);
	internal Point cp3 = new(1.0f, 1.0f);
	internal Point cp4 = new(1.0f, 1.0f);

	internal bool IsLinear() =>
		cp1.X == 0.0f && cp1.Y == 0.0f && cp2.X == 0.0f && cp2.Y == 0.0f && cp3.X == 1.0f && cp3.Y == 1.0f && cp4.X == 1.0f && cp4.Y == 1.0f;
}

//------------------------------------------------------------------------
// helper class. Responsibility: dealing with Storyboard specifics.
//
// Keyframes have to be grouped into keyframecollections
// This class takes care of carrying that state and is a 1-1 mapping to one
// call to one of the generate storyboards methods.
//------------------------------------------------------------------------
internal sealed partial class ThemeGeneratorHelper
{
	// we are going to create animations. All these keyframes are going to have to
	// be put into doubleanimationusingkeyframes.
	// this is a mapping between the propertyname and the keyframecollection.
	internal ThemeGeneratorHelper(
		Point startOffset,
		Point destinationOffset,
		string? targetName,
		DependencyObject? target,
		bool onlyGenerateSteadyState,
		IList<Timeline> timelineCollection)
	{
		m_pTimelines = timelineCollection;
		m_startOffset = startOffset;
		m_destinationOffset = destinationOffset;
		m_targetName = targetName;
		m_onlyGenerateSteadyState = onlyGenerateSteadyState;
		m_target = target;
	}

	private readonly IList<Timeline> m_pTimelines;

	private long m_begintime;   // the begintime set on the timeline
	private long m_additionalTime; // additional time, that might be used

	private bool m_originValuesSet;
	private bool m_clipValuesSet;
	private readonly bool m_onlyGenerateSteadyState;

	private readonly Point m_startOffset;
	private readonly Point m_destinationOffset;

	// m_pActivationFactory/m_pStoryboardStatics: the static Storyboard.SetTarget* methods are used directly.

	private string? m_strTranslateXPropertyName;
	private string? m_strTranslateYPropertyName;
	private string? m_strOpacityPropertyName;
	private string? m_strCenterYPropertyName;
	private string? m_strScaleXPropertyName;
	private string? m_strScaleYPropertyName;
	private string? m_strClipScaleXPropertyName;
	private string? m_strClipScaleYPropertyName;
	private string? m_strClipTranslateXPropertyName;
	private string? m_strClipTranslateYPropertyName;

	private readonly string? m_targetName;
	private DependencyObject? m_target;
	private string? m_strOverrideTranslateXPropertyName;
	private string? m_strOverrideTranslateYPropertyName;
	private readonly Dictionary<string, DoubleAnimationUsingKeyFrames> m_doubleAnimationsMap = new();
	// TODO Uno: m_pointerAnimationsMap / RegisterPointerKeyFrame not ported (no PointerAnimationUsingKeyFrames).

	private double m_initialOpacity;
	private bool m_overrideInitialOpacity;

	internal Point GetStartOffset() => m_startOffset;
	internal Point GetDestinationOffset() => m_destinationOffset;

	internal string GetTranslateXPropertyName() => m_strOverrideTranslateXPropertyName ?? m_strTranslateXPropertyName!;
	internal string GetTranslateYPropertyName() => m_strOverrideTranslateYPropertyName ?? m_strTranslateYPropertyName!;
	internal string GetOpacityPropertyName() => m_strOpacityPropertyName!;
	internal string GetCenterYPropertyName() => m_strCenterYPropertyName!;
	internal string GetScaleXPropertyName() => m_strScaleXPropertyName!;
	internal string GetScaleYPropertyName() => m_strScaleYPropertyName!;
	internal string GetClipScaleXPropertyName() => m_strClipScaleXPropertyName!;
	internal string GetClipScaleYPropertyName() => m_strClipScaleYPropertyName!;
	internal string GetClipTranslateXPropertyName() => m_strClipTranslateXPropertyName!;
	internal string GetClipTranslateYPropertyName() => m_strClipTranslateYPropertyName!;

	// TODO Uno: PreventHitTestingWhileAnimating is not ported (no caller).

	internal void SetOverrideTranslateXPropertyName(string value) => m_strOverrideTranslateXPropertyName = value;

	internal void SetOverrideTranslateYPropertyName(string value) => m_strOverrideTranslateYPropertyName = value;

	internal void SetOverrideInitialOpacity(double initialOpacity)
	{
		m_initialOpacity = initialOpacity;
		m_overrideInitialOpacity = true;
	}

	internal bool GetInitialOpacity(ref double fallbackValue)
	{
		fallbackValue = m_initialOpacity;
		return m_overrideInitialOpacity;
	}

	internal void SetAdditionalTime(long time) => m_additionalTime = time;
	internal long GetAdditionalTime() => m_additionalTime;
}

internal static partial class ThemeGenerator
{
	// Storyboard and target ids from vsanimation.h.
	internal const int TAS_FADEIN = 4;
	internal const int TA_FADEIN_SHOWN = 1;
	internal const int TAS_FADEOUT = 5;
	internal const int TA_FADEOUT_HIDDEN = 1;
	internal const int TAS_DRAGSOURCESTART = 22;
	internal const int TA_DRAGSOURCESTART_DRAGSOURCE = 1;
	internal const int TA_DRAGSOURCESTART_AFFECTED = 2;
	internal const int TAS_DRAGBETWEENENTER = 27;
	internal const int TA_DRAGBETWEENENTER_AFFECTED = 1;
	internal const int TAS_DRAGBETWEENLEAVE = 28;
	internal const int TA_DRAGBETWEENLEAVE_AFFECTED = 1;

	// TODO Uno: GetStaggerFunction is not ported (no PVLStaggerFunction, no caller in the item chrome).
}
