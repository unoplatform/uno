// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI.Composition;
using Microsoft.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Microsoft.UI.Dispatching;
using Windows.Foundation;

namespace MUXControlsTestApp.Utilities;

internal sealed class CompositionScrollControllerPanningInfo : IScrollControllerPanningInfo
{
	private DispatcherQueue dispatcherQueue = null;
	private CompositionPropertySet expressionAnimationSources = null;
	private ExpressionAnimation thumbOffsetAnimation = null;
	private Visual panningVisual = null;
	private FrameworkElement panningFrameworkElement = null;
	private UIElement panningElementAncestor = null;
	private Orientation panOrientation = Orientation.Vertical;
	private bool isAnimatingThumbOffset = true;
	private bool isThumbPannable = true;
	private bool isThumbPositionMirrored = false;
	private bool isRailEnabled = true;
	private string minOffsetPropertyName;
	private string maxOffsetPropertyName;
	private string offsetPropertyName;
	private string multiplierPropertyName;
	private double minOffset;
	private double maxOffset;

	public event TypedEventHandler<IScrollControllerPanningInfo, String> LogMessage;
	public event TypedEventHandler<IScrollControllerPanningInfo, Object> Changed;
	public event TypedEventHandler<IScrollControllerPanningInfo, ScrollControllerPanRequestedEventArgs> PanRequested;

	internal bool IsAnimatingThumbOffset
	{
		get
		{
			return isAnimatingThumbOffset;
		}
		set
		{
			if (isAnimatingThumbOffset != value)
			{
				isAnimatingThumbOffset = value;
			}
		}
	}

	internal bool IsThumbPannable
	{
		get
		{
			return isThumbPannable;
		}
		set
		{
			if (isThumbPannable != value)
			{
				isThumbPannable = value;
				RaiseChanged();
			}
		}
	}

	internal bool IsThumbPositionMirrored
	{
		get
		{
			return isThumbPositionMirrored;
		}
		set
		{
			if (isThumbPositionMirrored != value)
			{
				if (isAnimatingThumbOffset)
				{
					StopThumbAnimation();
				}

				isThumbPositionMirrored = value;
				UpdatePanningElementOffsetMultiplier();

				if (isAnimatingThumbOffset)
				{
					UpdateThumbExpression();
					StartThumbAnimation();
				}

			}
		}
	}

	internal Visual PanningVisual
	{
		get
		{
			return panningVisual;
		}
	}

	// Returns the thumb track, an ancestor of the pannable thumb.
	public UIElement PanningElementAncestor
	{
		get
		{
			// Note: Returning panning UIElement would cause flickers when interacting with it (InteractionTracker issue).
			RaiseLogMessage("CompositionScrollControllerPanningInfo: get_PanningElementAncestor for PanOrientation=" + panOrientation);

			return isThumbPannable ? panningElementAncestor : null;
		}
		private set
		{
			if (panningElementAncestor != value)
			{
				panningElementAncestor = value;
				RaiseChanged();
			}
		}
	}

	// Returns True when the pannable thumb is locked on a track.
	public bool IsRailEnabled
	{
		get
		{
			return isRailEnabled;
		}
		internal set
		{
			if (isRailEnabled != value)
			{
				isRailEnabled = value;
				RaiseChanged();
			}
		}
	}

	public Orientation PanOrientation
	{
		get
		{
			RaiseLogMessage("CompositionScrollControllerPanningInfo: get_PanOrientation for PanOrientation=" + panOrientation);

			return panOrientation;
		}
		internal set
		{
			if (panOrientation != value)
			{
				panOrientation = value;
				RaiseChanged();
			}
		}
	}

	public void SetPanningElementExpressionAnimationSources(
		CompositionPropertySet propertySet,
		string minOffsetPropertyName,
		string maxOffsetPropertyName,
		string offsetPropertyName,
		string multiplierPropertyName)
	{
		RaiseLogMessage(
			"CompositionScrollControllerPanningInfo: SetPanningElementExpressionAnimationSources for PanOrientation=" + panOrientation +
			" with minOffsetPropertyName=" + minOffsetPropertyName +
			", maxOffsetPropertyName=" + maxOffsetPropertyName +
			", offsetPropertyName=" + offsetPropertyName +
			", multiplierPropertyName=" + multiplierPropertyName);

		expressionAnimationSources = propertySet;
		if (expressionAnimationSources != null)
		{
			this.minOffsetPropertyName = minOffsetPropertyName.Trim();
			this.maxOffsetPropertyName = maxOffsetPropertyName.Trim();
			this.offsetPropertyName = offsetPropertyName.Trim();
			this.multiplierPropertyName = multiplierPropertyName.Trim();

			UpdatePanningElementOffsetMultiplier();

			if (thumbOffsetAnimation == null && IsAnimatingThumbOffset)
			{
				EnsureThumbAnimation();
				UpdateThumbExpression();
				StartThumbAnimation();
			}
		}
		else
		{
			this.minOffsetPropertyName =
			this.maxOffsetPropertyName =
			this.offsetPropertyName =
			this.multiplierPropertyName = string.Empty;

			if (IsAnimatingThumbOffset)
			{
				thumbOffsetAnimation = null;
				StopThumbAnimation();
			}
		}
	}

	internal CompositionScrollControllerPanningInfo(
		DispatcherQueue dispatcherQueue,
		bool isRailEnabled,
		Orientation panOrientation)
	{
		this.dispatcherQueue = dispatcherQueue;
		this.IsRailEnabled = isRailEnabled;
		this.PanOrientation = panOrientation;
	}

	internal void SetPanningFrameworkElement(FrameworkElement value)
	{
		if (panningFrameworkElement != value)
		{
			panningFrameworkElement = value;

			if (panningFrameworkElement == null)
			{
				panningVisual = null;

				PanningElementAncestor = null;
			}
			else
			{
				panningVisual = ElementCompositionPreview.GetElementVisual(panningFrameworkElement);
				ElementCompositionPreview.SetIsTranslationEnabled(panningFrameworkElement, true);

				PanningElementAncestor = panningFrameworkElement.Parent as UIElement;
			}
		}
	}

