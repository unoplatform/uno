// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Timeline.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	// CoreImports::Timeline_GetClockState
	internal static ClockState Timeline_GetClockState(Timeline timeline)
	{
		if (timeline.m_clockState == TimelineClockState.NotStarted)
		{
			return ClockState.Active;
		}
		else
		{
			return (ClockState)timeline.m_clockState;
		}
	}

	// CoreImports::Timeline_GetCurrentTime
	internal static double Timeline_GetCurrentTime(Timeline timeline) => timeline.m_rCurrentTime;

	// CoreImports::Timeline_ResolveName
	internal static DependencyObject? Timeline_ResolveName(Timeline timeline, string strName)
		=> timeline.ResolveName(strName, null /* pParentTimeline */);

#if !HAS_UNO
	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// Original destructor logic (not executed):
	// ReleaseInterface(m_pBeginTime);
	// if (m_wucAnimator)
	// {
	//     // If we have a WUC animation, make sure it's stopped when we're released. Otherwise we
	//     // could keep the WUC animation alive and playing until someone else sets a different value on the target property.
	//     // This can lead to infinite animations looping forever even after they've been released by Xaml.
	//     CompositionAnimationHelper helper;
	//     IGNOREHR(helper.Stop(m_wucAnimator.Get()));
	// }
	// ReleaseTarget();
	// ReleaseInterface(m_pDynamicTimelineParent);
