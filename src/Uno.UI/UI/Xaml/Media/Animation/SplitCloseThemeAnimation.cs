// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\SplitCloseThemeAnimation_Partial.h, commit 978ab6363
// MUX Reference src\dxaml\xcp\dxaml\lib\ThemeAnimations.cpp, commit 978ab6363

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class SplitCloseThemeAnimation : ITimeline
{
	private const long s_CloseDuration = 167;
	private const long s_OpacityChangeDuration = 83;
	private const long s_OpacityChangeBeginTime = s_CloseDuration - s_OpacityChangeDuration;

	// In WinUI, SplitCloseThemeAnimation derives from DynamicTimeline whose OnBegin lazily generates child
	// timelines into the parent timeline group. Uno hosts the generated timelines on a private inner
	// Storyboard (Storyboard fills the same role as ParallelTimeline) and forwards Begin/Stop/Completed.
	private Storyboard? _innerStoryboard;
	private EventHandler<object>? _innerCompletedHandler;

	void ITimeline.Begin()
	{
		State = TimelineState.Active;

		StopInner();

		var sb = new Storyboard();
		try
		{
			CreateTimelines(bOnlyGenerateSteadyState: false, sb.Children);
		}
		catch (Exception ex) when (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().LogWarning($"Failed to generate timelines for SplitCloseThemeAnimation: {ex}");
		}

		_innerStoryboard = sb;

		if (sb.Children.Count == 0)
		{
			ScheduleAsyncCompletion();
			return;
		}

		_innerCompletedHandler = OnInnerCompleted;
		sb.Completed += _innerCompletedHandler;
		sb.Begin();
	}

	void ITimeline.Stop()
	{
		State = TimelineState.Stopped;
		StopInner();
	}

	private void OnInnerCompleted(object? sender, object? args)
	{
		if (State == TimelineState.Stopped)
		{
			return;
		}

		// Detach the handler but leave the inner storyboard's children in their fill state so the held values
		// (e.g., the final opacity/translate) remain visible after the animation has finished. Stop is only
		// invoked when ITimeline.Stop is called explicitly.
		if (_innerStoryboard is { } sb && _innerCompletedHandler is { } h)
		{
			sb.Completed -= h;
			_innerCompletedHandler = null;
		}

		State = TimelineState.Stopped;
		OnCompleted();
	}

	private void StopInner()
	{
		if (_innerStoryboard is { } sb)
		{
			if (_innerCompletedHandler is { } h)
			{
				sb.Completed -= h;
				_innerCompletedHandler = null;
			}
			sb.Stop();
			_innerStoryboard = null;
		}
	}

	private void ScheduleAsyncCompletion()
	{
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			if (State == TimelineState.Stopped)
			{
				return;
			}

			State = TimelineState.Stopped;
			OnCompleted();
		});
	}

	private void CreateTimelines(bool bOnlyGenerateSteadyState, TimelineCollection pTimelineCollection)
	{
		// unfortunately the split open is different enough from split close that I don't see that much value in moving code to a helper method.
		// also, we need to have the flexibility to change these independently, even though they use the same primitives at the moment.
		const double closedRatio = 0.15;
		var nullPoint = new Point();
		var splitOrigin = new Point(0, 0.5);
		var linear = new TimingFunctionDescription();
		var easing = new TimingFunctionDescription();
		easing.cp3.X = 0.0; // Cubic-bezier (0, 0, 0, 1). Default TimingFunctionDescription() constructor creates a Linear curve (0,0,0,0,1,1,1,1).

		ThemeGeneratorHelper? pBackgroundSupplier = null;
		ThemeGeneratorHelper? pPanelSupplier = null;
		ThemeGeneratorHelper? pFaceplateSupplier = null;

		// Parts and rects.
		var strOpenName = OpenedTargetName;
		var pOpenTarget = OpenedTarget;
		var strContentName = ContentTargetName;
		var pContentTarget = ContentTarget;
		var strCloseName = ClosedTargetName;
		var pCloseTarget = ClosedTarget;
		double closedLength = ClosedLength;
		double openedLength = OpenedLength;
		double offsetFromCenter = OffsetFromCenter;
		double contentTranslateOffset = ContentTranslationOffset;
		double initialClipScaleY = 0;
		double finalClipScaleY = 0;
		AnimationDirection direction = ContentTranslationDirection;

		if ((string.IsNullOrEmpty(strOpenName) && pOpenTarget is null) || openedLength == 0)   // dividing by openlength
		{
			return;
		}

		// Resolve targets by name from the templated parent's namescope. WinUI does this implicitly through
		// Storyboard.SetTargetName resolution as part of the parent storyboard chain; because the generated
		// timelines live on a synthetic inner Storyboard outside the live tree, we resolve up front.
		pOpenTarget ??= ResolveTargetByName(strOpenName);
		pContentTarget ??= ResolveTargetByName(strContentName);
		pCloseTarget ??= ResolveTargetByName(strCloseName);

		// Since we will be translating the clip, that means, for example, that
		// a clip scale of 1.0 would not cover the element entirely. Thus we
		// need to adjust the scale accordingly. In other words, if the area
		// covered by the clip is partially translated off the element, then
		// we need to make the clip bigger in order to compensate.

		initialClipScaleY = (0.5 + Math.Abs(offsetFromCenter / openedLength)) * 2;

		var spClosedLength = ReadLocalValue(ClosedLengthProperty);
		bool isUnsetValue = spClosedLength == DependencyProperty.UnsetValue;

		// If the ClosedLength property was not set, by default we will instead use
		// a specific proportion of the OpenedLength.
		if (isUnsetValue)
		{
			double clipLength = openedLength * closedRatio;
			double maxOffset = openedLength * (1 - closedRatio) / 2.0;   // Max offset possible before the clip is partially off the element.
			if (Math.Abs(offsetFromCenter) > maxOffset)
			{
				double pixelsOff = (clipLength / 2.0) - (openedLength / 2.0 - Math.Abs(offsetFromCenter));
				finalClipScaleY = pixelsOff / openedLength * 2.0 + closedRatio;
			}
			else
			{
				finalClipScaleY = closedRatio;
			}
		}
		else
		{
			finalClipScaleY = closedLength / openedLength;
		}

		// ********** background, clip and opacity ************
		pBackgroundSupplier = new ThemeGeneratorHelper(nullPoint, nullPoint, strOpenName, pOpenTarget, bOnlyGenerateSteadyState, pTimelineCollection);
		pBackgroundSupplier.Initialize();
		pBackgroundSupplier.SetClipOriginValues(splitOrigin);
		pBackgroundSupplier.RegisterKeyFrame(pBackgroundSupplier.GetClipScaleYPropertyName(), initialClipScaleY, 0, 0, easing);
		pBackgroundSupplier.RegisterKeyFrame(pBackgroundSupplier.GetClipScaleYPropertyName(), finalClipScaleY, 0, s_CloseDuration, easing);
		pBackgroundSupplier.RegisterKeyFrame(pBackgroundSupplier.GetClipTranslateYPropertyName(), offsetFromCenter, 0, 0, easing);  // immediately go there

		pBackgroundSupplier.RegisterKeyFrame(pBackgroundSupplier.GetOpacityPropertyName(), 1.0, 0, 0, linear);
		pBackgroundSupplier.RegisterKeyFrame(pBackgroundSupplier.GetOpacityPropertyName(), 1.0, 0, s_OpacityChangeBeginTime, linear);
		pBackgroundSupplier.RegisterKeyFrame(pBackgroundSupplier.GetOpacityPropertyName(), 0.0, s_OpacityChangeBeginTime, s_OpacityChangeDuration, linear);

		// ********* content, opacity and translation *********
		if (!string.IsNullOrEmpty(strContentName) || pContentTarget is not null)
		{
			pPanelSupplier = new ThemeGeneratorHelper(nullPoint, nullPoint, strContentName, pContentTarget, bOnlyGenerateSteadyState, pTimelineCollection);
			pPanelSupplier.Initialize();
			pPanelSupplier.RegisterKeyFrame(pPanelSupplier.GetOpacityPropertyName(), 1, 0, 0, linear);    // start off fully opaque
			pPanelSupplier.RegisterKeyFrame(pPanelSupplier.GetOpacityPropertyName(), 1.0, 0, s_OpacityChangeBeginTime, linear);
			pPanelSupplier.RegisterKeyFrame(pPanelSupplier.GetOpacityPropertyName(), 0.0, s_OpacityChangeBeginTime, s_OpacityChangeDuration, linear);

			if (direction == AnimationDirection.Top || direction == AnimationDirection.Bottom)
			{
				pPanelSupplier.RegisterKeyFrame(pPanelSupplier.GetTranslateYPropertyName(), 0, 0, 0, easing);    // start off with offset so we can come to rest at 0
				pPanelSupplier.RegisterKeyFrame(
					pPanelSupplier.GetTranslateYPropertyName(),
					direction == AnimationDirection.Bottom ? contentTranslateOffset : -contentTranslateOffset,
					0,
					s_CloseDuration,
					easing);
			}
			else
			{
				pPanelSupplier.RegisterKeyFrame(pPanelSupplier.GetTranslateXPropertyName(), 0, 0, 0, easing);    // start off with offset so we can come to rest at 0
				pPanelSupplier.RegisterKeyFrame(
					pPanelSupplier.GetTranslateXPropertyName(),
					direction == AnimationDirection.Right ? contentTranslateOffset : -contentTranslateOffset,
					0,
					s_CloseDuration,
					easing);
			}
		}

		// *************** faceplate, opacity *****************
		if (!string.IsNullOrEmpty(strCloseName) || pCloseTarget is not null)
		{
			pFaceplateSupplier = new ThemeGeneratorHelper(nullPoint, nullPoint, strCloseName, pCloseTarget, bOnlyGenerateSteadyState, pTimelineCollection);
			pFaceplateSupplier.Initialize();
			pFaceplateSupplier.RegisterKeyFrame(pFaceplateSupplier.GetOpacityPropertyName(), 0.0, 0, 0, linear);
			pFaceplateSupplier.RegisterKeyFrame(pFaceplateSupplier.GetOpacityPropertyName(), 0.0, 0, s_OpacityChangeBeginTime, linear);
			pFaceplateSupplier.RegisterKeyFrame(pFaceplateSupplier.GetOpacityPropertyName(), 1.0, s_OpacityChangeBeginTime, s_OpacityChangeDuration, linear);
		}
	}

	// Resolve a target by name from the templated parent's namescope. The generated timelines do not live
	// in the live visual tree, so the standard Storyboard.SetTargetName resolution does not apply.
	private DependencyObject? ResolveTargetByName(string? name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}

		object? current = this;
		while (current is not null)
		{
			if (current is FrameworkElement fe)
			{
				var found = fe.FindName(name) as DependencyObject;
				if (found is not null)
				{
					return found;
				}
			}
			current = (current as DependencyObject)?.GetParent();
		}
		return null;
	}
}