	// Evaluates the relative speed (mutiplier) between the IScrollController consumer content and 
	// the pannable thumb. Then updates the expressionAnimationSources's multiplier scalar.
	internal void UpdatePanningElementOffsetMultiplier()
	{
		if (expressionAnimationSources != null && !string.IsNullOrWhiteSpace(multiplierPropertyName))
		{
			float panningVisualScrollMultiplier = PanningElementOffsetMultiplier;

			RaiseLogMessage("CompositionScrollControllerPanningInfo: UpdatePanningElementOffsetMultiplier for PanOrientation=" + panOrientation +
				", PanningElementOffsetMultiplier=" + panningVisualScrollMultiplier);

			expressionAnimationSources.InsertScalar(multiplierPropertyName, panningVisualScrollMultiplier);
		}
	}

	// Updates the pannable thumb's length according to the dimensions given by IScrollController.SetValues(...) 
	// and IScrollController's UI size.
	internal bool UpdatePanningFrameworkElementLength(
		double minOffset,
		double maxOffset,
		double viewportLength)
	{
		this.minOffset = minOffset;
		this.maxOffset = maxOffset;

		if (panningFrameworkElement != null)
		{
			double parentWidth = 0.0;
			double parentHeight = 0.0;
			FrameworkElement parent = panningFrameworkElement.Parent as FrameworkElement;

			if (parent != null)
			{
				parentWidth = parent.ActualWidth;
				parentHeight = parent.ActualHeight;
			}

			if (panOrientation == Orientation.Horizontal)
			{
				double newWidth;
				if (viewportLength == 0.0)
				{
					newWidth = 40.0;
				}
				else
				{
					newWidth = Math.Max(Math.Min(40.0, parentWidth), viewportLength / (maxOffset - minOffset + viewportLength) * parentWidth);
				}
				if (newWidth != panningFrameworkElement.Width)
				{
					RaiseLogMessage("CompositionScrollControllerPanningInfo: UpdatePanningFrameworkElementLength for PanOrientation=Horizontal, setting Width=" + newWidth);

					panningFrameworkElement.Width = newWidth;
					var ignored = dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdatePanningElementOffsetMultiplier);
					return true;
				}
			}
			else
			{
				double newHeight;
				if (viewportLength == 0.0)
				{
					newHeight = 40.0;
				}
				else
				{
					newHeight = Math.Max(Math.Min(40.0, parentHeight), viewportLength / (maxOffset - minOffset + viewportLength) * parentHeight);
				}
				if (newHeight != panningFrameworkElement.Height)
				{
					RaiseLogMessage("CompositionScrollControllerPanningInfo: UpdatePanningFrameworkElementLength for PanOrientation=Vertical, setting Height=" + newHeight);

					panningFrameworkElement.Height = newHeight;
					var ignored = dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdatePanningElementOffsetMultiplier);
					return true;
				}
			}
		}
		return false;
	}

	internal void StartThumbAnimation()
	{
		if (panningVisual != null && thumbOffsetAnimation != null)
		{
			if (panOrientation == Orientation.Horizontal)
			{
				panningVisual.StartAnimation("Translation.X", thumbOffsetAnimation);
			}
			else
			{
				panningVisual.StartAnimation("Translation.Y", thumbOffsetAnimation);
			}
		}
	}

	internal void StopThumbAnimation()
	{
		if (panningVisual != null)
		{
			if (panOrientation == Orientation.Horizontal)
			{
				panningVisual.StopAnimation("Translation.X");
			}
			else
			{
				panningVisual.StopAnimation("Translation.Y");
			}
		}
	}

	internal bool RaisePanRequested(PointerPoint pointerPoint)
	{
		RaiseLogMessage("CompositionScrollControllerPanningInfo: RaisePanRequested for PanOrientation=" + panOrientation + " with pointerPoint=" + pointerPoint);

		if (PanRequested != null)
		{
			ScrollControllerPanRequestedEventArgs args = new ScrollControllerPanRequestedEventArgs(pointerPoint);

			PanRequested(this, args);
			RaiseLogMessage("CompositionScrollControllerPanningInfo: RaisePanRequested result Handled=" + args.Handled);

			return args.Handled;
		}

		return false;
	}

	// Returns the multiplier between the IScrollController content movements and the 
	// pannable thumb movements.
	internal float PanningElementOffsetMultiplier
	{
		get
		{
			if (panningFrameworkElement != null)
			{
				panningFrameworkElement.UpdateLayout();

				double parentLength = 0.0;
				double panningFrameworkElementLength = panOrientation == Orientation.Horizontal ? panningFrameworkElement.ActualWidth : panningFrameworkElement.ActualHeight;
				FrameworkElement parent = panningFrameworkElement.Parent as FrameworkElement;
				if (parent != null)
				{
					parentLength = panOrientation == Orientation.Horizontal ? parent.ActualWidth : parent.ActualHeight;
				}
				if (parentLength != panningFrameworkElementLength)
				{
					RaiseLogMessage("CompositionScrollControllerPanningInfo: PanningElementOffsetMultiplier evaluation:");
					RaiseLogMessage("maxOffset:" + maxOffset);
					RaiseLogMessage("minOffset:" + minOffset);
					RaiseLogMessage("panningFrameworkElementLength:" + panningFrameworkElementLength);
					RaiseLogMessage("parentLength:" + parentLength);

					float multiplier = (float)((maxOffset - minOffset) / (panningFrameworkElementLength - parentLength));

					if (IsThumbPositionMirrored)
						multiplier *= -1.0f;

					return multiplier;
				}
			}

			return 0.0f;
		}
	}

	// Creates the thumbOffsetAnimation Composition expression animation used to 
	// position the vertical pannable thumb within its track.
	private void EnsureThumbAnimation()
	{
		if (thumbOffsetAnimation == null && expressionAnimationSources != null &&
			!string.IsNullOrWhiteSpace(multiplierPropertyName) &&
			!string.IsNullOrWhiteSpace(offsetPropertyName) &&
			!string.IsNullOrWhiteSpace(minOffsetPropertyName) &&
			!string.IsNullOrWhiteSpace(maxOffsetPropertyName))
		{
			thumbOffsetAnimation = expressionAnimationSources.Compositor.CreateExpressionAnimation();
			thumbOffsetAnimation.SetReferenceParameter("sources", expressionAnimationSources);
		}
	}

	// Updates the thumbOffsetAnimation Composition expression animation using the PropertySet provided
	// in the SetPanningElementExpressionAnimationSources call.
	private void UpdateThumbExpression()
	{
		if (thumbOffsetAnimation != null)
		{
			if (isThumbPositionMirrored)
			{
				thumbOffsetAnimation.Expression =
					"(sources." + maxOffsetPropertyName + " - min(sources." + maxOffsetPropertyName + ",max(sources." + minOffsetPropertyName + ",sources." + offsetPropertyName + ")))/(sources." + multiplierPropertyName + ")";
			}
			else
			{
				thumbOffsetAnimation.Expression =
					"min(sources." + maxOffsetPropertyName + ",max(sources." + minOffsetPropertyName + ",sources." + offsetPropertyName + "))/(-sources." + multiplierPropertyName + ")";
			}
		}
	}

	private void RaiseLogMessage(string logMessage)
	{
		if (LogMessage != null)
		{
			LogMessage(this, logMessage);
		}
	}

	private void RaiseChanged()
	{
		RaiseLogMessage("CompositionScrollControllerPanningInfo: RaiseChanged for PanOrientation=" + panOrientation);

		if (Changed != null)
		{
			Changed(this, null);
		}
	}
}

