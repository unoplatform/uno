// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\ThemeGenerator.cpp, commit 978ab6363

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

//------------------------------------------------------------------------
// helper class. Responsibility: dealing with Storyboard specifics.
//
// Keyframes have to be grouped into keyframecollections
// This class takes care of carrying that state and is a 1-1 mapping to one
// call to one of the generate storyboards methods.
//------------------------------------------------------------------------
internal sealed class ThemeGeneratorHelper
{
	private readonly DependencyObject? _target;
	private readonly string? _targetName;
	private readonly bool _onlyGenerateSteadyState;
	private readonly TimelineCollection _timelineCollection;

	private long _begintime;   // the begintime set on the timeline
	private long _additionalTime; // additional time, that might be used

	private bool _originValuesSet;
	private bool _clipValuesSet;

	private readonly Point _startOffset;
	private readonly Point _destinationOffset;

	private readonly string _strTranslateXPropertyName;
	private readonly string _strTranslateYPropertyName;
	private readonly string _strOpacityPropertyName;
	private readonly string _strCenterYPropertyName;
	private readonly string _strScaleXPropertyName;
	private readonly string _strScaleYPropertyName;
	private readonly string _strClipScaleXPropertyName;
	private readonly string _strClipScaleYPropertyName;
	private readonly string _strClipTranslateXPropertyName;
	private readonly string _strClipTranslateYPropertyName;

	private string? _strOverrideTranslateXPropertyName;
	private string? _strOverrideTranslateYPropertyName;

	private readonly Dictionary<string, DoubleAnimationUsingKeyFrames> _doubleAnimationsMap = new();

	private double _initialOpacity;
	private bool _overrideInitialOpacity;

