// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ThemeGenerator.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ThemeGeneratorHelper
{
	// The C++ destructor only releases COM references.

	internal void Initialize()
	{
		m_strTranslateXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).TranslateX";
		m_strTranslateYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).TranslateY";
		m_strOpacityPropertyName = "(UIElement.TransitionTarget).Opacity";
		m_strCenterYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).CenterY";
		m_strScaleXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).ScaleX";
		m_strScaleYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).ScaleY";
		m_strClipScaleXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.ScaleX)";
		m_strClipScaleYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.ScaleY)";
		m_strClipTranslateXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.TranslateX)";
		m_strClipTranslateYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.TranslateY)";
	}

	internal void RegisterKeyFrame(
		string targetPropertyName,
		double value,
		long begintime,
		long duration,
		TimingFunctionDescription? pEasing)
	{
		DoubleKeyFrame pKeyFrame;

		long time = 0;

		ArgumentNullException.ThrowIfNull(targetPropertyName);

		if (pEasing is null)
		{
			pKeyFrame = new DiscreteDoubleKeyFrame();
		}
		else if (pEasing.IsLinear() || m_onlyGenerateSteadyState)
		{
			pKeyFrame = new LinearDoubleKeyFrame();
		}
		else
		{
			SplineDoubleKeyFrame spKeyFrame = new();
			KeySpline spKeySpline = new();
			spKeySpline.ControlPoint1 = pEasing.cp2;
			spKeySpline.ControlPoint2 = pEasing.cp3;
			spKeyFrame.KeySpline = spKeySpline;
			pKeyFrame = spKeyFrame;
		}

		// a keyframe needs to be registered inside a timeline. These are cached in the map, keyed on the property.
		if (!m_doubleAnimationsMap.TryGetValue(targetPropertyName, out var pKeyframes))
		{
			pKeyframes = new DoubleAnimationUsingKeyFrames();
			pKeyframes.IsThemeGenerated = true;
			m_doubleAnimationsMap[targetPropertyName] = pKeyframes;

			m_pTimelines.Add(pKeyframes);

			SetTimelineTarget(pKeyframes, targetPropertyName);

			// with the first keyframe to be created, we'll take its time and actually start the timeline at that point
			if (begintime > 0 && !m_onlyGenerateSteadyState)
			{
				m_begintime = begintime;    // cache for later consumption
				pKeyframes.BeginTime = TimeSpan.FromTicks(m_begintime * 10000);
			}
		}

		var spKeyframeCollection = pKeyframes.KeyFrames;
		if (m_onlyGenerateSteadyState)
		{
			// steady state animation is created by only taking the last keyframe and setting its keytime to 0.
			var keyframeCollectionSize = spKeyframeCollection.Count;
			MUX_ASSERT(keyframeCollectionSize < 2); // would expect one at the very most
			if (keyframeCollectionSize > 0)
			{
				spKeyframeCollection.Clear();
			}
		}
		spKeyframeCollection.Add(pKeyFrame);

		// correct for the begintime set on the timeline
		time = m_onlyGenerateSteadyState ? 0 : begintime + duration - m_begintime;

		// initialize with keytime from transform
		// keyframe keytime is where a value needs to be at a certain time
		pKeyFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(10000 * time));
		pKeyFrame.Value = value;
	}

	// TODO Uno: RegisterPointerKeyFrame is not ported (no PointerAnimationUsingKeyFrames, no caller in the item chrome).

	internal void Set2DTransformOriginValues(Point originPoint)
	{
		if (!m_originValuesSet)   // only support one set of origin. Consider throwing if incoming is different
		{
			SetOriginForProperty(originPoint, "(UIElement.TransitionTarget).TransformOrigin", ref m_originValuesSet);
		}
	}

	internal void SetClipOriginValues(Point originPoint)
	{
		if (!m_clipValuesSet)   // only support one set of origin. Consider throwing if incoming is different
		{
			SetOriginForProperty(originPoint, "(UIElement.TransitionTarget).ClipTransformOrigin", ref m_clipValuesSet);
		}
	}

	// TODO Uno: PreventHitTestingWhileAnimating is not ported (no caller).

	internal void SetOverrideTarget(DependencyObject? pTarget) => m_target = pTarget;

	private void SetOriginForProperty(Point originPoint, string propertyName, ref bool valueSet)
	{
		if (!valueSet)   // only support one set of origin. Consider throwing if incoming is different
		{
			valueSet = true;

			// TODO Uno: PointAnimation is not implemented on Skia; a 0-duration object animation holds the origin instead.
			ObjectAnimationUsingKeyFrames spOriginTimeline = new();
			spOriginTimeline.IsThemeGenerated = true;

			spOriginTimeline.KeyFrames.Add(new DiscreteObjectKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
				Value = originPoint,
			});

			spOriginTimeline.Duration = new Duration(TimeSpan.Zero);

			m_pTimelines.Add(spOriginTimeline);

			SetTimelineTarget(spOriginTimeline, propertyName);
		}
	}
}

