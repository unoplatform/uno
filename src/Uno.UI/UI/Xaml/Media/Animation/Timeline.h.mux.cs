// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Timeline.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	private protected virtual void UpdateAnimation(
		in ComputeStateParams parentParams,
		ref ComputeStateParams myParams,
		double beginTime,
		DurationType durationType,
		float durationValue,
		in OptionalDouble expirationTime)
	{
#if HAS_UNO
		// TODO Uno specific: CAnimation is folded into Timeline (the public animations derive from Timeline directly).
		if (IsAnimation())
		{
			AnimationUpdateAnimation(parentParams, ref myParams, beginTime, durationType, durationValue, expirationTime);
		}
#endif
	}

	private protected virtual bool IsDurationForProgressDifferent() => false;

	private protected virtual void GetDurationForProgress(out DurationType pDurationType, out float prDurationValue)
		=> GetNaturalDuration(out pDurationType, out prDurationValue);

	internal virtual void OnBegin()
	{
#if HAS_UNO
		// TODO Uno specific: CAnimation is folded into Timeline.
		if (IsAnimation())
		{
			AnimationOnBegin();
			return;
		}

		// TODO Uno: The theme animations (CDynamicTimeline in WinUI) are not ported. They begin as empty, zero-length
		// timelines, so that the storyboards containing them still run and complete.
		if (IsUnportedThemeTimeline())
		{
			return;
		}
#endif

		throw new InvalidOperationException();
	}

#if !HAS_UNO
	// TODO Uno: DynamicTimeline (theme transitions) is not ported.
	// void SetDynamicTimelineParent(_In_ CTimeline* pGeneratingParent)
	// CTimeline* GetDynamicTimelineParent() const
#endif

	internal void SetTargetProperty(DependencyProperty? pDP) => m_pManualTargetDependencyProperty = pDP;

	internal virtual bool IsInActiveState() => (m_clockState == TimelineClockState.Active) || (m_clockState == TimelineClockState.NotStarted);

	internal virtual bool IsInStoppedState() => (m_clockState == TimelineClockState.Stopped) || (m_clockState == TimelineClockState.NotStarted);

	internal bool HasPendingThemeChange() => m_hasPendingThemeChange;

	internal bool IsCompletedEventFired() => m_IsCompletedEventFired;

	internal virtual bool IsAnimation() => false;

	internal bool IsInTimeManager() => m_fIsInTimeManager;

	internal double m_rCurrentTime;

	// TODO Uno specific: WinUI reads these from the property system storage; Uno caches them on property changes.
	private protected RepeatBehavior m_repeatBehavior = new RepeatBehavior(1);
	private protected TimeSpan? m_pBeginTime = TimeSpan.Zero;
	private protected Duration m_duration = Duration.Automatic;

	private protected WeakReference<Timeline>? m_timingParent;
	private protected WeakReference<DependencyObject>? m_manualTargetObjectWeakRef;
	private protected WeakReference<DependencyObject>? m_targetObjectWeakRef;
	private protected DependencyProperty? m_pManualTargetDependencyProperty;
	private protected DependencyProperty? m_pTargetDependencyProperty;

	private protected FillBehavior m_fillBehavior = FillBehavior.HoldEnd;
	internal TimelineClockState m_clockState = TimelineClockState.Stopped;
	private protected float m_rSpeedRatio = 1.0f;
	private protected float m_rCurrentProgress;
	private protected int m_nIteration;
	private protected bool m_fInitialized;
	private protected bool m_fIsInTimeManager;
	private protected bool m_IsCompletedEventFired;
	private int m_completedHandlerRegisteredCount;
	private bool m_hasPendingThemeChange;
	private protected bool m_fAutoReverse;

	private static bool s_allowDependentAnimations = true;

	// The amount of tolerance (in seconds) allowed for floating point error when comparing times
	internal const float s_timeTolerance = 0.0005f; // 0.5 milliseconds
}

/// <summary>
/// WinUI's DirectUI::ClockState: the public <see cref="ClockState"/> values plus the internal NotStarted.
/// </summary>
internal enum TimelineClockState
{
	Active = ClockState.Active,
	Filling = ClockState.Filling,
	Stopped = ClockState.Stopped,
	NotStarted,
}

/// <summary>COptionalDouble</summary>
internal struct OptionalDouble
{
	public bool m_fHasValue;
	public double m_rValue;
}
