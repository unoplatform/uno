// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference animation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	// CAnimation::UpdateAnimation
	private void AnimationUpdateAnimation(
		in ComputeStateParams parentParams,
		ref ComputeStateParams myParams,
		double beginTime,
		DurationType durationType,
		float durationValue,
		in OptionalDouble expirationTime)
	{
		var pTimeManager = TimeManager.Instance;
		global::System.Diagnostics.Debug.Assert(pTimeManager is not null); // needs to be valid, since we are animating.

		var tickOnUIThread = false;

		// If we have a target object
		if (m_targetObjectWeakRef is not null)
		{
			// If we have a weak ref object but no actual object, we need to clear ourselves from
			// the hash table iff we are the active animation on the property.  This helps make
			// sure that animations acting on deleted objects get cleaned up.
			if (GetResolvedTargetObject() is null)
			{
				// TODO Uno: The hash table entries are weak on their target, there is nothing to remove.

				// Release the target object if we succeed in removing ourselves from the tree
				ReleaseTarget();
			}
			// If the target is valid and this animation has control of the target, update the animation.
			// If this animation does not have control of the target but is handing off to another animation,
			// update the animation as well. It will not update the target but will pass the handoff value
			// to the other animation.
			else if (m_hasControlOfTarget)
			{
				//
				// Determine whether this animation is independent, and notify target UIElements if so.
				// This must be done every frame, because whether an animation is dependent or not can change
				// dynamically as its target is attached or detached from different contexts (e.g. Brush.Transform
				// vs. UIElement.RenderTransform) or if a target element moves into or out of HW cache.
				//
				// PC doesn't tick all animations.
				// - Don't update or fire events for dependent continuous animations on the UI thread.
				// - All discrete animations and only independent continuous animations can tick on the UI thread.
				//
				// The render thread can tick all its animations since they're all independent.
				ShouldTickOnUIThread(
					myParams.isPaused,
					out tickOnUIThread,
					out var isIndependentAnimation);

				if (tickOnUIThread)      // tick only certain animations on the UI thread for Jupiter.
				{
					// TODO: TICK: Consider preventing UI thread ticks for independent animations in most scenarios, since they're no-ops.
					// TODO: TICK: This should be possible, but state would need to be updated when recovering from idle (e.g. hit-testing).
					switch (m_clockState)
					{
						case TimelineClockState.NotStarted:
							if (parentParams.pFrameSchedulerNoRef != null)
							{
								// Request a tick to the active period when running forwards.
								RequestTickToActivePeriodBegin(parentParams, myParams, beginTime);
							}
							break;

						case TimelineClockState.Stopped:
							// This animation does not need to tick again once stopped.
							if (m_fInitialized)
							{
								if (!m_IsCompletedEventFired)
								{
									FireCompletedEvent();
								}
							}

							FinalizeIteration();

							if (parentParams.pFrameSchedulerNoRef != null)
							{
								// Request a tick to the active period when running in reverse.
								RequestTickToActivePeriodEnd(parentParams, myParams, expirationTime);
							}
							break;

						case TimelineClockState.Active:
						{
							if (m_fUsesKeyFrames)
							{
								UpdateAnimationUsingKeyFrames(isIndependentAnimation);
								if (CanRequestTicksWhileActive() && !myParams.isPaused)
								{
									RequestNextTickForKeyFrames(myParams, durationType, durationValue);
								}
							}
							else
							{
								if (CanRequestTicksWhileActive() && parentParams.pFrameSchedulerNoRef != null && !myParams.isPaused)
								{
									// For animations w/o key frames, always request to tick again as soon as possible.
									parentParams.pFrameSchedulerNoRef.RequestAdditionalFrame(0);
								}

								var rEasedProgress = EasingFunctionBase.EaseValue(m_pEasingFunction, m_rCurrentProgress);
								InterpolateCurrentValue(rEasedProgress);
								PostInterpolateValues();
								var currentValue = ValueAssign(AssignmentOperand.CurrentValue);
								DoAnimationValueOperation(
									currentValue,
									AnimationValueOperation.AnimationValueSet,
									isIndependentAnimation);
							}
							break;
						}

						case TimelineClockState.Filling:
						{
							// This animation does not need to tick again once filling.
							if (m_fInitialized)
							{
								if (!m_IsCompletedEventFired)
								{
									FireCompletedEvent();
								}

								// Update the filling frame
								if (m_fUsesKeyFrames)
								{
									UpdateAnimationUsingKeyFrames(isIndependentAnimation);
									// No need to schedule the next frame - this animation is filling so there isn't a next frame.
								}
								else
								{
									var rEasedProgress = EasingFunctionBase.EaseValue(m_pEasingFunction, m_rCurrentProgress);
									InterpolateCurrentValue(rEasedProgress);
									PostInterpolateValues();
									var currentValue = ValueAssign(AssignmentOperand.CurrentValue);
									DoAnimationValueOperation(
										currentValue,
										AnimationValueOperation.AnimationValueSet,
										isIndependentAnimation);
								}
							}

							if (parentParams.pFrameSchedulerNoRef != null)
							{
								// Request a tick to the active period when running in reverse.
								RequestTickToActivePeriodEnd(parentParams, myParams, expirationTime);
							}
							break;
						}

						default:
							global::System.Diagnostics.Debug.Fail("Unsupported state");
							break;
					}
				}
			}
		}

#if !HAS_UNO
		// TODO Uno: Releases the WUC animation of an animation that turned out to be dependent.
#endif
	}

	private void UpdateAnimationUsingKeyFrames(bool isIndependentAnimation)
	{
		IKeyFrame? pKeyFrame = null;
		var frameCount = KeyFrames_Core is { } keyFrames ? CountKeyFrames(keyFrames) : 0;
		var nCurrentSegment = 0;
		var rCumulativePercentSpan = 0f;
		var rSegmentPercentSpan = 0f;
		var rSegmentProgress = 0f;

		if (frameCount == 0)
		{
			return;
		}

		var sortedKeyFrames = GetKeyFrameCollection().GetSortedCollection();
		var sortedFrameCount = sortedKeyFrames.Count;
		global::System.Diagnostics.Debug.Assert(sortedFrameCount == frameCount);

		// Set the end points of the interpolation to the base value
		ValueAssign(AssignmentOperand.From, AssignmentOperand.BaseValue);
		ValueAssign(AssignmentOperand.To, AssignmentOperand.BaseValue);

		// Set the From Value to either the first keyframe or the base value
		pKeyFrame = sortedKeyFrames[0];
		if (pKeyFrame.Percent > 0)
		{
			// If the first time is non-zero, we interpolate from the base value
			rSegmentPercentSpan = pKeyFrame.Percent;
		}
		else
		{
			// If the first keyframe is at time=0, we interpolate from the first keyfame
			ValueAssign(AssignmentOperand.From, pKeyFrame.GetValue());
		}

		// Set the To Value to be the first keyframe
		ValueAssign(AssignmentOperand.To, pKeyFrame.GetValue());

		// Per-KeyFrame Segment value computation: check that we are interpolating between the correct
		//      keyframe values.
		while (nCurrentSegment < frameCount
			&& sortedKeyFrames[nCurrentSegment].Percent <= m_rCurrentProgress)
		{
			// Move to the next segment
			nCurrentSegment++;
			ValueAssign(AssignmentOperand.From, AssignmentOperand.To);
			rCumulativePercentSpan += rSegmentPercentSpan;

			// Check to see if we are at the end
			if (nCurrentSegment < frameCount)
			{
				pKeyFrame = sortedKeyFrames[nCurrentSegment];
				ValueAssign(AssignmentOperand.To, pKeyFrame.GetValue());
				rSegmentPercentSpan = pKeyFrame.Percent - rCumulativePercentSpan;
			}
			else
			{
				// We need to hold the last value for the time after the last segment
				pKeyFrame = sortedKeyFrames[frameCount - 1];
				ValueAssign(AssignmentOperand.To, pKeyFrame.GetValue());
				if (rCumulativePercentSpan < 1.0f)
				{
					rSegmentPercentSpan = 1.0f - rCumulativePercentSpan;
				}
				else
				{
					// We may be at the case that this is the last segment at the end time,
					//   so the effective duration of the interval is zero.  Override this
					//   to prevent a divide by zero later.
					rSegmentPercentSpan = 1.0f;
				}
			}
		}

		// Progress computation: scale the overall progress to this particular keyframe segment
		//      and apply any time-compresion due to keysplines on a per keyframe basis.

		// Compute the linear progress of this segment
		rSegmentProgress = (m_rCurrentProgress - rCumulativePercentSpan) / rSegmentPercentSpan;

		// Now interpolate according to the current keyframe type
		if (nCurrentSegment < frameCount)
		{
			rSegmentProgress = sortedKeyFrames[nCurrentSegment].GetEffectiveProgress(rSegmentProgress);
		}
		else
		{
			// We are dealing with the last segment or single segment, hold that last value
			rSegmentProgress = sortedKeyFrames[frameCount - 1].GetEffectiveProgress(rSegmentProgress);
		}

		InterpolateCurrentValue(rSegmentProgress);
		PostInterpolateValues();

		if (m_targetObjectWeakRef is not null && GetResolvedTargetObject() is not null)
		{
			var currentValue = ValueAssign(AssignmentOperand.CurrentValue);
			DoAnimationValueOperation(
				currentValue,
				AnimationValueOperation.AnimationValueSet,
				isIndependentAnimation);
		}
	}

	// Schedule frames as needed, with special care to tick infrequently for discrete key frames (i.e. only
	// on the transitions between frames).
	private void RequestNextTickForKeyFrames(
		in ComputeStateParams myParams,
		DurationType durationType,
		float durationValue)
	{
		if (myParams.pFrameSchedulerNoRef != null && KeyFrames_Core != null)
		{
			// Key frame animations always resolve their duration to a timespan.
			global::System.Diagnostics.Debug.Assert(durationType == DurationType.TimeSpan);

			var sortedKeyFrames = GetKeyFrameCollection().GetSortedCollection();
			var frameCount = sortedKeyFrames.Count;

			var nCurrentSegment = 0;
			while (nCurrentSegment < frameCount && sortedKeyFrames[nCurrentSegment].Percent <= m_rCurrentProgress)
			{
				nCurrentSegment++;
			}

			// If current progress is within a continuously-interpolated key-frame segment, request an immediate tick.
			if (nCurrentSegment < frameCount && !sortedKeyFrames[nCurrentSegment].IsDiscrete)
			{
				// For continuously-interpolated key frames, always request to tick again as soon as possible.
				myParams.pFrameSchedulerNoRef.RequestAdditionalFrame(0 /*immediate*/);
			}
			else
			{
				// Otherwise, we're in a case where we can schedule a future tick, and potentially go idle
				// until the animated value will change again.

				// The next key-time is either the one we're interpolating to or from, depending on the direction
				// time is progressing.
				var currentSegment = nCurrentSegment;
				var targetSegment = myParams.isReversed ? currentSegment - 1 : currentSegment;
				double targetTime;

				if (targetSegment < 0)
				{
					// We're in the first segment running backwards. Target the beginning of the animation.
					global::System.Diagnostics.Debug.Assert(myParams.isReversed);
					targetTime = 0.0;
				}
				else if (targetSegment >= frameCount)
				{
					// We're in the final segment running forwards, beyond the final key-time. The animated value will
					// hold from this point until the Duration has elapsed, so target the end of the animation.
					global::System.Diagnostics.Debug.Assert(!myParams.isReversed);
					targetTime = durationValue;
				}
				else
				{
					// The target is one of the key-frame's key-times.
					global::System.Diagnostics.Debug.Assert(targetSegment >= 0 && targetSegment < frameCount);
					var keyTimeProgress = sortedKeyFrames[targetSegment].Percent;

					// Convert from progress percentage back in millisecond interval.
					targetTime = keyTimeProgress * durationValue;
				}

				// Calculate the elapsed time from now until the target is reached.
				double tickIntervalInSeconds;
				if (myParams.isReversed)
				{
					tickIntervalInSeconds = m_rCurrentTime - targetTime;
				}
				else
				{
					tickIntervalInSeconds = targetTime - m_rCurrentTime;
				}

				myParams.pFrameSchedulerNoRef.RequestAdditionalFrame(
					XcpCeiling(tickIntervalInSeconds * 1000.0 / myParams.speedRatio));
			}
		}
	}

	// Perform begin time initializations
	// CAnimation::OnBegin
	private void AnimationOnBegin()
	{
		DependencyObject? previouslyAnimatedDOWeakRef = null;
		DependencyProperty? previouslyAnimatedPropertyIndex = null;
		Timeline? pOtherAnimation = null;

		// we are in the middle of an animation - this has to be valid
		var pTimeManager = TimeManager.Instance;

		try
		{
			// Before we go on to a new target, remove our old one
			if (m_targetObjectWeakRef is not null && m_pTargetDependencyProperty != null)
			{
				// See if we are still animating this property
				var previousTarget = GetResolvedTargetObject();
				pOtherAnimation = pTimeManager.GetAnimationOnProperty(previousTarget, m_pTargetDependencyProperty);

				if (pOtherAnimation == this)
				{
					// save previous DO/DP if we were indeed animating it
					previouslyAnimatedDOWeakRef = previousTarget;
					previouslyAnimatedPropertyIndex = m_pTargetDependencyProperty;

					// Remove ourselves from the list
					pTimeManager.ClearAnimationOnProperty(previousTarget!, m_pTargetDependencyProperty);
				}

				pOtherAnimation = null;
			}

			// We always want to resolve target, even in the case of manually set targets.
			// This may update the target object and target dependency property returned by
			// GetTargetObjectWeakRef and GetTargetDependencyPropertyIndex, respectively.
			ResolveLocalTarget(GetTimingParent());

			// TODO Uno: Validate strictness of the property being animated (ValidateStrictnessOnProperty).

			var target = GetResolvedTargetObject()!;

			// We have a valid target, make sure we get the correct base value and
			// do proper handoff behavior.

			// See if anything else is animating this property
			pOtherAnimation = pTimeManager.GetAnimationOnProperty(target, m_pTargetDependencyProperty);

			// Set marks and get the ultimate base value
			// Need to get the NonAnimatedBaseValue before resolving default To,From,By
			if (pOtherAnimation != null)
			{
				// It is not expected that the other animation is this one, since the first thing
				// we do in this function is remove ourselves from the property.
				global::System.Diagnostics.Debug.Assert(pOtherAnimation != this);

				// Prevent animation composition in the same timing tree.
				if (pOtherAnimation.GetRootTimingParent() == GetRootTimingParent())
				{
					// Remove this animation from the list
					pTimeManager.ClearAnimationOnProperty(target, m_pTargetDependencyProperty!);

					// Trigger an error
					throw new InvalidOperationException("Multiple animations in the same containing Storyboard cannot target the same property on a single element.");
				}
			}

			//
			// Read the base value from the animation target only, even if there's a pending handoff. Handoffs are only processed
			// when ticking animations. OnBegin is called outside the tick, so the handoff value hasn't been written by the handoff
			// source yet. It's possible that this animation had OnBegin called on it twice in the same frame. The first call could
			// set up a handoff and mark this animation as pending, and the second call must not process the handoff.
			//
			ReadBaseValuesFromTargetOrHandoff(
				pOtherAnimation,
				previouslyAnimatedDOWeakRef,
				previouslyAnimatedPropertyIndex);

			// Gather the real duration
			GetDurationForProgress(out var durationType, out var rNaturalDuration);

			m_isZeroDuration = (rNaturalDuration == 0)
				|| IsZeroLengthRepeatBehavior(m_repeatBehavior);

			{
				var parentTimeline = GetTimingParent();

				while (parentTimeline != null)
				{
					if (IsZeroLength(parentTimeline.m_duration)
						|| IsZeroLengthRepeatBehavior(parentTimeline.m_repeatBehavior)
						)
					{
						m_isZeroDuration = true;
						break;
					}

					parentTimeline = parentTimeline.GetTimingParent();
				}
			}

			// Calculate interpolation factors for key-frames, if any.
			if (m_fUsesKeyFrames)
			{
				// Make sure our keyframes are initialized to the expected state
				GetKeyFrameCollection().InitializeKeyFrames(rNaturalDuration);
			}

			//
			// Always register the new animation on the target object+property, even if there's a queued deferred handoff.
			// The next time that something looks up the animation in the target, it will find this one. This will be used
			// to cancel the pending continuous handoff to this animation.
			//
			RegisterAnimationOnTarget();

			//
			// Take control of the target, regardless of whether there's a deferred handoff. We want synchronous changes in
			// this new animation to affect the target.
			//
			TakeControlOfTarget(pOtherAnimation);

#if !HAS_UNO
			if (pDCompTreeHostNoRef->HasDCompDevice())
			{
				AttachDCompAnimationInstancesToTarget();
			}
#endif

			// We need to reset this flag every time we call Begin on the parent Storyboard
			ResetCompletedEventFired();
		}
		catch
		{
			// release target weak ref object if begin didn't succeed
			ReleaseTarget();
			throw;
		}

		// DurationVOHelper::IsZeroLength
		static bool IsZeroLength(Duration value)
			=> value.Type == DurationType.TimeSpan && value.TimeSpan.TotalSeconds == 0.0;

		// RepeatBehaviorVOHelper::IsZeroLength
		static bool IsZeroLengthRepeatBehavior(RepeatBehavior value)
			=> (value.Type == RepeatBehaviorType.Count && (float)value.Count == 0.0f) ||
				(value.Type == RepeatBehaviorType.Duration && value.Duration.TotalSeconds == 0.0);
	}

	private void ReadBaseValuesFromTargetOrHandoff(
		Timeline? pPrecedingAnimation,
		DependencyObject? pPreviouslyAnimatedDOWeakRef,
		DependencyProperty? pPreviouslyAnimatedPropertyIndex)
	{
		// Get the current value of the object+property as the base value.
		GetAnimationBaseValue();

		var target = GetResolvedTargetObject()!;

		// The non-animated base value is what will be restored as the property value when the animation ends.

		// If there is a preceding animation handing off to this one, the preceding animation stored the correct base
		// value. Copy it.
		if (pPrecedingAnimation != null)
		{
			var precedingAnimationNonAnimatedBaseValue = pPrecedingAnimation.ValueAssign(AssignmentOperand.NonAnimatedBaseValue);

			ValueAssign(
				AssignmentOperand.NonAnimatedBaseValue,
				precedingAnimationNonAnimatedBaseValue);

			// The m_hasHandoff flag marks a DComp animation as having a handoff from some previous animation, and the DWM will
			// stitch together the starting value to make a smooth transition. However, not every Xaml animation creates a DComp
			// animation. Animations that have zero duration complete instantly and don't create DComp animations, so even though
			// in Xaml this animation might be picking up from some previous animation B, in DComp there may not be a B' to pick
			// up from, in which case we don't want to set the m_hasHandoff flag to avoid picking up from the wrong animation.
			// So we verify that the previous animation in fact has a duration. If the duration was zero, then the value update
			// happened entirely in Xaml, which means we already have the up-to-date value and there's no need to stitch things
			// together in the DWM.
			m_hasHandoff = !pPrecedingAnimation.IsFilling() && !pPrecedingAnimation.IsZeroDuration();
		}
		// If the animation was not running previously, or it was running but upon restarting has a different target
		// object+property, the current value is the ultimate base value. This value is safe to use as the base value
		// because it is not animated - if it was, there would have been a preceding animation.
		else if (pPreviouslyAnimatedDOWeakRef == null
				 || pPreviouslyAnimatedDOWeakRef != target
				 || pPreviouslyAnimatedPropertyIndex == null
				 || pPreviouslyAnimatedPropertyIndex != m_pTargetDependencyProperty)
		{
			// There is one exception to the statement above. If there is no preceding animation,
			// it is still possible for the property to be animated. This is the case if the property
			// is being modified by a Setter in a VisualState. In that particular case, using the
			// base value is not safe, and instead, we should use the non-animated base value of
			// the property as the non-animated base value of the animation. For any other cases,
			// using the base value of the animation as the non-animated base value is OK.
			if (IsAnimatedProperty(target, m_pTargetDependencyProperty!))
			{
				var value = GetAnimationBaseValue(target, m_pTargetDependencyProperty!);
				ValueAssign(
					AssignmentOperand.NonAnimatedBaseValue,
					value);
			}
			else
			{
				ValueAssign(
					AssignmentOperand.NonAnimatedBaseValue,
					AssignmentOperand.BaseValue);
			}

			m_hasHandoff = false;
		}
		else
		{
			// If the animation began again but is targeting the same property and object, it is handing
			// off to itself. The non-animated base value remains the same as before this animation ever started
			// on that object+property.

#if !HAS_UNO
			// TODO Uno: The self-handoff detection checks the DComp animation instance set on the target.
#endif
		}

		// Initialize value stores for interpolation. Key-frames don't expose From/To/By, so this is unnecessary for them.
		if (!m_fUsesKeyFrames)
		{
			if (IsFromDefault)
			{
				ValueAssign(AssignmentOperand.From, AssignmentOperand.BaseValue);
			}
			else
			{
				// If there's an explicit From value, then there is no handoff. This animation starts from that explicit value.
				m_hasHandoff = false;
			}

			if (IsToDefault)
			{
				if (IsByDefault)
				{
					// If this animation has a default To and no By, go back to non-animated value.
					ValueAssign(AssignmentOperand.To, AssignmentOperand.NonAnimatedBaseValue);
					m_isToAnimation = true; // Count this as an explicit To - handoff won't affect the final value.
				}
				else
				{
					ComputeToUsingFromAndBy();
					m_isToAnimation = false; // This is a By animation - handoff will affect the final value too.
				}
			}
			else
			{
				m_isToAnimation = true;
			}
		}
	}

	private void RegisterAnimationOnTarget()
	{
		var pTimeManager = TimeManager.Instance;

		// Set this animation as the current one on the object+property.
		pTimeManager.SetAnimationOnProperty(
			GetResolvedTargetObject()!,
			m_pTargetDependencyProperty!,
			this);
	}

	private void TakeControlOfTarget(Timeline? pPreviousAnimation)
	{
		if (pPreviousAnimation != null)
		{
			global::System.Diagnostics.Debug.Assert(pPreviousAnimation.m_hasControlOfTarget);
			pPreviousAnimation.ReleaseControlOfTarget();
		}

		m_hasControlOfTarget = true;
	}

	private void ReleaseControlOfTarget()
	{
#if !HAS_UNO
		DetachDCompAnimationInstancesFromTarget();
#endif
		m_hasControlOfTarget = false;
	}

	// CAnimation::FinalizeIteration
	private void AnimationFinalizeIteration()
	{
		if (m_clockState != TimelineClockState.Stopped)
		{
			m_clockState = TimelineClockState.Stopped;
		}

		if (m_targetObjectWeakRef is not null && m_pTargetDependencyProperty != null)
		{
			// Get the time manager
			// we are in the middle of an animation - this has to be valid
			var pTimeManager = TimeManager.Instance;
			var target = GetResolvedTargetObject();

			// See if we are animating this property
			var pOtherAnimation = pTimeManager.GetAnimationOnProperty(target, m_pTargetDependencyProperty);

			if (pOtherAnimation == this)
			{
				// Remove ourselves from the list
				pTimeManager.ClearAnimationOnProperty(target!, m_pTargetDependencyProperty);
			}

			// only set value if initialized
			if (m_fInitialized && target is not null)
			{
				// AnimationValueClear resets to base value if a HoldEnd value is not
				// provided.
				DoAnimationValueOperation(
					null, // pValue (No HoldEnd value)
					AnimationValueOperation.AnimationValueClear,
					false /*isValueChangeIndependent*/);
			}
		}

		// Once the animation stops, mark it as no longer having control over the target. Otherwise, when this animation
		// starts again, it will detach animation instances from its old target via OnBegin -> ResolveLocalTarget ->
		// ReleaseTarget. Those animation instance may now belong to some other Xaml animation altogether.
		ReleaseControlOfTarget();

		// The CTimeline part runs from the calling Timeline.FinalizeIteration.
	}

	// Resolve target name and target property
	// CAnimation::ResolveLocalTarget
	private void AnimationResolveLocalTarget(Timeline? pParentTimeline)
	{
		try
		{
			TimelineResolveLocalTarget(pParentTimeline);

			// If we haven't resolved these yet, then we should report an error here
			if (m_targetObjectWeakRef is null || m_pTargetDependencyProperty == null)
			{
				throw new InvalidOperationException("Cannot resolve TargetProperty on specified object.");
			}

			// Now we verify that our target accepts our type
			// (validation for custom properties happens during SetValue)
			if (!IsCustomProperty(m_pTargetDependencyProperty))
			{
				var bIsIncompatibleType = false;

				if (AnimatedPropertyType is { } animatedPropertyType)
				{
					if (m_pTargetDependencyProperty.Type != animatedPropertyType)
					{
						bIsIncompatibleType = true;
					}
				}
				else
				{
					// No restrictions here (ObjectAnimationUsingKeyFrames)
				}

				// if type is incompatible, create error and parameters and return error HR
				if (bIsIncompatibleType)
				{
					throw new InvalidOperationException(
						$"{GetType().Name} cannot be used to animate property {m_pTargetDependencyProperty.Name} due to incompatible type.");
				}
			}
		}
		catch
		{
			// release target weak ref object if resolution didn't succeed
			ReleaseTarget();
			throw;
		}
	}

