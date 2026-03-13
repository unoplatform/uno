// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemCollectionTransitionProvider.cpp, tag winui3/release/1.8.4

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class LinedFlowLayoutItemCollectionTransitionProvider : ItemCollectionTransitionProvider
{
	private const float Epsilon = 0.001f;
	private static readonly TimeSpan QuickAnimationDuration = TimeSpan.FromMilliseconds(167);
	private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(250);
	private const float SlideInDistance = 100.0f;
	private static readonly Vector3 ScaleVisible = new(1.0f, 1.0f, 1.0f);
	private static readonly Vector3 ScaleCollapsed = new(0.0f, 0.0f, 1.0f);

	private readonly Dictionary<Visual, CompositionAnimationGroup> _currentAnimationGroupByVisual = new();
	private CubicBezierEasingFunction _easeInFunction;
	private CubicBezierEasingFunction _easeOutFunction;

	protected override bool ShouldAnimateCore(ItemCollectionTransition transition)
	{
		return true;
	}

	protected override void StartTransitions(IList<ItemCollectionTransition> transitions)
	{
		var addTransitions = new List<ItemCollectionTransition>();
		var removeTransitions = new List<ItemCollectionTransition>();
		var moveTransitions = new List<ItemCollectionTransition>();

		foreach (var transition in transitions)
		{
			switch (transition.Operation)
			{
				case ItemCollectionTransitionOperation.Add:
					addTransitions.Add(transition);
					break;
				case ItemCollectionTransitionOperation.Remove:
					removeTransitions.Add(transition);
					break;
				case ItemCollectionTransitionOperation.Move:
					moveTransitions.Add(transition);
					break;
			}
		}

		StartAddTransitions(addTransitions, removeTransitions.Count > 0, moveTransitions.Count > 0);
		StartRemoveTransitions(removeTransitions, addTransitions.Count > 0);
		StartMoveTransitions(moveTransitions, removeTransitions.Count > 0);
	}

	private void StartAddTransitions(List<ItemCollectionTransition> transitions, bool hasRemoveTransitions, bool hasMoveTransitions)
	{
		foreach (var transition in transitions)
		{
			var progress = transition.Start();
			var visual = ElementCompositionPreview.GetElementVisual(progress.Element);
			var compositor = visual.Compositor;
			var animationGroup = compositor.CreateAnimationGroup();

			if ((transition.Triggers & ItemCollectionTransitionTriggers.CollectionChangeAdd) == 0)
			{
				visual.Scale = ScaleVisible;
				SlideIn(animationGroup, compositor, TimeSpan.Zero);
			}
			else
			{
				var delay = TimeSpan.Zero;

				if (hasMoveTransitions)
				{
					delay += DefaultAnimationDuration + DefaultAnimationDuration;
				}

				if (hasRemoveTransitions)
				{
					delay += QuickAnimationDuration;
				}

				visual.CenterPoint = GetCenterPoint(progress.Element);
				visual.Opacity = 1.0f;
				visual.TransformMatrix = Matrix4x4.Identity;

				ScaleIn(animationGroup, compositor, delay, presetInitialValue: true);
			}

			var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
			StartAnimationGroup(visual, animationGroup);
			batch.End();
			batch.Completed += (s, e) =>
			{
				_currentAnimationGroupByVisual.Remove(visual);
				progress.Complete();
			};
		}
	}

	private void StartRemoveTransitions(List<ItemCollectionTransition> transitions, bool hasAddAnimations)
	{
		foreach (var transition in transitions)
		{
			if ((transition.Triggers & ItemCollectionTransitionTriggers.CollectionChangeReset) != 0 && hasAddAnimations)
			{
				continue;
			}

			var progress = transition.Start();
			var visual = ElementCompositionPreview.GetElementVisual(progress.Element);
			var compositor = visual.Compositor;
			var animationGroup = compositor.CreateAnimationGroup();

			visual.CenterPoint = GetCenterPoint(progress.Element);
			visual.Opacity = 1.0f;
			visual.TransformMatrix = Matrix4x4.Identity;

			ScaleOut(animationGroup, compositor, TimeSpan.Zero);

			var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
			StartAnimationGroup(visual, animationGroup);
			batch.End();
			batch.Completed += (s, e) =>
			{
				_currentAnimationGroupByVisual.Remove(visual);
				progress.Complete();
			};
		}
	}

	private void StartMoveTransitions(List<ItemCollectionTransition> transitions, bool hasRemoveTransitions)
	{
		foreach (var transition in transitions)
		{
			if ((transition.Triggers & ItemCollectionTransitionTriggers.LayoutTransition) != 0)
			{
				continue;
			}

			var progress = transition.Start();
			var oldBounds = transition.OldBounds;
			var newBounds = transition.NewBounds;

			var visual = ElementCompositionPreview.GetElementVisual(progress.Element);
			var compositor = visual.Compositor;
			var animationGroup = compositor.CreateAnimationGroup();

			var waitBeforeAnimations = hasRemoveTransitions ? QuickAnimationDuration : TimeSpan.Zero;

			if (Math.Abs(oldBounds.Y - newBounds.Y) < Epsilon)
			{
				visual.Opacity = 1.0f;
				visual.Scale = ScaleVisible;
				Translate(animationGroup, compositor, oldBounds, newBounds, waitBeforeAnimations + TimeSpan.FromMilliseconds(QuickAnimationDuration.TotalMilliseconds / 2), presetInitialValue: true);

				var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
				StartAnimationGroup(visual, animationGroup);
				batch.End();
				batch.Completed += (s, e) =>
				{
					_currentAnimationGroupByVisual.Remove(visual);
					progress.Complete();
				};
			}
			else
			{
				var scaleInCenterPoint = GetCenterPoint(progress.Element);
				var scaleOutCenterPoint = new Vector3(
					scaleInCenterPoint.X + (float)(oldBounds.X - newBounds.X),
					scaleInCenterPoint.Y + (float)(oldBounds.Y - newBounds.Y),
					scaleInCenterPoint.Z);

				var startBounds = new Vector3(
					(float)(oldBounds.X - newBounds.X),
					(float)(oldBounds.Y - newBounds.Y),
					0.0f);

				var originalTransformMatrix = visual.TransformMatrix;

				visual.CenterPoint = scaleOutCenterPoint;
				visual.Opacity = 1.0f;
				visual.TransformMatrix = originalTransformMatrix * Matrix4x4.CreateTranslation(startBounds);

				ScaleOut(animationGroup, compositor, waitBeforeAnimations + TimeSpan.FromMilliseconds(QuickAnimationDuration.TotalMilliseconds / 2), presetInitialValue: true);

				var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
				StartAnimationGroup(visual, animationGroup);
				batch.End();
				batch.Completed += (s, e) =>
				{
					_currentAnimationGroupByVisual.Remove(visual);

					visual.CenterPoint = scaleInCenterPoint;
					visual.TransformMatrix = originalTransformMatrix;

					var scaleInGroup = compositor.CreateAnimationGroup();
					ScaleIn(scaleInGroup, compositor, TimeSpan.Zero);

					var innerBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
					StartAnimationGroup(visual, scaleInGroup);
					innerBatch.End();
					innerBatch.Completed += (s2, e2) =>
					{
						_currentAnimationGroupByVisual.Remove(visual);
						progress.Complete();
					};
				};
			}
		}
	}

	private void SlideIn(CompositionAnimationGroup group, Compositor compositor, TimeSpan delay)
	{
		var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
		opacityAnimation.InsertKeyFrame(0.0f, 0.0f);
		opacityAnimation.InsertKeyFrame(1.0f, 1.0f);
		opacityAnimation.Duration = DefaultAnimationDuration;
		opacityAnimation.Target = "Opacity";

		if (delay > TimeSpan.Zero)
		{
			opacityAnimation.DelayTime = delay;
			opacityAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		}

		var translationAnimation = compositor.CreateVector2KeyFrameAnimation();
		translationAnimation.SetVector2Parameter("start", new Vector2(0, SlideInDistance));
		translationAnimation.SetVector2Parameter("end", Vector2.Zero);
		translationAnimation.InsertExpressionKeyFrame(0.0f, "start");
		translationAnimation.InsertExpressionKeyFrame(1.0f, "end", GetEaseInFunction(compositor));
		translationAnimation.Duration = DefaultAnimationDuration;
		translationAnimation.Target = "TransformMatrix._41_42";

		if (delay > TimeSpan.Zero)
		{
			translationAnimation.DelayTime = delay;
			translationAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
		}

		group.Add(opacityAnimation);
		group.Add(translationAnimation);
	}

	private void ScaleIn(CompositionAnimationGroup group, Compositor compositor, TimeSpan delay, bool presetInitialValue = false)
	{
		var animation = compositor.CreateVector3KeyFrameAnimation();
		animation.InsertKeyFrame(0.0f, ScaleCollapsed);
		animation.InsertKeyFrame(1.0f, ScaleVisible, GetEaseInFunction(compositor));
		animation.Duration = QuickAnimationDuration;
		animation.Target = "Scale";

		if (delay > TimeSpan.Zero)
		{
			animation.DelayTime = delay;
			animation.DelayBehavior = presetInitialValue ?
				AnimationDelayBehavior.SetInitialValueBeforeDelay :
				AnimationDelayBehavior.SetInitialValueAfterDelay;
		}

		group.Add(animation);
	}

	private void ScaleOut(CompositionAnimationGroup group, Compositor compositor, TimeSpan delay, bool presetInitialValue = false)
	{
		var animation = compositor.CreateVector3KeyFrameAnimation();
		animation.InsertKeyFrame(0.0f, ScaleVisible);
		animation.InsertKeyFrame(1.0f, ScaleCollapsed, GetEaseOutFunction(compositor));
		animation.Duration = QuickAnimationDuration;
		animation.Target = "Scale";

		if (delay > TimeSpan.Zero)
		{
			animation.DelayTime = delay;
			animation.DelayBehavior = presetInitialValue ?
				AnimationDelayBehavior.SetInitialValueBeforeDelay :
				AnimationDelayBehavior.SetInitialValueAfterDelay;
		}

		group.Add(animation);
	}

	private void Translate(CompositionAnimationGroup group, Compositor compositor, Rect from, Rect to, TimeSpan delay, bool presetInitialValue = false)
	{
		var positionAnimation = compositor.CreateVector2KeyFrameAnimation();
		positionAnimation.SetVector2Parameter("start", new Vector2((float)(from.X - to.X), (float)(from.Y - to.Y)));
		positionAnimation.SetVector2Parameter("end", Vector2.Zero);
		positionAnimation.InsertExpressionKeyFrame(0.0f, "start");
		positionAnimation.InsertExpressionKeyFrame(1.0f, "end", GetEaseInFunction(compositor));
		positionAnimation.Duration = DefaultAnimationDuration;
		positionAnimation.Target = "TransformMatrix._41_42";

		if (delay > TimeSpan.Zero)
		{
			positionAnimation.DelayTime = delay;
			positionAnimation.DelayBehavior = presetInitialValue ?
				AnimationDelayBehavior.SetInitialValueBeforeDelay :
				AnimationDelayBehavior.SetInitialValueAfterDelay;
		}

		group.Add(positionAnimation);
	}

	private static Vector3 GetCenterPoint(UIElement element)
	{
		return new Vector3(element.ActualSize.X / 2, element.ActualSize.Y / 2, 0.0f);
	}

	private CubicBezierEasingFunction GetEaseInFunction(Compositor compositor)
	{
		_easeInFunction ??= compositor.CreateCubicBezierEasingFunction(new Vector2(0.55f, 0), new Vector2(0, 1));
		return _easeInFunction;
	}

	private CubicBezierEasingFunction GetEaseOutFunction(Compositor compositor)
	{
		_easeOutFunction ??= compositor.CreateCubicBezierEasingFunction(new Vector2(1, 0), new Vector2(1, 1));
		return _easeOutFunction;
	}

	private void StartAnimationGroup(Visual visual, CompositionAnimationGroup animationGroup)
	{
		if (_currentAnimationGroupByVisual.TryGetValue(visual, out var existingGroup))
		{
			visual.StopAnimationGroup(existingGroup);
			_currentAnimationGroupByVisual.Remove(visual);
		}

		_currentAnimationGroupByVisual[visual] = animationGroup;
		visual.StartAnimationGroup(animationGroup);
	}
}