partial class ThemeGenerator
{
	//------------------------------------------------------------------------
	// Translation logic. Responsibility: understanding pvl definition of TRANSLATE2D
	//------------------------------------------------------------------------
	private static void CreateTranslate2DTimelines(
		ThemeAnimationTransform pTransform,
		TimingFunctionDescription pEasing,
		ThemeGeneratorHelper pHelper)
	{
		long startTime = (long)pTransform.dwStartTime + pHelper.GetAdditionalTime();
		long duration = (long)pTransform.dwDurationTime;

		// this is a transform, so we need a doubleanimationusingkeyframes for both x and y

		// ------------ origin ------------
		// we do not support origin on translate
		if ((pTransform.eFlags & ThemeAnimationTransformFlags.HasOriginValues) != 0)
		{
			throw new ArgumentException("Origin values are not supported on a translate transform.");
		}

		// ------------ initial value -------
		if ((pTransform.eFlags & ThemeAnimationTransformFlags.HasInitialValues) != 0)
		{
			throw new ArgumentException("Initial values are not supported on a translate transform.");
		}

		// ------------ expecting user values ---
		if ((pTransform.eFlags ^ ThemeAnimationTransformFlags.TargetValuesUser) != 0)
		{
			throw new ArgumentException("A translate transform expects user target values.");
		}

		if (startTime > 0)
		{
			// lock the transition to the current value between 0 and startTime
			pHelper.RegisterKeyFrame(pHelper.GetTranslateXPropertyName(), pHelper.GetStartOffset().X, 0, 0, pEasing);
			pHelper.RegisterKeyFrame(pHelper.GetTranslateYPropertyName(), pHelper.GetStartOffset().Y, 0, 0, pEasing);

		}

		// ------------ X
		pHelper.RegisterKeyFrame(pHelper.GetTranslateXPropertyName(), pHelper.GetStartOffset().X, startTime, 0, pEasing);
		pHelper.RegisterKeyFrame(pHelper.GetTranslateXPropertyName(), pHelper.GetDestinationOffset().X, startTime, duration, pEasing);

		// ------------ Y
		pHelper.RegisterKeyFrame(pHelper.GetTranslateYPropertyName(), pHelper.GetStartOffset().Y, startTime, 0, pEasing);
		pHelper.RegisterKeyFrame(pHelper.GetTranslateYPropertyName(), pHelper.GetDestinationOffset().Y, startTime, duration, pEasing);
	}

	//------------------------------------------------------------------------
	// Scale logic. Responsibility: understanding pvl definition of SCALE2D
	//------------------------------------------------------------------------
	private static void CreateScale2DTimelines(
		ThemeAnimationTransform pTransform,
		TimingFunctionDescription pEasing,
		ThemeGeneratorHelper pHelper)
	{
		var pTransform2D = pTransform;
		long startTime = (long)pTransform.dwStartTime + pHelper.GetAdditionalTime();

		// ------------ origin ------------
		// we support origin values on a scale
		if ((pTransform.eFlags & ThemeAnimationTransformFlags.HasOriginValues) != 0)
		{
			Point point = new(pTransform2D.rOriginX, pTransform2D.rOriginY);
			pHelper.Set2DTransformOriginValues(point);
		}

		// ------------ initial value -------
		if ((pTransform.eFlags & ThemeAnimationTransformFlags.HasInitialValues) != 0)
		{
			// additionaltime means we have to lock the scale to the initial value
			if (startTime > 0)
			{
				pHelper.RegisterKeyFrame(pHelper.GetScaleXPropertyName(), (double)pTransform2D.rInitialX, 0, 0, pEasing);
				pHelper.RegisterKeyFrame(pHelper.GetScaleYPropertyName(), (double)pTransform2D.rInitialY, 0, 0, pEasing);
			}

			pHelper.RegisterKeyFrame(pHelper.GetScaleXPropertyName(), (double)pTransform2D.rInitialX, startTime, 0, pEasing);
			pHelper.RegisterKeyFrame(pHelper.GetScaleYPropertyName(), (double)pTransform2D.rInitialY, startTime, 0, pEasing);
		}

		// ------------ actual keyframe ------
		pHelper.RegisterKeyFrame(pHelper.GetScaleXPropertyName(), (double)pTransform2D.rX, startTime, (long)pTransform.dwDurationTime, pEasing);
		pHelper.RegisterKeyFrame(pHelper.GetScaleYPropertyName(), (double)pTransform2D.rY, startTime, (long)pTransform.dwDurationTime, pEasing);
	}