public class CompositionScrollControllerOffsetChangeCompletedEventArgs
{
	internal CompositionScrollControllerOffsetChangeCompletedEventArgs(int offsetChangeCorrelationId)
	{
		OffsetChangeCorrelationId = offsetChangeCorrelationId;
	}

	public int OffsetChangeCorrelationId
	{
		get;
		set;
	}
}

public sealed partial class CompositionScrollController : Control, IScrollController
{
	public event TypedEventHandler<CompositionScrollController, CompositionScrollControllerOffsetChangeCompletedEventArgs> OffsetChangeCompleted;

	public static bool s_IsDebugOutputEnabled = false;

	private const float SmallChangeAdditionalVelocity = 144.0f;
	private const float SmallChangeInertiaDecayRate = 0.975f;

	public enum AnimationType
	{
		Default,
		Accordion,
		Teleportation
	}

	private struct OperationInfo
	{
		public int CorrelationId;
		public double RelativeOffsetChange;
		public double OffsetTarget;

		public OperationInfo(int correlationId, double relativeOffsetChange, double offsetTarget) : this()
		{
			CorrelationId = correlationId;
			RelativeOffsetChange = relativeOffsetChange;
			OffsetTarget = offsetTarget;
		}
	}

	private List<string> lstAsyncEventMessage = new List<string>();
	private List<int> lstOffsetChangeCorrelationIds = new List<int>();
	private List<int> lstAddScrollVelocityCorrelationIds = new List<int>();
	private CompositionScrollControllerPanningInfo panningInfo = null;
	private Dictionary<int, OperationInfo> operations = new Dictionary<int, OperationInfo>();
	private FrameworkElement panningFrameworkElement = null;
	private UIElement horizontalGrid = null;
	private UIElement verticalGrid = null;
	private UIElement horizontalThumb = null;
	private UIElement verticalThumb = null;
	private RepeatButton horizontalDecrementRepeatButton = null;
	private RepeatButton verticalDecrementRepeatButton = null;
	private RepeatButton horizontalIncrementRepeatButton = null;
	private RepeatButton verticalIncrementRepeatButton = null;
	private Orientation orientation = Orientation.Vertical;
	private bool isScrollable = false;
	private double minOffset = 0.0;
	private double maxOffset = 0.0;
	private double offset = 0.0;
	private double offsetTarget = 0.0;
	private double viewportLength = 0.0;
	private double preManipulationThumbOffset = 0.0;

	public CompositionScrollController()
	{
		RaiseLogMessage("CompositionScrollController: CompositionScrollController()");

		this.DefaultStyleKey = typeof(CompositionScrollController);
		panningInfo = new CompositionScrollControllerPanningInfo(DispatcherQueue, true, orientation);
		panningInfo.LogMessage += PanningInfo_LogMessage;
		IsScrollingWithMouse = false;
		OffsetChangeAnimationType = AnimationType.Default;
		StockOffsetChangeDuration = TimeSpan.MinValue;
		OverriddenOffsetChangeDuration = TimeSpan.MinValue;
		IsEnabledChanged += CompositionScrollController_IsEnabledChanged;
		SizeChanged += CompositionScrollController_SizeChanged;
	}

	protected override void OnApplyTemplate()
	{
		RaiseLogMessage("CompositionScrollController: OnApplyTemplate()");

		UnhookHandlers();

		base.OnApplyTemplate();

		horizontalGrid = GetTemplateChild("HorizontalGrid") as UIElement;
		verticalGrid = GetTemplateChild("VerticalGrid") as UIElement;
		horizontalThumb = GetTemplateChild("HorizontalThumb") as UIElement;
		verticalThumb = GetTemplateChild("VerticalThumb") as UIElement;
		horizontalDecrementRepeatButton = GetTemplateChild("HorizontalDecrementRepeatButton") as RepeatButton;
		verticalDecrementRepeatButton = GetTemplateChild("VerticalDecrementRepeatButton") as RepeatButton;
		horizontalIncrementRepeatButton = GetTemplateChild("HorizontalIncrementRepeatButton") as RepeatButton;
		verticalIncrementRepeatButton = GetTemplateChild("VerticalIncrementRepeatButton") as RepeatButton;

		UpdateOrientation();
	}

	public AnimationType OffsetChangeAnimationType
	{
		get;
		set;
	}

	public TimeSpan StockOffsetChangeDuration
	{
		get;
		private set;
	}

	public TimeSpan OverriddenOffsetChangeDuration
	{
		get;
		set;
	}

	public bool IsAnimatingThumbOffset
	{
		get
		{
			return panningInfo.IsAnimatingThumbOffset;
		}
		set
		{
			RaiseLogMessage("CompositionScrollController: IsAnimatingThumbOffset(" + value + ")");

			panningInfo.IsAnimatingThumbOffset = value;
		}
	}

	public bool IsThumbPannable
	{
		get
		{
			return panningInfo.IsThumbPannable;
		}
		set
		{
			RaiseLogMessage("CompositionScrollController: IsThumbPannable(" + value + ")");

			panningInfo.IsThumbPannable = value;
		}
	}