	public ThemeGeneratorHelper(
		Point startOffset,
		Point destinationOffset,
		string? targetName,
		DependencyObject? target,
		bool onlyGenerateSteadyState,
		TimelineCollection timelineCollection)
	{
		_originValuesSet = false;
		_clipValuesSet = false;
		_timelineCollection = timelineCollection;
		_startOffset = startOffset;
		_destinationOffset = destinationOffset;
		_targetName = targetName;
		_target = target;
		_onlyGenerateSteadyState = onlyGenerateSteadyState;
		_begintime = 0;
		_additionalTime = 0;
		_initialOpacity = 0;
		_overrideInitialOpacity = false;

		_strTranslateXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).TranslateX";
		_strTranslateYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).TranslateY";
		_strOpacityPropertyName = "(UIElement.TransitionTarget).Opacity";
		_strCenterYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).CenterY";
		_strScaleXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).ScaleX";
		_strScaleYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).ScaleY";
		_strClipScaleXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.ScaleX)";
		_strClipScaleYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.ScaleY)";
		_strClipTranslateXPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.TranslateX)";
		_strClipTranslateYPropertyName = "(UIElement.TransitionTarget).(TransitionTarget.ClipTransform).(CompositeTransform.TranslateY)";
	}

	public void Initialize()
	{
		// Faulting in the TransitionTarget on the resolved target ensures the property paths above
		// have a real DependencyObject chain to walk through. WinUI handles this implicitly via
		// CCoreServices::SetAllowTransitionTargetCreation on DynamicTimeline::OnBegin.
		if (_target is UIElement targetElement)
		{
			_ = targetElement.TransitionTarget;
		}
	}

	public Point GetStartOffset() => _startOffset;
	public Point GetDestinationOffset() => _destinationOffset;

	public string GetTranslateXPropertyName() => _strOverrideTranslateXPropertyName ?? _strTranslateXPropertyName;
	public string GetTranslateYPropertyName() => _strOverrideTranslateYPropertyName ?? _strTranslateYPropertyName;
	public string GetOpacityPropertyName() => _strOpacityPropertyName;
	public string GetCenterYPropertyName() => _strCenterYPropertyName;
	public string GetScaleXPropertyName() => _strScaleXPropertyName;
	public string GetScaleYPropertyName() => _strScaleYPropertyName;
	public string GetClipScaleXPropertyName() => _strClipScaleXPropertyName;
	public string GetClipScaleYPropertyName() => _strClipScaleYPropertyName;
	public string GetClipTranslateXPropertyName() => _strClipTranslateXPropertyName;
	public string GetClipTranslateYPropertyName() => _strClipTranslateYPropertyName;

	public void RegisterKeyFrame(
		string targetPropertyName,
		double value,
		long begintime,
		long duration,
		TimingFunctionDescription? pEasing)
	{
		if (string.IsNullOrEmpty(targetPropertyName))
		{
			return;
		}

		DoubleKeyFrame pKeyFrame;

		if (pEasing is null)
		{
			pKeyFrame = new DiscreteDoubleKeyFrame();
		}
		else if (pEasing.Value.IsLinear() || _onlyGenerateSteadyState)
		{
			pKeyFrame = new LinearDoubleKeyFrame();
		}
		else
		{
			var spline = new KeySpline
			{
				ControlPoint1 = pEasing.Value.cp2,
				ControlPoint2 = pEasing.Value.cp3,
			};
			pKeyFrame = new SplineDoubleKeyFrame
			{
				KeySpline = spline,
			};
		}

		// a keyframe needs to be registered inside a timeline. These are cached in the map, keyed on the property.
		if (!_doubleAnimationsMap.TryGetValue(targetPropertyName, out var pKeyframes))
		{
			pKeyframes = new DoubleAnimationUsingKeyFrames();
			_doubleAnimationsMap[targetPropertyName] = pKeyframes;

			_timelineCollection.Add(pKeyframes);    // this will not addref since pKeyframes does not have state

			if (_target is not null)
			{
				Storyboard.SetTarget(pKeyframes, _target);
			}
			else if (!string.IsNullOrEmpty(_targetName))
			{
				Storyboard.SetTargetName(pKeyframes, _targetName);
			}
			Storyboard.SetTargetProperty(pKeyframes, targetPropertyName);

			// with the first keyframe to be created, we'll take its time and actually start the timeline at that point
			if (begintime > 0 && !_onlyGenerateSteadyState)
			{
				_begintime = begintime;    // cache for later consumption
				var beginTimespan = TimeSpan.FromTicks(_begintime * 10000);
				pKeyframes.BeginTime = beginTimespan;
			}
		}

		var spKeyframeCollection = pKeyframes.KeyFrames;
		if (_onlyGenerateSteadyState)
		{
			// steady state animation is created by only taking the last keyframe and setting its keytime to 0.
			global::System.Diagnostics.Debug.Assert(spKeyframeCollection.Count < 2); // would expect one at the very most
			if (spKeyframeCollection.Count > 0)
			{
				spKeyframeCollection.Clear();
			}
		}
		spKeyframeCollection.Add(pKeyFrame);

		// correct for the begintime set on the timeline
		long time = _onlyGenerateSteadyState ? 0 : begintime + duration - _begintime;

		// initialize with keytime from transform
		var keyTimeTimeSpan = TimeSpan.FromTicks(10000 * time);  // keyframe keytime is where a value needs to be at a certain time

		var keyTime = KeyTime.FromTimeSpan(keyTimeTimeSpan);
		pKeyFrame.KeyTime = keyTime;
		pKeyFrame.Value = value;
	}

	public void Set2DTransformOriginValues(Point originPoint)
	{
		if (!_originValuesSet)   // only support one set of origin. Consider throwing if incoming is different
		{
			_originValuesSet = true;
			if (_target is UIElement targetElement)
			{
				targetElement.TransitionTarget.TransformOrigin = originPoint;
			}
		}
	}

	public void SetClipOriginValues(Point originPoint)
	{
		if (!_clipValuesSet)   // only support one set of origin. Consider throwing if incoming is different
		{
			_clipValuesSet = true;
			if (_target is UIElement targetElement)
			{
				targetElement.TransitionTarget.ClipTransformOrigin = originPoint;
			}
		}
	}

	public void SetOverrideTranslateXPropertyName(string value) => _strOverrideTranslateXPropertyName = value;

	public void SetOverrideTranslateYPropertyName(string value) => _strOverrideTranslateYPropertyName = value;

	public void SetOverrideInitialOpacity(double initialOpacity)
	{
		_initialOpacity = initialOpacity;
		_overrideInitialOpacity = true;
	}

	public bool GetInitialOpacity(out double fallbackValue)
	{
		fallbackValue = _initialOpacity;
		return _overrideInitialOpacity;
	}

	public void SetAdditionalTime(long time) => _additionalTime = time;

	public long GetAdditionalTime() => _additionalTime;
}