	//------------------------------------------------------------------------
	// Opacity logic. Responsibility: understanding pvl definition of OPACITY
	//------------------------------------------------------------------------
	private static void CreateOpacityTimelines(
		ThemeAnimationTransform pTransform,
		TimingFunctionDescription pEasing,
		ThemeGeneratorHelper pHelper)
	{
		var pOpacityTransform = pTransform;
		long startTime = (long)pTransform.dwStartTime + pHelper.GetAdditionalTime();
		long duration = (long)pTransform.dwDurationTime;

		// ------------ initial value -------
		double initialOpacity = (double)pOpacityTransform.rInitialOpacity;
		if (pHelper.GetInitialOpacity(ref initialOpacity) || (pTransform.eFlags & ThemeAnimationTransformFlags.HasInitialValues) != 0)
		{
			// WinUI quirk: the overridden initial opacity is fetched but the PVL one is registered.
			if (startTime > 0)
			{
				pHelper.RegisterKeyFrame(pHelper.GetOpacityPropertyName(), (double)pOpacityTransform.rInitialOpacity, 0, 0, pEasing);
			}
			pHelper.RegisterKeyFrame(pHelper.GetOpacityPropertyName(), (double)pOpacityTransform.rInitialOpacity, startTime, 0, pEasing);
		}

		// ------------ actual keyframe ------
		pHelper.RegisterKeyFrame(pHelper.GetOpacityPropertyName(), (double)pOpacityTransform.rOpacity, startTime, duration, pEasing);
	}

	//------------------------------------------------------------------------
	// Clip logic. Responsibility: understanding pvl definition of Clip
	//------------------------------------------------------------------------
	private static void CreateClipTimelines(
		ThemeAnimationTransform pTransform,
		TimingFunctionDescription pEasing,
		ThemeGeneratorHelper pHelper)
	{
		var pTransform2D = pTransform;
		long startTime = (long)pTransform.dwStartTime + pHelper.GetAdditionalTime();

		// ------------ origin ------------
		// we expect origin values on a clip
		if ((pTransform.eFlags & ThemeAnimationTransformFlags.HasOriginValues) != 0)
		{
			// we set the center once per animation.
			Point point = new(pTransform2D.rOriginX, pTransform2D.rOriginY);
			pHelper.SetClipOriginValues(point);
		}
		else
		{
			throw new ArgumentException("A clip transform expects origin values.");
		}

		if ((pTransform.eFlags & ThemeAnimationTransformFlags.HasInitialValues) != 0)
		{
			// split out creating X and Y versions to support PVL definitions with different X and Y clips

			if (pTransform2D.rInitialX != pTransform2D.rX)
			{
				// additionaltime means we have to lock to the initial value
				if (startTime > 0)
				{
					pHelper.RegisterKeyFrame(pHelper.GetClipScaleXPropertyName(), (double)pTransform2D.rInitialX, 0, 0, pEasing);
				}
				pHelper.RegisterKeyFrame(pHelper.GetClipScaleXPropertyName(), (double)pTransform2D.rInitialX, startTime, 0, pEasing);
				pHelper.RegisterKeyFrame(pHelper.GetClipScaleXPropertyName(), (double)pTransform2D.rX, startTime, (long)pTransform.dwDurationTime, pEasing);
			}

			if (pTransform2D.rInitialY != pTransform2D.rY)
			{
				// additionaltime means we have to lock to the initial value
				if (startTime > 0)
				{
					// WinUI quirk: the Y lock keyframe goes to the ClipScaleX timeline.
					pHelper.RegisterKeyFrame(pHelper.GetClipScaleXPropertyName(), (double)pTransform2D.rInitialY, 0, 0, pEasing);
				}
				pHelper.RegisterKeyFrame(pHelper.GetClipScaleYPropertyName(), (double)pTransform2D.rInitialY, startTime, 0, pEasing);
				pHelper.RegisterKeyFrame(pHelper.GetClipScaleYPropertyName(), (double)pTransform2D.rY, startTime, (long)pTransform.dwDurationTime, pEasing);
			}
		}
		else
		{
			throw new ArgumentException("A clip transform expects initial values.");
		}
	}