	public bool IsThumbPositionMirrored
	{
		get
		{
			return panningInfo.IsThumbPositionMirrored;
		}
		set
		{
			RaiseLogMessage("CompositionScrollController: IsThumbPositionMirrored(" + value + ")");

			if (panningInfo.IsThumbPositionMirrored != value)
			{
				panningInfo.IsThumbPositionMirrored = value;

				UpdatePanningFrameworkElementOffset();
			}
		}
	}

	public Orientation Orientation
	{
		get
		{
			return orientation;
		}
		set
		{
			RaiseLogMessage("CompositionScrollController: Orientation(" + value + ")");

			if (orientation != value)
			{
				UnhookHandlers();
				if (IsAnimatingThumbOffset)
				{
					panningInfo.StopThumbAnimation();
				}
				orientation = value;
				UpdateOrientation();
			}
		}
	}

	public void SetIsScrollable(bool isScrollable)
	{
		RaiseLogMessage(
			"CompositionScrollController: SetIsScrollable for Orientation=" + Orientation +
			" with isScrollable=" + isScrollable);
		this.isScrollable = isScrollable;
		// CanScroll's evaluation depends on isScrollable.
		UpdateCanScroll();
	}

	public void SetValues(double minOffset, double maxOffset, double offset, double viewportLength)
	{
		RaiseLogMessage(
			"CompositionScrollController: SetValues for Orientation=" + Orientation +
			" with minOffset=" + minOffset +
			", maxOffset=" + maxOffset +
			", offset=" + offset +
			", viewportLength=" + viewportLength);

		if (maxOffset < minOffset)
		{
			throw new ArgumentOutOfRangeException("maxOffset");
		}

		if (viewportLength < 0.0)
		{
			throw new ArgumentOutOfRangeException("viewportLength");
		}

		offset = Math.Max(minOffset, offset);
		offset = Math.Min(maxOffset, offset);

		if (operations.Count == 0)
		{
			offsetTarget = offset;
		}
		else
		{
			offsetTarget = Math.Max(minOffset, offsetTarget);
			offsetTarget = Math.Min(maxOffset, offsetTarget);
		}

		bool updatePanningFrameworkElementLength =
			this.minOffset != minOffset ||
			this.maxOffset != maxOffset ||
			this.viewportLength != viewportLength;

		this.minOffset = minOffset;
		this.offset = offset;
		this.maxOffset = maxOffset;
		this.viewportLength = viewportLength;

		if (updatePanningFrameworkElementLength && !panningInfo.UpdatePanningFrameworkElementLength(minOffset, maxOffset, viewportLength))
		{
			panningInfo.UpdatePanningElementOffsetMultiplier();
		}

		UpdatePanningFrameworkElementOffset();

		// Potentially changed minOffset / maxOffset value(s) may have an effect
		// on the read-only IScrollController.CanScroll property.
		UpdateCanScroll();
	}

	// Builds a custom Vector3KeyFrameAnimation animation that the ScrollPresenter applies when an animated offset change
	// is requested by the CompositionScrollController. 
	public CompositionAnimation GetScrollAnimation(
		int correlationId,
		Vector2 startPosition,
		Vector2 endPosition,
		CompositionAnimation defaultAnimation)
	{
		RaiseLogMessage(
			"CompositionScrollController: GetScrollAnimation for Orientation=" + Orientation +
			" with OffsetsChangeCorrelationId=" + correlationId + ", startPosition=" + startPosition + ", endPosition=" + endPosition);

		try
		{
			Vector3KeyFrameAnimation stockKeyFrameAnimation = defaultAnimation as Vector3KeyFrameAnimation;

			if (stockKeyFrameAnimation != null)
			{
				Vector3KeyFrameAnimation customKeyFrameAnimation = stockKeyFrameAnimation;

				if (OffsetChangeAnimationType != AnimationType.Default)
				{
					OperationInfo oi = operations[correlationId];
					float positionTarget = (float)oi.OffsetTarget;
					float otherOrientationPosition = orientation == Orientation.Horizontal ? startPosition.Y : startPosition.X;

					customKeyFrameAnimation = stockKeyFrameAnimation.Compositor.CreateVector3KeyFrameAnimation();
					if (OffsetChangeAnimationType == AnimationType.Accordion)
					{
						float deltaPosition = 0.1f * (float)(positionTarget - offset);

						if (orientation == Orientation.Horizontal)
						{
							for (int step = 0; step < 3; step++)
							{
								customKeyFrameAnimation.InsertKeyFrame(
									1.0f - (0.4f / (float)Math.Pow(2, step)),
									new Vector3(positionTarget + deltaPosition, otherOrientationPosition, 0.0f));
								deltaPosition /= -2.0f;
							}

							customKeyFrameAnimation.InsertKeyFrame(1.0f, new Vector3(positionTarget, otherOrientationPosition, 0.0f));
						}
						else
						{
							for (int step = 0; step < 3; step++)
							{
								customKeyFrameAnimation.InsertKeyFrame(
									1.0f - (0.4f / (float)Math.Pow(2, step)),
									new Vector3(otherOrientationPosition, positionTarget + deltaPosition, 0.0f));
								deltaPosition /= -2.0f;
							}

							customKeyFrameAnimation.InsertKeyFrame(1.0f, new Vector3(otherOrientationPosition, positionTarget, 0.0f));
						}
					}
					else
					{
						// Teleportation case
						float deltaPosition = (float)(positionTarget - offset);

						CubicBezierEasingFunction cubicBezierStart = stockKeyFrameAnimation.Compositor.CreateCubicBezierEasingFunction(
							new Vector2(1.0f, 0.0f),
							new Vector2(1.0f, 0.0f));

						StepEasingFunction step = stockKeyFrameAnimation.Compositor.CreateStepEasingFunction(1);

						CubicBezierEasingFunction cubicBezierEnd = stockKeyFrameAnimation.Compositor.CreateCubicBezierEasingFunction(
							new Vector2(0.0f, 1.0f),
							new Vector2(0.0f, 1.0f));

						if (orientation == Orientation.Horizontal)
						{
							customKeyFrameAnimation.InsertKeyFrame(
								0.499999f,
								new Vector3(positionTarget - 0.9f * deltaPosition, otherOrientationPosition, 0.0f),
								cubicBezierStart);
							customKeyFrameAnimation.InsertKeyFrame(
								0.5f,
								new Vector3(positionTarget - 0.1f * deltaPosition, otherOrientationPosition, 0.0f),
								step);
							customKeyFrameAnimation.InsertKeyFrame(
								1.0f,
								new Vector3(positionTarget, otherOrientationPosition, 0.0f),
								cubicBezierEnd);
						}
						else
						{
							customKeyFrameAnimation.InsertKeyFrame(
								0.499999f,
								new Vector3(otherOrientationPosition, positionTarget - 0.9f * deltaPosition, 0.0f),
								cubicBezierStart);
							customKeyFrameAnimation.InsertKeyFrame(
								0.5f,
								new Vector3(otherOrientationPosition, positionTarget - 0.1f * deltaPosition, 0.0f),
								step);
							customKeyFrameAnimation.InsertKeyFrame(
								1.0f,
								new Vector3(otherOrientationPosition, positionTarget, 0.0f),
								cubicBezierEnd);
						}
					}
					customKeyFrameAnimation.Duration = stockKeyFrameAnimation.Duration;
				}

				if (OverriddenOffsetChangeDuration != TimeSpan.MinValue)
				{
					StockOffsetChangeDuration = stockKeyFrameAnimation.Duration;
					customKeyFrameAnimation.Duration = OverriddenOffsetChangeDuration;
				}

				return customKeyFrameAnimation;
			}
		}
		catch (Exception ex)
		{
			RaiseLogMessage("CompositionScrollController: GetScrollAnimation exception=" + ex);
		}

		return null;
	}