#if !HAS_UNO
	// TODO Uno: CAnimation::SetValue / GetValue: setting From/To/By on a key-frame animation and the null handling of
	// From/To/By are covered by the separate, nullable properties of the Uno animations.
#endif

	// Update animated value to new theme
	// CAnimation::NotifyThemeChangedCore
	private void AnimationNotifyThemeChangedCore()
	{
		Timeline? pTimingParent = null;

		// Change animation's properties to new theme
		// (the theme walk updates the resources of the animation and its key frames in Uno)

		// Request a new tick to process theme change.
		if (m_fInitialized
			&& m_targetObjectWeakRef is not null
			&& GetResolvedTargetObject() is not null)
		{
			pTimingParent = GetTimingParent();
			if (pTimingParent != null)
			{
				pTimingParent.RequestTickForPendingThemeChange();
			}
		}
	}

	// Set/Clear animated value to the DP of a DO.  But if this is a custom
	// property, we need to set it asynchronously to avoid re-entrancy.
	private void DoAnimationValueOperation(
		object? value,
		AnimationValueOperation operation,
		bool isValueChangeIndependent)
	{
		var pDO = GetResolvedTargetObject();

		if (m_hasControlOfTarget && pDO is not null)
		{
			// TODO Uno: WinUI queues the operation on custom (app-defined) properties as a deferred animation operation,
			// so that app code isn't called out into from the tick. Uno sets every property directly.

			if (operation == AnimationValueOperation.AnimationValueSet)
			{
				SetAnimatedValue(pDO, m_pTargetDependencyProperty!, value);
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(!isValueChangeIndependent);
				global::System.Diagnostics.Debug.Assert(operation == AnimationValueOperation.AnimationValueClear);

				ClearAnimatedValue(pDO, m_pTargetDependencyProperty!);
			}
		}
	}

	private KeyFrameCollection GetKeyFrameCollection() => m_pKeyFrames ??= new KeyFrameCollection(() => KeyFrames_Core);

	// Returns TRUE if this animation could not be independently animated but is allowed
	// to animate by default anyway.
	private bool IsAllowedDependentAnimation()
	{
		// Object key frame animations could never interpolate smoothly and (typically) don't
		// target properties that can be independently animated.  Since they're mostly useful
		// for UI logic changes, allowing them to run on the UI thread by default is okay.  The
		// same is true for zero-duration animations, which are often used as 'setters' in XAML.
		// A zero-duration animation could never be independently animated since it would be
		// complete on the UI thread frame that picked it up.  As such, don't bother setting up
		// the data structures to allow it to independently-animate even if the target property
		// were on the accepted list.
		// Duration defaults to 1-second if it isn't set - only capture zero-duration animations here.
		return (m_duration.Type == DurationType.TimeSpan && m_duration.TimeSpan.TotalSeconds == 0.0)
			|| (m_fUsesKeyFrames && this is ObjectAnimationUsingKeyFrames);
	}

	// Check the element that uses the specified target object. If the
	// element is under a bitmap cache, then the animation targeting the
	// target object can't be independent.
	internal void CheckElementUsingAnimatedTarget(
		DependencyObject pTargetObject,
		List<DependencyObject>? pIATargets,
		ref bool pIsIndependentAnimation)
	{
		var isIndependentAnimation = pIsIndependentAnimation;

		var parentItems = RentParentItems(pTargetObject);
		try
		{
			foreach (var pParent in parentItems)
			{
				isIndependentAnimation = FindIndependentAnimationTargetsRecursive(
					pParent,
					pTargetObject,
					pIATargets);

				// If we encounter any dependent consumer of this animation, treat the entire animation as dependent. The
				// exception is if we're targeting a TransitionTarget - see block below.
				if (!isIndependentAnimation)
				{
					break;
				}
			}
		}
		finally
		{
			ReturnParentItems(parentItems);
		}

		// TODO Uno: TransitionTarget is not ported, so the TransitionTarget exception does not apply.

		pIsIndependentAnimation = isIndependentAnimation;
	}

	// Find the element that is a valid target for primitive composition
	private bool FindIndependentAnimationTargets(
		DependencyObject pTargetObject,
		DependencyObject? pSender,
		List<DependencyObject>? pIATargets = null)
	{
		var isIndependentAnimation = FindIndependentAnimationTargetsRecursive(
			pTargetObject,
			pSender,
			pIATargets);

		if (pIATargets != null)
		{
			// After finding all potential targets, apply one additional check.
			// Some consumers (like a ResourceDictionary) are allowed as long as all other consumers
			// are independent, but this animation should only be independent if there are other consumers.
			isIndependentAnimation = isIndependentAnimation && pIATargets.Count > 0;

			// Also, ensure the direct target object itself is in the list of IATargets. All UIElement targets were already
			// added by the search above, but if the target was any other type it should be added here as well.
			if (isIndependentAnimation && pTargetObject is not UIElement)
			{
				// The animation types are handled at the UIElement level, not the DO level.
				// Target DOs of other types just get a generic notification that it is being independently animated.
				pIATargets.Add(pTargetObject);
			}
		}

		return isIndependentAnimation;
	}

	private protected virtual bool FindIndependentAnimationTargetsRecursive(
		DependencyObject pTargetObject,
		DependencyObject? pSender,
		List<DependencyObject>? pIATargets)
	{
		// Default to dependent.
		return false;
	}

	// Determines if an animation can run independently or should be ticked on the UIThread.
	// Notify target elements if they are being independently-animated this frame.
	private void ShouldTickOnUIThread(
		bool isStoryboardPaused,
		out bool pTickOnUIThread,
		out bool pIsIndependentAnimation)
	{
		var tickOnUIThread = false;
		var isIndependentAnimation = false;

		// TODO: HWPC: The logic used to attach to targets verifies the animation can be independent, but so does CloneForComposition
		if (GetResolvedTargetObject() is { } target  // the target still exists...
			&& m_pTargetDependencyProperty != null)  // we have a target property...
		{
			// If the animation is not continuous, it's okay to treat it as independent since there's no 'smoothness'
			// sacrificed by running it on the UI thread.  These animations will not run on the render thread, so
			// skip the walk up looking for IA targets.
			if (IsAllowedDependentAnimation())
			{
				tickOnUIThread = true;
			}
			else
			{
				// For continuous animations, only those that qualify as independent will be ticked on the UI thread.
#if HAS_UNO
				// TODO Uno: The targets aren't handed to a compositor, they only feed the consumer check: the list is reused across ticks.
				var iaTargets = t_iaTargets ??= new List<DependencyObject>();
#else
				var iaTargets = new List<DependencyObject>();
#endif
				isIndependentAnimation = FindIndependentAnimationTargets(
					target,
					null /* sender, for recursive calls */,
					iaTargets);
#if HAS_UNO
				iaTargets.Clear();
#endif

				if (isIndependentAnimation)
				{
					// Independent animations should tick on the UI thread.  Dependent animations should not.
					tickOnUIThread = true;

					// TODO Uno: The composition-conversion and Filling/Paused independence rules only decide how WinUI
					// hands the animation to the compositor. Uno ticks every animation on the UI thread.
					if (!isStoryboardPaused && !GetRootTimingParent().IsInActiveState())
					{
						isIndependentAnimation = false;
					}
				}
			}

			// If the animation would normally be treated as dependent, it can still be ticked on the UI thread
			// if dependent animations are allowed and this animation was specifically opt-ed in.
			if (!tickOnUIThread && s_allowDependentAnimations && m_enableDependentAnimation)
			{
				tickOnUIThread = true;
			}

			// If the animation doesn't qualify as independent, report it on the first tick that's ignored.
			//
			// The animation being judged dependent or not can change based on the state of the tree from
			// frame to frame while the animation is running. We keep a state bit to prevent warning output
			// every tick, and to also catch cases where the animation changes from independent to dependent.
			if (tickOnUIThread)
			{
				m_fFiredDependentAnimationWarning = false;
			}
			else if (!m_fFiredDependentAnimationWarning)
			{
				if (target.GetParent() == null)
				{
					// We regularly see warnings for Transform and Projection objects that aren't currently parented.
					// Skip warnings if the target isn't parented, which usually means the animation was started and
					// then target was removed from the tree before the first tick.
				}
				else if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn(
						$"WARNING: Animation of \"{m_pTargetDependencyProperty.Name}\" on \"{target.GetType().Name}\" is not independent and will be skipped");
				}

				m_fFiredDependentAnimationWarning = true;
			}
		}

		pTickOnUIThread = tickOnUIThread;
		pIsIndependentAnimation = isIndependentAnimation;
	}

	internal void GetAnimationTargets(out DependencyObject? ppTargetRef, out DependencyProperty? ppProperty)
	{
		if (m_pTargetDependencyProperty == null)
		{
			// we always expect these to be in synch
			global::System.Diagnostics.Debug.Assert(m_targetObjectWeakRef is null);

			// This goes up the Timeline tree to resolve any targets.
			ResolveLocalTarget(GetTimingParent());
		}

		ppTargetRef = GetResolvedTargetObject();
		ppProperty = m_pTargetDependencyProperty;
	}

	// Fire Complete event for the animation when it has a target object
	// and it is ticking on the UI thread.
	// CAnimation::FireCompletedEvent
	private void AnimationFireCompletedEvent()
	{
		global::System.Diagnostics.Debug.Assert(!IsCompletedEventFired());

		ShouldTickOnUIThread(
			false /* isStoryboardPaused - used to determine isIndependentAnimation */,
			out var tickOnUIThread,
			out _);

		if (m_targetObjectWeakRef is not null && tickOnUIThread)
		{
			TimelineFireCompletedEvent();
		}
	}
}