	//------------------------------------------------------------------------
	// Helper. Responsibility: understanding uxtheme api for retrieving timingfunctions.
	//------------------------------------------------------------------------
	private static void InitializeTimingCurve(
		uint dwTimingFunctionId,
		TimingFunctionDescription pTimingDescription)
	{
		// TODO Uno: PVL from uxtheme (values dumped from Windows build 29680).
		if (!s_timingFunctions.TryGetValue(dwTimingFunctionId, out var pCubicBezier))
		{
			throw new NotSupportedException($"Unknown PVL timing function {dwTimingFunctionId}.");
		}

		pTimingDescription.cp2 = pCubicBezier.cp2;
		pTimingDescription.cp3 = pCubicBezier.cp3;
	}

	//------------------------------------------------------------------------
	// Helper. Responsibility: understanding uxtheme api for retrieving pvl definition.
	//------------------------------------------------------------------------
	private static ThemeAnimationTransform[] RetrieveTransforms(int storyboardID, int targetID)
	{
		// TODO Uno: PVL from uxtheme (values dumped from Windows build 29680).
		if (!s_transforms.TryGetValue((storyboardID, targetID), out var transforms))
		{
			throw new ArgumentException($"Unknown PVL storyboard {storyboardID} / target {targetID}.");
		}

		return transforms;
	}

	//------------------------------------------------------------------------
	// Main logic. Responsibility: dissecting PVL specifics.
	//------------------------------------------------------------------------
	private static void AddTimelines(
		int storyboardID,
		int targetID,
		ThemeGeneratorHelper pHelper)
	{
		var transforms = RetrieveTransforms(storyboardID, targetID);

		for (var index = 0; index < transforms.Length; ++index)
		{
			TimingFunctionDescription pTimingFunctionDescription = new();   // perf is just not an issue here, so not reusing current description

			var pTransform = transforms[index];

			InitializeTimingCurve(pTransform.dwTimingFunctionId, pTimingFunctionDescription);

			switch (pTransform.eTransformType)
			{
				case ThemeAnimationTransformType.Translate2D:
					CreateTranslate2DTimelines(pTransform, pTimingFunctionDescription, pHelper);
					break;

				case ThemeAnimationTransformType.Scale2D:
					CreateScale2DTimelines(pTransform, pTimingFunctionDescription, pHelper);
					break;

				case ThemeAnimationTransformType.Clip:
					CreateClipTimelines(pTransform, pTimingFunctionDescription, pHelper);
					break;

				case ThemeAnimationTransformType.Opacity:
					CreateOpacityTimelines(pTransform, pTimingFunctionDescription, pHelper);
					break;

				default:
					break;
			}
		}
	}

	//------------------------------------------------------------------------
	// Adds timelines as defined by pvl
	//------------------------------------------------------------------------
	internal static void AddTimelinesForThemeAnimation(
		int storyboardID,
		int targetID,
		ThemeGeneratorHelper pHelper) =>
		AddTimelines(storyboardID, targetID, pHelper);

	//------------------------------------------------------------------------
	// Adds timelines as defined by pvl
	//------------------------------------------------------------------------
	internal static void AddTimelinesForThemeAnimation(
		int storyboardID,
		int targetID,
		string? targetName,      // can be a zero sized string
		DependencyObject? pTarget,   // can be null
		bool onlyGenerateSteadyState,
		Point startOffset,
		Point destinationOffset,
		IList<Timeline> timelineCollection)
	{
		ThemeGeneratorHelper pSupplier = new(startOffset, destinationOffset, targetName, pTarget, onlyGenerateSteadyState, timelineCollection);
		pSupplier.Initialize();

		AddTimelinesForThemeAnimation(storyboardID, targetID, pSupplier);
	}

	internal static void AddTimelinesForThemeAnimation(
		int storyboardID,
		int targetID,
		bool onlyGenerateSteadyState,
		Point startOffset,
		Point destinationOffset,
		long additionalTime,
		IList<Timeline> timelineCollection)
	{
		ThemeGeneratorHelper pSupplier = new(startOffset, destinationOffset, null, null, onlyGenerateSteadyState, timelineCollection);
		pSupplier.Initialize();
		pSupplier.SetAdditionalTime(additionalTime);

		AddTimelinesForThemeAnimation(storyboardID, targetID, pSupplier);
	}
}