	public void NotifyRequestedScrollCompleted(
		int correlationId)
	{
		int offsetChangeCorrelationId = correlationId;

		RaiseLogMessage(
			"CompositionScrollController: NotifyRequestedScrollCompleted for Orientation=" + Orientation +
			" with offsetChangeCorrelationId=" + offsetChangeCorrelationId);

		if (lstOffsetChangeCorrelationIds.Contains(offsetChangeCorrelationId))
		{
			lstOffsetChangeCorrelationIds.Remove(offsetChangeCorrelationId);

			double relativeOffsetChange = 0.0;

			if (operations.ContainsKey(offsetChangeCorrelationId))
			{
				OperationInfo oi = operations[offsetChangeCorrelationId];
				relativeOffsetChange = oi.RelativeOffsetChange;
				operations.Remove(offsetChangeCorrelationId);
			}

			RaiseLogMessage("CompositionScrollController: ScrollTo/By completed. CorrelationId=" + offsetChangeCorrelationId);
		}
		else if (lstAddScrollVelocityCorrelationIds.Contains(offsetChangeCorrelationId))
		{
			lstAddScrollVelocityCorrelationIds.Add(offsetChangeCorrelationId);

			RaiseLogMessage("CompositionScrollController: AddScrollVelocityRequest completed. CorrelationId=" + offsetChangeCorrelationId);
		}

		if (OffsetChangeCompleted != null)
		{
			RaiseLogMessage(
				"CompositionScrollController: NotifyRequestedScrollCompleted raising OffsetChangeCompleted event.");
			OffsetChangeCompleted(this, new CompositionScrollControllerOffsetChangeCompletedEventArgs(offsetChangeCorrelationId));
		}
	}

	public IScrollControllerPanningInfo PanningInfo
	{
		get
		{
			// Returning non-null instance because this IScrollController implementation 
			// supports UI-thread-independent panning with touch or pen.
			return panningInfo;
		}
	}

	public bool CanScroll
	{
		get;
		private set;
	}

	public bool IsScrollingWithMouse
	{
		get;
		private set;
	}

	public event TypedEventHandler<CompositionScrollController, string> LogMessage;

	public event TypedEventHandler<IScrollController, Object> CanScrollChanged;
	public event TypedEventHandler<IScrollController, Object> IsScrollingWithMouseChanged;
	public event TypedEventHandler<IScrollController, ScrollControllerScrollToRequestedEventArgs> ScrollToRequested;
	public event TypedEventHandler<IScrollController, ScrollControllerScrollByRequestedEventArgs> ScrollByRequested;
	public event TypedEventHandler<IScrollController, ScrollControllerAddScrollVelocityRequestedEventArgs> AddScrollVelocityRequested;

	public int ScrollTo(
		double offset, ScrollingAnimationMode animationMode)
	{
		RaiseLogMessage("CompositionScrollController: ScrollTo for Orientation=" + Orientation + " with offset=" + offset + ", animationMode=" + animationMode);

		return RaiseScrollToRequested(
			offset, animationMode, false /*hookupCompletion*/);
	}

	public int ScrollBy(
		double offsetDelta, ScrollingAnimationMode animationMode)
	{
		RaiseLogMessage("CompositionScrollController: ScrollBy for Orientation=" + Orientation + " with offsetDelta=" + offsetDelta + ", animationMode=" + animationMode);

		return RaiseScrollByRequested(
			offsetDelta, animationMode, false /*hookupCompletion*/);
	}

	public int AddScrollVelocity(
		float offsetVelocity, float? inertiaDecayRate)
	{
		RaiseLogMessage("CompositionScrollController: AddScrollVelocity for Orientation=" + Orientation + " with offsetVelocity=" + offsetVelocity + ", inertiaDecayRate=" + inertiaDecayRate);

		return RaiseAddScrollVelocityRequested(
			offsetVelocity, inertiaDecayRate, false /*hookupCompletion*/);
	}

	private void PanningInfo_LogMessage(IScrollControllerPanningInfo sender, string message)
	{
		RaiseLogMessage(message);
	}

	private void RaiseLogMessage(string message)
	{
		if (s_IsDebugOutputEnabled)
			System.Diagnostics.Debug.WriteLine(message);

		if (LogMessage != null)
		{
			LogMessage(this, message);
		}
	}

	private void RaiseCanScrollChanged()
	{
		RaiseLogMessage("CompositionScrollController: RaiseCanScrollChanged for Orientation=" + Orientation);
		if (CanScrollChanged != null)
		{
			CanScrollChanged(this, null);
		}
	}