#endif

	// Releases the WeakRef to the target object.
	private protected void ReleaseTarget()
	{
#if !HAS_UNO
		DetachDCompAnimationInstancesFromTarget();
#endif
		m_targetObjectWeakRef = null;
		m_pTargetDependencyProperty = null;
	}

	// CTimeline::SetValue
	private void OnSpeedRatioChanged(double newValue)
	{
		m_rSpeedRatio = (float)newValue;

		// Validate parameters
		if (m_rSpeedRatio <= 0.0f)
		{
			m_rSpeedRatio = 1.0f;
			throw new ArgumentException("SpeedRatio must be greater than zero.");
		}
	}

	internal void ComputeState(in ComputeStateParams parentParams)
	{
		var dontCare = true;
		ComputeState(parentParams, ref dontCare, hasExternalReferencesTracking: false);
	}

	internal void ComputeState(in ComputeStateParams parentParams, ref bool pHasNoExternalReferences)
		=> ComputeState(parentParams, ref pHasNoExternalReferences, hasExternalReferencesTracking: true);

	private void ComputeState(in ComputeStateParams parentParams, ref bool pHasNoExternalReferences, bool hasExternalReferencesTracking)
	{
		var myParams = parentParams;
		myParams.speedRatio *= m_rSpeedRatio;

#if !HAS_UNO
		myParams.isAncestorWaitingForDCompAnimationCompleted &= m_isWaitingForDCompAnimationCompleted;
		myParams.cannotConvertToCompositionAnimation |= (m_conversionResult != CompositionAnimationConversionResult::Success);

		// Reset independent animation state.  It will be recalculated by this method each tick.
		const bool hadIndependentAnimationLastTick = m_hasIndependentAnimation;
		m_hasIndependentAnimation = FALSE;
#endif

		ComputeStateImpl(parentParams, ref myParams, ref pHasNoExternalReferences, hasExternalReferencesTracking);

#if !HAS_UNO
		m_hasIndependentAnimation = hasIndependentAnimationThisTick;

		// If this timeline has changed from dependent to independent or vice versa, notify the time manager.
		if (hadIndependentAnimationLastTick != hasIndependentAnimationThisTick)
		{
			GetContext()->GetTimeManager()->NotifyIndependentAnimationChange();
		}
#endif

		m_hasPendingThemeChange = false;
	}

	private protected virtual void ComputeStateImpl(
		in ComputeStateParams parentParams,
		ref ComputeStateParams myParams,
		ref bool pHasNoExternalReferences,
		bool hasExternalReferencesTracking)
	{
		var isLocallyReversed = false;
		var rBeginTime = m_pBeginTime?.TotalSeconds ?? 0.0;

		// At this point the timeline computes all timing computations based off the parent time
		// NotStarted until we gather enough data to determine we are active
		m_clockState = TimelineClockState.NotStarted;
		m_nIteration = 0;

		GetNaturalDuration(out var durationType, out var rDurationValue);

		// Compute the expiration time
		ComputeExpirationTime(
			rBeginTime,
			durationType,
			rDurationValue,
			m_rSpeedRatio,  // we need to use our local speed ratio
			out var expirationTime);

		// The parent time can be modified by ComputeCurrentClockState, so copy it on the stack if it's valid.
		var clampedParentParams = parentParams;

		// Determines whether the timeline has started, is running, or has finished.
		ComputeCurrentClockState(
			ref clampedParentParams,
			rBeginTime,
			expirationTime,
			out var shouldComputeProgress);

		if (IsDurationForProgressDifferent())
		{
			GetDurationForProgress(out durationType, out rDurationValue);
		}

		if (shouldComputeProgress)
		{
			// Compute local progress accounting for iteration count.
			global::System.Diagnostics.Debug.Assert(clampedParentParams.hasTime);
			ComputeLocalProgressAndTime(
				rBeginTime,
				clampedParentParams.time,
				durationType,
				rDurationValue,
				expirationTime,
				out isLocallyReversed);

			// Fire per-iteration initialization.
			if (!m_fInitialized)
			{
				InitializeIteration();
			}
		}
		else
		{
			// If we haven't yet gone into active mode, skip progress computation.
			global::System.Diagnostics.Debug.Assert(m_clockState == TimelineClockState.NotStarted);
		}

		// Update animated values and update the next tick time.
		myParams.isReversed = parentParams.isReversed ^ isLocallyReversed;

		UpdateAnimation(
			parentParams, // NOTE: the actual parent time is used for scheduling purposes, not the clamped one
			ref myParams,
			rBeginTime,
			durationType,
			rDurationValue,
			expirationTime);

		// Check external references
		if (hasExternalReferencesTracking)
		{
			CheckHasNoExternalRefs(ref pHasNoExternalReferences);
		}
	}

	internal virtual void FinalizeIteration()
	{
#if HAS_UNO
		// TODO Uno specific: CAnimation is folded into Timeline.
		if (IsAnimation())
		{
			AnimationFinalizeIteration();
		}
#endif

		m_fInitialized = false;
		m_rCurrentTime = 0.0;
	}

	internal virtual void OnAddToTimeManager()
	{
		m_fIsInTimeManager = true;

		// TODO Uno: WinUI enables the Completed event requests and pegs the managed peer here. Uno raises Completed
		// directly, and the time manager's reference keeps the timeline alive.
	}

	internal virtual void OnRemoveFromTimeManager()
	{
		m_fIsInTimeManager = false;

#if !HAS_UNO
		// This flag should only be set when being ticked in the time manager.
		m_hasIndependentAnimation = FALSE;
#endif

		// TODO Uno: WinUI disables the Completed event requests and unpegs the managed peer here.

#if HAS_UNO
		// TODO Uno specific: CAnimation is folded into Timeline.
		if (IsAnimation())
		{
			AnimationOnRemoveFromTimeManager();
		}
#endif
	}

	// Encapsulates the lifetime checks for active Timelines.
	private void CheckHasNoExternalRefs(ref bool pbHasNoExternalReferences)
	{
		// TODO Uno: WinUI removes a timeline from the time manager once nothing but the time manager references it
		// (counting references, and ignoring Completed handlers of timelines that run forever). Uno can't count
		// references, so the time manager keeps it until it stops.
		pbHasNoExternalReferences = false;
	}

	// Resolve target name and target property
	private protected void ResolveLocalTarget(Timeline? pParentTimeline)
	{
#if HAS_UNO
		// TODO Uno specific: CAnimation is folded into Timeline.
		if (IsAnimation())
		{
			AnimationResolveLocalTarget(pParentTimeline);
		}
		else
		{
			TimelineResolveLocalTarget(pParentTimeline);
		}
	}

	private void TimelineResolveLocalTarget(Timeline? pParentTimeline)
	{
#endif
		DependencyObject? targetObjectWeakRef = null;
		DependencyProperty? pTargetDependencyProperty = null;
		var strTargetName = Storyboard.GetTargetName(this);
		var strTargetProperty = Storyboard.GetTargetProperty(this);
		Timeline? pParent = null;

		// Always clean our resolved target
		ReleaseTarget();

		// if we have a manual target object, it takes precedence
		if (GetTargetObjectNoRef() is { } manualTarget)
		{
			targetObjectWeakRef = manualTarget;
		}

		// if we have a manual target DP (internal only), it takes precedence
		if (m_pManualTargetDependencyProperty != null)
		{
			pTargetDependencyProperty = m_pManualTargetDependencyProperty;
		}

		if (pParentTimeline != null)
		{
			// get targeting information from the parent chain
			pParent = pParentTimeline;
			while (pParent != null)
			{
				// TODO Uno: timelines under a dynamictimeline should use it as its proxy for resolving through templatescope.
				// DynamicTimeline is not ported.

				// attempt to get a target name
				if (string.IsNullOrEmpty(strTargetName))
				{
					strTargetName = Storyboard.GetTargetName(pParent);
				}

				// attempt to get a property path
				if (string.IsNullOrEmpty(strTargetProperty))
				{
					strTargetProperty = Storyboard.GetTargetProperty(pParent);
				}

				// attempt to get a manually set target object
				if (targetObjectWeakRef == null)
				{
					targetObjectWeakRef = pParent.GetTargetObjectNoRef();
				}

				// attempt to get a manually set target property
				if (pTargetDependencyProperty == null)
				{
					pTargetDependencyProperty = m_pManualTargetDependencyProperty;
				}

				pParent = pParent.GetTimingParent();
			}
		}

		// If we don't have a target object yet, try to resolve it using name
		if (targetObjectWeakRef == null)
		{
			// if we don't have a name specified, nothing we can do at this point
			if (string.IsNullOrEmpty(strTargetName))
			{
				return;
			}

			var pTargetObject = ResolveName(strTargetName, pParentTimeline);

			// if we have a name, but it can't be found, set error with details.
			if (pTargetObject == null)
			{
				// Don't fail fast - VSM can catch this error and handle it
				throw new InvalidOperationException($"Cannot resolve TargetName {strTargetName}.");
			}

			targetObjectWeakRef = pTargetObject;
		}

		// By this point we know we have a target object - need to resolve target property.
		var resolvedTargetObject = targetObjectWeakRef;
		if (resolvedTargetObject != null)
		{
			// if we don't yet have a target DP, need to parse the property path
			if (pTargetDependencyProperty == null)
			{
				// if we don't have a property path, nothing we can do at this point
				if (string.IsNullOrEmpty(strTargetProperty))
				{
					return;
				}

				// Now parse the property path.  Note that this may change what we consider the
				// target object so we won't take a reference unless we succeed at parsing the
				// property path.
				var succeeded = ParsePropertyPath(ref resolvedTargetObject, out pTargetDependencyProperty, strTargetProperty);

				// It is an error NOT to find the target property
				if (!succeeded || pTargetDependencyProperty == null)
				{
					throw new InvalidOperationException($"Cannot resolve TargetProperty {strTargetProperty} on specified object.");
				}
			}

			// By this point, we have the final resolved target object and the DP
			m_targetObjectWeakRef = new WeakReference<DependencyObject>(resolvedTargetObject);
			m_pTargetDependencyProperty = pTargetDependencyProperty;
		}
		else
		{
			// We have no target in the current tree - ignore
		}
	}

	internal DependencyObject? ResolveName(string strName, Timeline? pParentTimeline)
	{
		// TODO Uno: WinUI resolves through the namescope of the timeline (or of the DynamicTimeline proxy), using the
		// templated parent for template namescope members. Uno resolves from the nearest element up the parent chain.
		return ResolveNameFromParents(strName, pParentTimeline);
	}

	internal void RequestTickForPendingThemeChange()
	{
		Timeline? pTimingParent = null;
		var pTimeManager = TimeManager.Instance;

		pTimingParent = GetTimingParent();

		if (pTimingParent != null && (pTimingParent != pTimeManager.GetRootTimeline()))
		{
			m_hasPendingThemeChange = true;

			// This is not the top-level timeline, so bubble up request for new tick
			pTimingParent.RequestTickForPendingThemeChange();
		}
		else
		{
			// This is the top-level timeline.
			var pFrameScheduler = pTimeManager.GetFrameScheduler();

			// Add timeline to TimeManager if needed
			if (pTimingParent == null)
			{
				pTimeManager.AddTimeline(this);
			}

			// Schedule new tick to process Animation's new theme
			pFrameScheduler.RequestAdditionalFrame(0 /* immediate */);
			m_hasPendingThemeChange = true;
		}
	}

	internal virtual bool IsFinite()
	{
		var isFiniteAnimation = true;
		var pCurrentTimeline = this;

		while (pCurrentTimeline != null)
		{
			if ((pCurrentTimeline.m_duration.Type == DurationType.Forever) ||
				(pCurrentTimeline.m_repeatBehavior.Type == RepeatBehaviorType.Forever))
			{
				isFiniteAnimation = false;
				break;
			}

			pCurrentTimeline = pCurrentTimeline.GetTimingParent();
		}

		return isFiniteAnimation;
	}

	// This returns the effective duration of a clock.  The effective duration is basically the
	// length of the clock's active period, taking into account speed ratio, repeat, and autoreverse.
	// Null is used to represent an infinite effective duration.
	private void ComputeEffectiveDuration(
		DurationType durationType,
		float rDurationValue,
		float rAppliedSpeedRatio,
		out OptionalDouble poptEffectiveDuration)
	{
		global::System.Diagnostics.Debug.Assert(durationType != DurationType.Automatic);
		global::System.Diagnostics.Debug.Assert(rAppliedSpeedRatio > 0.0f);

		poptEffectiveDuration = default;

		var rRepeatBehavior = 1.0f;
		var rLimitingTime = double.MaxValue;

		if (m_repeatBehavior.Type == RepeatBehaviorType.Duration)
		{
			rLimitingTime = m_repeatBehavior.Duration.TotalSeconds;
		}
		else
		{
			// Repeat is either forever or automatic, so look at the count field
			rRepeatBehavior *= (float)m_repeatBehavior.Count;
		}

		// We have a zero or negative duration or repeat, set the effective duration to ZERO
		if ((durationType == DurationType.TimeSpan && rDurationValue <= 0.0f)
		  || (rRepeatBehavior <= 0.0f))
		{
			poptEffectiveDuration.m_fHasValue = true;
			poptEffectiveDuration.m_rValue = 0.0;
		}
		// We repeat forever or we last forever without any clippling RepeatBehavior
		else if ((m_repeatBehavior.Type == RepeatBehaviorType.Forever)
			|| (durationType == DurationType.Forever && m_repeatBehavior.Type == RepeatBehaviorType.Count))
		{
			poptEffectiveDuration.m_fHasValue = false;  // no value == forever
			poptEffectiveDuration.m_rValue = 0.0;
		}
		// We last forever, but we have a clipping RepeatBehavior
		else if (durationType == DurationType.Forever)
		{
			poptEffectiveDuration.m_fHasValue = true;
			if (m_repeatBehavior.Type == RepeatBehaviorType.Duration)
			{
				poptEffectiveDuration.m_rValue = rLimitingTime;
			}
			else
			{
				poptEffectiveDuration.m_rValue = rRepeatBehavior / rAppliedSpeedRatio;
			}
		}
		// We repeat for a specific number of iterations
		else
		{
			double rScalingFactor = 0;
			if (m_repeatBehavior.Type == RepeatBehaviorType.Duration)
			{
				rScalingFactor = rLimitingTime / rAppliedSpeedRatio;
			}
			else
			{
				rScalingFactor = rRepeatBehavior / rAppliedSpeedRatio;
			}

			if (m_fAutoReverse)
			{
				rScalingFactor *= 2.0;
			}

			poptEffectiveDuration.m_fHasValue = true;
			poptEffectiveDuration.m_rValue = Math.Min(rScalingFactor * rDurationValue, rLimitingTime);
		}
	}

	private protected void ComputeExpirationTime(
		double rBeginTime,
		DurationType durationType,
		float rDurationValue,
		float rAppliedSpeedRatio,
		out OptionalDouble poptExpirationTime)
	{
		global::System.Diagnostics.Debug.Assert(durationType != DurationType.Automatic);

		poptExpirationTime = default;

		// Get the effective duration and compute the expiration time
		ComputeEffectiveDuration(
			durationType,
			rDurationValue,
			rAppliedSpeedRatio,
			out var effectiveDuration);

		if (effectiveDuration.m_fHasValue)
		{
			poptExpirationTime.m_fHasValue = true;
			poptExpirationTime.m_rValue = rBeginTime + effectiveDuration.m_rValue;
		}
		else
		{
			poptExpirationTime.m_fHasValue = false; // indicates infinite expiration
		}
	}

	// Determine if we are active, filling, or off
	// pParentTime is clamped if it is inside the postfill zone.
	// We have to handle reversed parent differently because we have closed-open intervals in global time
	private void ComputeCurrentClockState(
		ref ComputeStateParams clampedParentParams,
		double rBeginTime,
		in OptionalDouble poptExpirationTime,
		out bool pShouldComputeProgress)
	{
		// Check if the parent hadn't started yet, or if we are before the begin time.
		if (!clampedParentParams.hasTime || clampedParentParams.time < rBeginTime)
		{
			m_clockState = TimelineClockState.NotStarted;
			m_rCurrentProgress = 0.0f;
			pShouldComputeProgress = false;
		}
		else
		{
			// If we have an expiration time, check fill behavior.
			if (IsExpired(poptExpirationTime, clampedParentParams.time))
			{
				// TODO Uno: WinUI keeps an expired independent animation Active until the DComp animation reports its
				// completion. Uno has no DComp animations, so an expired timeline always completes right away.

				// Clamp the effective parent time and the current time to the expiration time.
				if (m_fillBehavior == FillBehavior.HoldEnd)
				{
					m_clockState = TimelineClockState.Filling;
					m_rCurrentTime = poptExpirationTime.m_rValue;
					clampedParentParams.time = m_rCurrentTime;
				}
				else
				{
					m_clockState = TimelineClockState.Stopped;
				}
			}
			else
			{
				m_clockState = TimelineClockState.Active;
			}

			pShouldComputeProgress = true;
		}
	}

	private void ComputeLocalProgressAndTime(
		double rBeginTime,
		double rParentTime,
		DurationType durationType,
		float rDurationValue,
		in OptionalDouble poptExpirationTime,
		out bool pIsInReverseSegment)
	{
		global::System.Diagnostics.Debug.Assert(rParentTime >= rBeginTime);
		var isInReverseSegment = false;

		// We compute our current time relative to our parent time and take into account our own speed.
		// This is always relative to the parent's frame of reference.
		m_rCurrentTime = (rParentTime - rBeginTime) * m_rSpeedRatio;

		if (durationType == DurationType.TimeSpan)
		{
			// TODO Uno: WinUI holds an expired independent animation at its expiration time while it waits for the
			// DComp animation to complete. Uno has no DComp animations.

			if (rDurationValue <= 0.0f)
			{
				// There are scenarios for ZERO duration animations
				// In those cases we want to hold the END value
				m_rCurrentTime = 0.0;
				m_rCurrentProgress = 1.0f;
			}
			else
			{
				var rIterations = (float)(m_rCurrentTime / rDurationValue);
				m_nIteration = (int)MathF.Floor(rIterations);
				m_rCurrentProgress = rIterations - (float)m_nIteration;

				// If we have done more than one iteration, the boundary case indicates
				// full progress so that hold behaviors works at the end
				// Note that there are some tricky precision issues here. In particular,
				// our computation includes:
				//
				// rParentTime = rBeginTime + rDuration;    <-- drops precision to rBeginTime range
				// rCurrentTime = rParentTime - rBeginTime; <-- still in rBeginTime range
				//
				// However, rDuration hasn't had truncated precision, so we can't do an
				// exact test with m_rCurrentProgress == 0.
				//
				// Worse yet, the precision drop depends on whether or not we stay internal
				// to the FPU or whether we write to memory (and drop precision), so we can't
				// reliably drop precision here without forcing external memory writes.
				//
				// So, instead of the zero test, we do a current progress < 0.1% test for
				// this boundary case so we can fill last value.
				//
				// TODO: Think about how to avoid precision loss here more reliably.

				// We have an expiration time, so we are clipping.  Only check for the boundary
				// case
				if ((rIterations >= 1.0f && m_rCurrentProgress <= 0.001f) || (m_rCurrentProgress >= 0.999f))
				{
					if (IsExpired(poptExpirationTime, rParentTime))
					{
						// If we are near a complete cycle, and have autorepeat,
						// we must hold at the beginning and not the end ...
						if (m_fAutoReverse && (m_nIteration % 2) == 0)
						{
							m_rCurrentProgress = 0.0f;
						}
						else
						{
							m_rCurrentProgress = 1.0f;
						}
					}
				}

				// Invert the odd intervals for autoreverse
				if (m_fAutoReverse && (m_nIteration % 2) == 1)
				{
					global::System.Diagnostics.Debug.Assert(m_rCurrentProgress <= 1.0f);
					m_rCurrentProgress = 1.0f - m_rCurrentProgress;
					isInReverseSegment = true;
				}

				// Set the time we will report to our children timelines
				m_rCurrentTime = m_rCurrentProgress * rDurationValue;
			}
		}
		else // Duration is Forever
		{
			m_rCurrentProgress = 0.0f;
		}

		pIsInReverseSegment = isInReverseSegment;
	}

	private protected static bool IsExpired(in OptionalDouble poptExpirationTime, double rParentTime)
	{
		//
		// When there's a pending seek on the first tick, the start delta for the storyboard is calculated from
		// the global time and the pending seek time. The current time is then calculated from the global time
		// and the start delta. Floating point error can cause the current time to be slightly before the seek
		// time, so include a tolerance in this comparison.
		//
		return (poptExpirationTime.m_fHasValue
			&& poptExpirationTime.m_rValue <= rParentTime + s_timeTolerance);
	}

	// Compute the duration when one is not specified in the markup.
	// Animations override this method to provide their own default.
	internal virtual void GetNaturalDuration(out DurationType pDurationType, out float pDurationValue)
	{
#if HAS_UNO
		// TODO Uno specific: CAnimation is folded into Timeline.
		if (IsAnimation())
		{
			AnimationGetNaturalDuration(out pDurationType, out pDurationValue);
			return;
		}

		// TODO Uno: The unported theme animations are zero-length (see OnBegin).
		if (IsUnportedThemeTimeline())
		{
			pDurationType = DurationType.TimeSpan;
			pDurationValue = 0.0f;
			return;
		}
#endif

		// Timelines defaults infinite animation
		pDurationType = DurationType.Automatic;
		pDurationValue = 0.0f;
	}

	internal virtual void FireCompletedEvent()
	{
#if HAS_UNO
		// TODO Uno specific: CAnimation is folded into Timeline.
		if (IsAnimation())
		{
			AnimationFireCompletedEvent();
		}
		else
		{
			TimelineFireCompletedEvent();
		}
	}

	private protected void TimelineFireCompletedEvent()
	{
#endif
		global::System.Diagnostics.Debug.Assert(!IsCompletedEventFired());

		// See if we have an event handler for "Completed"
		if (_completedHandlers is { } handlers)
		{
			// Events MAY BE synchronous, so if anything RESETS this state inside the event
			//   this value will be incorrect if we set this later
			m_IsCompletedEventFired = true;

			// ... And fire it!
			// CEventManager::Raise posts the event to the dispatcher (it is not raised synchronously), so the handlers
			// run after the tick, once every timeline has applied its values.
			var handlersSnapshot = handlers.ToArray();
			global::Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(() =>
			{
				foreach (var handler in handlersSnapshot)
				{
					handler(this, null!);
				}
			}, global::Uno.UI.Dispatching.NativeDispatcherPriority.Normal);
		}
	}

	// Returns the root Timeline that is a parent of this one, which could be itself.
	internal Timeline GetRootTimingParent()
	{
		var pRoot = TimeManager.Instance.GetRootTimeline();
		var pParent = this;

		// we found the root if our parent's parent is the TimeManager or NULL
		while ((pParent.GetTimingParent() != pRoot) && (pParent.GetTimingParent() != null))
		{
			pParent = pParent.GetTimingParent()!;
		}

		return pParent;
	}

	// Returns S_OK in case this Timeline is in a state where it can be
	// safely modified.  Returns an appropriate error message otherwise.
	internal void CheckCanBeModified()
	{
		if (!CanBeModified())
		{
			throw new InvalidOperationException("Operation is not valid on an active Animation or Storyboard. Root Storyboard must be stopped first.");
		}
	}

	internal bool CanBeModified()
	{
		var pRootStoryboard = GetRootTimingParent();
		if (pRootStoryboard != null && !pRootStoryboard.IsInStoppedState())
		{
			return false;
		}

		return true;
	}

	internal void SetTargetObject(DependencyObject? pTarget)
	{
		// we can only do this if we can be modified
		CheckCanBeModified();

		m_manualTargetObjectWeakRef = pTarget is null ? null : new WeakReference<DependencyObject>(pTarget);
	}

	internal DependencyObject? GetTargetObjectNoRef()
		=> m_manualTargetObjectWeakRef is not null && m_manualTargetObjectWeakRef.TryGetTarget(out var target) ? target : null;

	internal DependencyObject? GetTargetObject() => GetTargetObjectNoRef();

	internal DependencyProperty? GetTargetProperty() => m_pManualTargetDependencyProperty;

	// Requests a tick for the time remaining until the beginning of this Timeline's active
	// period, if required.
	private protected void RequestTickToActivePeriodBegin(
		in ComputeStateParams parentParams,
		in ComputeStateParams myParams,
		double beginTime)
	{
		global::System.Diagnostics.Debug.Assert(m_clockState == TimelineClockState.NotStarted);

		// This timeline will be 'NotStarted' but have a valid parent time if it had a BeginTime.
		if (parentParams.hasTime)
		{
			global::System.Diagnostics.Debug.Assert(parentParams.time < beginTime);

			// This animation needs to schedule a tick for the time it starts when the parent time is running forwards.
			//
			// When running backwards in this state, this isn't necessary since this animation won't tick again
			// (from its perspective). If it ends up changing direction and running forwards again, it's because
			// the parent started a new iteration. The parent guarantees a tick at the beginning of each iteration
			// to notify all its children that they are running again in a new state or direction.
			//
			// Note that it isn't necessary to check whether the timeline is locally reversed, since a timeline will not
			// run backwards from Active into NotStarted again - it will loop into a forward iteration again.
			if (!parentParams.isReversed)
			{
				parentParams.pFrameSchedulerNoRef!.RequestAdditionalFrame(
					XcpCeiling((beginTime - parentParams.time) * 1000.0 / myParams.speedRatio));
			}
		}
		else
		{
			// Otherwise, it hasn't started because its parent hasn't started (which is why it has no parent time),
			// or the root timing parent is stopped and didn't want to any of its children to be active.
			// Note: Check the root storyboard for Stopped. In the case of nested storyboard, the root could be Stopped
			//       while all nested storyboards are Filling at time infinity.
		}
	}

	// Requests a tick for the time remaining until the end of this Timeline's active period,
	// if required.
	private protected void RequestTickToActivePeriodEnd(
		in ComputeStateParams parentParams,
		in ComputeStateParams myParams,
		in OptionalDouble expirationTime)
	{
		// If the parent time isn't provided, it means the parent hasn't started yet. In that situation,
		// the animation should not have started either.
		global::System.Diagnostics.Debug.Assert(m_clockState == TimelineClockState.Stopped || m_clockState == TimelineClockState.Filling);
		global::System.Diagnostics.Debug.Assert(parentParams.hasTime);

		// This animation needs to schedule a tick for the time it becomes active again, its
		// expiration time, when the parent time is running in reverse. This is analogous to ticking to the
		// beginning of its active period when running forwards (see RequestTickToActivePeriodBegin).
		//
		// When running forwards in the Stopped or Filling state, the animation will not tick again
		// (from its perspective). If it ends up changing direction and running backwards again, it's
		// because the parent started a new iteration. The parent guarantees a tick at the beginning of
		// each iteration to notify all its children that they are running again in a new state or direction.
		//
		// Note that it isn't necessary to check whether the timeline is locally reversed, since a timeline will not
		// run forwards from Active to Filling or Stopped if AutoReverse="true" - it will reverse direction instead.
		if (parentParams.isReversed)
		{
			parentParams.pFrameSchedulerNoRef!.RequestAdditionalFrame(
				XcpCeiling((parentParams.time - expirationTime.m_rValue) * 1000.0 / myParams.speedRatio));
		}
	}

	private static void RequestAdditionalFrame()
	{
		// The browser host and/or frame scheduler can be NULL during shutdown.
		TimeManager.RequestAdditionalFrame(0 /* immediate */);
	}
}