	private void RaiseIsScrollingWithMouseChanged()
	{
		RaiseLogMessage("CompositionScrollController: RaiseIsScrollingWithMouseChanged for Orientation=" + Orientation);
		if (IsScrollingWithMouseChanged != null)
		{
			IsScrollingWithMouseChanged(this, null);
		}
	}

	private int RaiseScrollToRequested(
		double offset,
		ScrollingAnimationMode animationMode,
		bool hookupCompletion)
	{
		RaiseLogMessage("CompositionScrollController: RaiseScrollToRequested for Orientation=" + Orientation + " with offset=" + offset + ", animationMode=" + animationMode);

		try
		{
			if (ScrollToRequested != null)
			{
				ScrollControllerScrollToRequestedEventArgs e =
					new ScrollControllerScrollToRequestedEventArgs(
						offset,
						new ScrollingScrollOptions(animationMode, ScrollingSnapPointsMode.Ignore));
				ScrollToRequested(this, e);
				if (e.CorrelationId != -1)
				{
					RaiseLogMessage("CompositionScrollController: ScrollToRequest started. OffsetsChangeCorrelationId=" + e.CorrelationId);

					if (hookupCompletion && !lstOffsetChangeCorrelationIds.Contains(e.CorrelationId))
					{
						lstOffsetChangeCorrelationIds.Add(e.CorrelationId);
					}
				}
				return e.CorrelationId;
			}
		}
		catch (Exception ex)
		{
			RaiseLogMessage("CompositionScrollController: RaiseScrollToRequested - exception=" + ex);
		}

		return -1;
	}

	private int RaiseScrollByRequested(
		double offsetDelta,
		ScrollingAnimationMode animationMode,
		bool hookupCompletion)
	{
		RaiseLogMessage("CompositionScrollController: RaiseScrollByRequested for Orientation=" + Orientation + " with offsetDelta=" + offsetDelta + ", animationMode=" + animationMode);

		try
		{
			if (ScrollByRequested != null)
			{
				ScrollControllerScrollByRequestedEventArgs e =
					new ScrollControllerScrollByRequestedEventArgs(
						offsetDelta,
						new ScrollingScrollOptions(animationMode, ScrollingSnapPointsMode.Ignore));
				ScrollByRequested(this, e);
				if (e.CorrelationId != -1)
				{
					RaiseLogMessage("CompositionScrollController: ScrollByRequest started. OffsetsChangeCorrelationId=" + e.CorrelationId);

					if (hookupCompletion && !lstOffsetChangeCorrelationIds.Contains(e.CorrelationId))
					{
						lstOffsetChangeCorrelationIds.Add(e.CorrelationId);
					}
				}
				return e.CorrelationId;
			}
		}
		catch (Exception ex)
		{
			RaiseLogMessage("CompositionScrollController: RaiseScrollByRequested - exception=" + ex);
		}

		return -1;
	}

	private int RaiseAddScrollVelocityRequested(
		float offsetVelocity, float? inertiaDecayRate, bool hookupCompletion)
	{
		RaiseLogMessage("CompositionScrollController: RaiseAddScrollVelocityRequested for Orientation=" + Orientation + " with offsetVelocity=" + offsetVelocity + ", inertiaDecayRate=" + inertiaDecayRate);

		if (AddScrollVelocityRequested != null)
		{
			ScrollControllerAddScrollVelocityRequestedEventArgs e =
				new ScrollControllerAddScrollVelocityRequestedEventArgs(
					offsetVelocity,
					inertiaDecayRate);
			AddScrollVelocityRequested(this, e);
			if (e.CorrelationId != -1)
			{
				RaiseLogMessage("CompositionScrollController: AddScrollVelocityRequest started. OffsetsChangeCorrelationId=" + e.CorrelationId);

				if (hookupCompletion && !lstAddScrollVelocityCorrelationIds.Contains(e.CorrelationId))
				{
					lstAddScrollVelocityCorrelationIds.Add(e.CorrelationId);
				}
			}
			return e.CorrelationId;
		}
		return -1;
	}

	private void PanningFrameworkElement_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		Point pt = e.GetCurrentPoint(sender as UIElement).Position;
		RaiseLogMessage("CompositionScrollController: PanningFrameworkElement_PointerPressed for Orientation=" + Orientation + ", position=" + pt);

		if (!CanScroll)
			return;

		switch (e.Pointer.PointerDeviceType)
		{
			case Microsoft.UI.Input.PointerDeviceType.Touch:
			case Microsoft.UI.Input.PointerDeviceType.Pen:
				// Attempt an UI-thread-independent pan.
				panningInfo.RaisePanRequested(e.GetCurrentPoint(null));
				break;
			case Microsoft.UI.Input.PointerDeviceType.Mouse:
				if (!IsScrollingWithMouse)
				{
					// Starting a mouse-driven thumb drag.
					IsScrollingWithMouse = true;
					RaiseIsScrollingWithMouseChanged();
				}
				break;
		}
	}

	private void PanningFrameworkElement_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
	{
		if (IsScrollingWithMouse)
		{
			preManipulationThumbOffset = Orientation == Orientation.Horizontal ? HorizontalThumbOffset : VerticalThumbOffset;
		}
	}

	private void PanningFrameworkElement_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
	{
		if (IsScrollingWithMouse)
		{
			double targetThumbOffset = preManipulationThumbOffset + (Orientation == Orientation.Horizontal ? e.Cumulative.Translation.X : e.Cumulative.Translation.Y);
			double scrollPresenterOffset = ScrollPresenterOffsetFromThumbOffset(targetThumbOffset);

			int offsetChangeCorrelationId = RaiseScrollToRequested(
				scrollPresenterOffset, ScrollingAnimationMode.Disabled, true /*hookupCompletion*/);
		}
	}

	private void PanningFrameworkElement_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	{
		if (IsScrollingWithMouse)
		{
			// Ending a mouse-driven thumb drag.
			IsScrollingWithMouse = false;
			RaiseIsScrollingWithMouseChanged();
		}
	}

	private void UnhookHandlers()
	{
		if (horizontalDecrementRepeatButton != null)
		{
			horizontalDecrementRepeatButton.Click -= DecrementRepeatButton_Click;
		}

		if (horizontalIncrementRepeatButton != null)
		{
			horizontalIncrementRepeatButton.Click -= IncrementRepeatButton_Click;
		}

		if (verticalDecrementRepeatButton != null)
		{
			verticalDecrementRepeatButton.Click -= DecrementRepeatButton_Click;
		}

		if (verticalIncrementRepeatButton != null)
		{
			verticalIncrementRepeatButton.Click -= IncrementRepeatButton_Click;
		}

		if (panningFrameworkElement != null)
		{
			panningFrameworkElement.PointerPressed -= PanningFrameworkElement_PointerPressed;
			panningFrameworkElement.ManipulationStarting -= PanningFrameworkElement_ManipulationStarting;
			panningFrameworkElement.ManipulationDelta -= PanningFrameworkElement_ManipulationDelta;
			panningFrameworkElement.ManipulationCompleted -= PanningFrameworkElement_ManipulationCompleted;

			FrameworkElement parent = panningFrameworkElement.Parent as FrameworkElement;
			if (parent != null)
			{
				parent.PointerPressed -= Parent_PointerPressed;
			}
		}
	}

	private bool UpdateCanScroll()
	{
		bool oldCanScroll = CanScroll;

		CanScroll =
			maxOffset > minOffset && isScrollable && IsEnabled;

		if (oldCanScroll != CanScroll)
		{
			RaiseCanScrollChanged();
			return true;
		}
		return false;
	}

	private void UpdateOrientation()
	{
		panningFrameworkElement = null;

		if (Orientation == Orientation.Horizontal)
		{
			if (horizontalGrid != null)
				horizontalGrid.Visibility = Visibility.Visible;
			if (verticalGrid != null)
				verticalGrid.Visibility = Visibility.Collapsed;
			panningFrameworkElement = horizontalThumb as FrameworkElement;
			if (panningFrameworkElement != null)
			{
				panningFrameworkElement.ManipulationMode = ManipulationModes.TranslateX;
			}

			if (horizontalDecrementRepeatButton != null)
			{
				horizontalDecrementRepeatButton.Click += DecrementRepeatButton_Click;
			}

			if (horizontalIncrementRepeatButton != null)
			{
				horizontalIncrementRepeatButton.Click += IncrementRepeatButton_Click;
			}
		}
		else
		{
			if (verticalGrid != null)
				verticalGrid.Visibility = Visibility.Visible;
			if (horizontalGrid != null)
				horizontalGrid.Visibility = Visibility.Collapsed;
			panningFrameworkElement = verticalThumb as FrameworkElement;
			if (panningFrameworkElement != null)
			{
				panningFrameworkElement.ManipulationMode = ManipulationModes.TranslateY;
			}

			if (verticalDecrementRepeatButton != null)
			{
				verticalDecrementRepeatButton.Click += DecrementRepeatButton_Click;
			}

			if (verticalIncrementRepeatButton != null)
			{
				verticalIncrementRepeatButton.Click += IncrementRepeatButton_Click;
			}
		}

		panningInfo.PanOrientation = Orientation;

		if (panningFrameworkElement != null)
		{
			// Setting up the pannable thumb for mouse-driven drags.
			panningFrameworkElement.PointerPressed += PanningFrameworkElement_PointerPressed;
			panningFrameworkElement.ManipulationStarting += PanningFrameworkElement_ManipulationStarting;
			panningFrameworkElement.ManipulationDelta += PanningFrameworkElement_ManipulationDelta;
			panningFrameworkElement.ManipulationCompleted += PanningFrameworkElement_ManipulationCompleted;

			FrameworkElement parent = panningFrameworkElement.Parent as FrameworkElement;

			if (parent != null)
			{
				parent.PointerPressed += Parent_PointerPressed;
				panningInfo.SetPanningFrameworkElement(panningFrameworkElement);
				panningInfo.StartThumbAnimation();
			}
			else
			{
				panningInfo.SetPanningFrameworkElement(null);
			}
		}
		else
		{
			panningInfo.SetPanningFrameworkElement(null);
		}
	}

	private void UpdatePanningFrameworkElementOffset()
	{
		Visual panningVisual = panningInfo.PanningVisual;

		if (!IsAnimatingThumbOffset && panningVisual != null)
		{
			if (orientation == Orientation.Horizontal)
			{
				float horizontalThumbOffset = (float)HorizontalThumbOffset;
				Vector3 currentHorizontalThumbOffset;

				panningVisual.Properties.TryGetVector3("Translation", out currentHorizontalThumbOffset);
				if (currentHorizontalThumbOffset.X != horizontalThumbOffset)
				{
					RaiseLogMessage("CompositionScrollController: UpdatePanningFrameworkElementOffset for Orientation=Horizontal, HorizontalThumbOffset=" + horizontalThumbOffset);
					panningVisual.Properties.InsertVector3("Translation", new Vector3(horizontalThumbOffset, 0.0f, 0.0f));
				}
			}
			else
			{
				float verticalThumbOffset = (float)VerticalThumbOffset;
				Vector3 currentVerticalThumbOffset;

				panningVisual.Properties.TryGetVector3("Translation", out currentVerticalThumbOffset);
				if (currentVerticalThumbOffset.Y != verticalThumbOffset)
				{
					RaiseLogMessage("CompositionScrollController: UpdatePanningFrameworkElementOffset for Orientation=Vertical, VerticalThumbOffset=" + verticalThumbOffset);
					panningVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, verticalThumbOffset, 0.0f));
				}
			}
		}
		else if (!IsThumbPannable && panningFrameworkElement != null)
		{
			Thickness margin;

			if (orientation == Orientation.Horizontal)
			{
				if (IsThumbPositionMirrored)
				{
					margin = new Thickness((maxOffset - offset) / panningInfo.PanningElementOffsetMultiplier, 0, 0, 0);
				}
				else
				{
					margin = new Thickness((minOffset - offset) / panningInfo.PanningElementOffsetMultiplier, 0, 0, 0);
				}
			}
			else
			{
				if (IsThumbPositionMirrored)
				{
					margin = new Thickness(0, (maxOffset - offset) / panningInfo.PanningElementOffsetMultiplier, 0, 0);
				}
				else
				{
					margin = new Thickness(0, (minOffset - offset) / panningInfo.PanningElementOffsetMultiplier, 0, 0);
				}
			}

			panningFrameworkElement.Margin = margin;
		}
	}

	private void DecrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("CompositionScrollController: DecrementRepeatButton_Click for Orientation=" + Orientation);

		if (!CanScroll)
			return;

		int offsetChangeCorrelationId =
			RaiseAddScrollVelocityRequested(IsThumbPositionMirrored ? SmallChangeAdditionalVelocity : -SmallChangeAdditionalVelocity, SmallChangeInertiaDecayRate, true /*hookupCompletion*/);
	}

	private void IncrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("CompositionScrollController: IncrementRepeatButton_Click for Orientation=" + Orientation);

		if (!CanScroll)
			return;

		int offsetChangeCorrelationId =
			RaiseAddScrollVelocityRequested(IsThumbPositionMirrored ? -SmallChangeAdditionalVelocity : SmallChangeAdditionalVelocity, SmallChangeInertiaDecayRate, true /*hookupCompletion*/);
	}

	// Returns the thumb offset within the track based on values provided in SetValues(...),
	// when the Orientation is horizontal.
	private double HorizontalThumbOffset
	{
		get
		{
			if (panningFrameworkElement != null)
			{
				double parentWidth = 0.0;
				FrameworkElement parent = panningFrameworkElement.Parent as FrameworkElement;
				if (parent != null)
				{
					parentWidth = parent.ActualWidth;
				}
				if (maxOffset != minOffset)
				{
					if (IsThumbPositionMirrored)
					{
						return (maxOffset - offset) / (maxOffset - minOffset) * (parentWidth - panningFrameworkElement.Width);
					}
					else
					{
						return (offset - minOffset) / (maxOffset - minOffset) * (parentWidth - panningFrameworkElement.Width);
					}
				}
			}

			return 0.0;
		}
	}

	// Returns the thumb offset within the track based on values provided in SetValues(...),
	// when the Orientation is vertical.
	private double VerticalThumbOffset
	{
		get
		{
			if (panningFrameworkElement != null)
			{
				double parentHeight = 0.0;
				FrameworkElement parent = panningFrameworkElement.Parent as FrameworkElement;
				if (parent != null)
				{
					parentHeight = parent.ActualHeight;
				}
				if (maxOffset != minOffset)
				{
					if (IsThumbPositionMirrored)
					{
						return (maxOffset - offset) / (maxOffset - minOffset) * (parentHeight - panningFrameworkElement.Height);
					}
					else
					{
						return (offset - minOffset) / (maxOffset - minOffset) * (parentHeight - panningFrameworkElement.Height);
					}
				}
			}

			return 0.0;
		}
	}

	private double ScrollPresenterOffsetFromThumbOffset(double thumbOffset)
	{
		double scrollPresenterOffset = 0.0;

		if (panningFrameworkElement != null)
		{
			double parentLength = 0.0;
			double panningFrameworkElementLength =
				Orientation == Orientation.Horizontal ? panningFrameworkElement.ActualWidth : panningFrameworkElement.ActualHeight;
			FrameworkElement parent = panningFrameworkElement.Parent as FrameworkElement;
			if (parent != null)
			{
				parentLength = Orientation == Orientation.Horizontal ? parent.ActualWidth : parent.ActualHeight;
			}
			if (parentLength != panningFrameworkElementLength)
			{
				if (IsThumbPositionMirrored)
				{
					scrollPresenterOffset = (parentLength - panningFrameworkElementLength - thumbOffset) * (maxOffset - minOffset) / (parentLength - panningFrameworkElementLength);
				}
				else
				{
					scrollPresenterOffset = thumbOffset * (maxOffset - minOffset) / (parentLength - panningFrameworkElementLength);
				}
			}
		}

		return scrollPresenterOffset;
	}

	private void CompositionScrollController_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		panningInfo.UpdatePanningFrameworkElementLength(minOffset, maxOffset, viewportLength);
		UpdatePanningFrameworkElementOffset();
	}

	private void CompositionScrollController_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		RaiseLogMessage("CompositionScrollController: IsEnabledChanged for Orientation=" + Orientation + ", IsEnabled=" + IsEnabled);

		// CanScroll's evaluation depends on IsEnabled.
		UpdateCanScroll();
	}

	private void Parent_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		Point pt = e.GetCurrentPoint(sender as UIElement).Position;
		RaiseLogMessage("CompositionScrollController: Parent_PointerPressed for Orientation=" + Orientation + ", position=" + pt);

		if (!CanScroll)
			return;

		double relativeOffsetChange = 0.0;
		double newOffsetTarget = 0.0;

		if (Orientation == Orientation.Horizontal)
		{
			if (pt.X < HorizontalThumbOffset)
			{
				if (IsThumbPositionMirrored)
					relativeOffsetChange = viewportLength;
				else
					relativeOffsetChange = -viewportLength;
			}
			else if (pt.X > HorizontalThumbOffset + panningFrameworkElement.ActualWidth)
			{
				if (IsThumbPositionMirrored)
					relativeOffsetChange = -viewportLength;
				else
					relativeOffsetChange = viewportLength;
			}
			else
			{
				return;
			}
		}
		else
		{
			if (pt.Y < VerticalThumbOffset)
			{
				if (IsThumbPositionMirrored)
					relativeOffsetChange = viewportLength;
				else
					relativeOffsetChange = -viewportLength;
			}
			else if (pt.Y > VerticalThumbOffset + panningFrameworkElement.ActualHeight)
			{
				if (IsThumbPositionMirrored)
					relativeOffsetChange = -viewportLength;
				else
					relativeOffsetChange = viewportLength;
			}
			else
			{
				return;
			}
		}

		newOffsetTarget = offsetTarget + relativeOffsetChange;
		newOffsetTarget = Math.Max(minOffset, newOffsetTarget);
		newOffsetTarget = Math.Min(maxOffset, newOffsetTarget);
		relativeOffsetChange = newOffsetTarget - offsetTarget;
		offsetTarget = newOffsetTarget;

		int offsetChangeCorrelationId = RaiseScrollToRequested(
			offsetTarget, ScrollingAnimationMode.Auto, true /*hookupCompletion*/);
		if (offsetChangeCorrelationId != -1 && !operations.ContainsKey(offsetChangeCorrelationId))
		{
			operations.Add(offsetChangeCorrelationId, new OperationInfo(offsetChangeCorrelationId, relativeOffsetChange, offsetTarget));
		}
	}
}
