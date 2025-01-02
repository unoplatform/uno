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

public class BiDirectionalScrollControllerScrollingScrollCompletedEventArgs
{
	internal BiDirectionalScrollControllerScrollingScrollCompletedEventArgs(int offsetsChangeCorrelationId)
	{
		OffsetsChangeCorrelationId = offsetsChangeCorrelationId;
	}

	public int OffsetsChangeCorrelationId
	{
		get;
		set;
	}
}

public sealed partial class BiDirectionalScrollController : ContentControl
{
	private class UniScrollControllerScrollingScrollCompletedEventArgs
	{
		public UniScrollControllerScrollingScrollCompletedEventArgs(int offsetChangeCorrelationId)
		{
			OffsetChangeCorrelationId = offsetChangeCorrelationId;
		}

		public int OffsetChangeCorrelationId
		{
			get;
			set;
		}
	}

	private class UniScrollController : IScrollController
	{
		public event TypedEventHandler<IScrollController, object> CanScrollChanged;
		public event TypedEventHandler<IScrollController, object> IsScrollingWithMouseChanged;
		public event TypedEventHandler<IScrollController, ScrollControllerScrollToRequestedEventArgs> ScrollToRequested;
		public event TypedEventHandler<IScrollController, ScrollControllerScrollByRequestedEventArgs> ScrollByRequested;
		public event TypedEventHandler<IScrollController, ScrollControllerAddScrollVelocityRequestedEventArgs> AddScrollVelocityRequested;

		public event TypedEventHandler<UniScrollController, UniScrollControllerScrollingScrollCompletedEventArgs> ScrollCompleted;

		private CompositionScrollControllerPanningInfo panningInfo = null;

		public UniScrollController(BiDirectionalScrollController owner, Orientation orientation)
		{
			panningInfo = new CompositionScrollControllerPanningInfo(owner.DispatcherQueue, owner.IsRailEnabled, orientation);

			Owner = owner;
			Orientation = orientation;
			MinOffset = 0.0;
			MaxOffset = 0.0;
			Offset = 0.0;
			ViewportLength = 0.0;
		}

		public IScrollControllerPanningInfo PanningInfo
		{
			get
			{
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
			get
			{
				RaiseLogMessage("UniScrollController: get_IsScrollingWithMouse for Orientation=" + Orientation);
				return Owner.IsScrollingWithMouse;
			}
		}

		internal double MinOffset
		{
			get;
			private set;
		}

		internal double MaxOffset
		{
			get;
			private set;
		}

		internal double Offset
		{
			get;
			private set;
		}

		internal double ViewportLength
		{
			get;
			private set;
		}

		internal bool IsRailEnabled
		{
			get
			{
				return panningInfo.IsRailEnabled;
			}
			set
			{
				panningInfo.IsRailEnabled = value;
			}
		}

		private bool IsScrollable
		{
			get;
			set;
		}

		private Orientation Orientation
		{
			get;
			set;
		}

		private BiDirectionalScrollController Owner
		{
			get;
			set;
		}

		public void SetIsScrollable(bool isScrollable)
		{
			RaiseLogMessage(
				"UniScrollController: SetIsScrollable for Orientation=" + Orientation +
				" with isScrollable=" + isScrollable);
			IsScrollable = isScrollable;
			UpdateCanScroll();
		}

		public void SetValues(double minOffset, double maxOffset, double offset, double viewportLength)
		{
			RaiseLogMessage(
				"UniScrollController: SetValues for Orientation=" + Orientation +
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

			double offsetTarget = 0.0;

			if (Owner.OperationsCount == 0)
			{
				offsetTarget = offset;
			}
			else
			{
				offsetTarget = Math.Max(minOffset, offsetTarget);
				offsetTarget = Math.Min(maxOffset, offsetTarget);
			}

			if (Orientation == Orientation.Horizontal)
				Owner.OffsetTarget = new Point(offsetTarget, Owner.OffsetTarget.Y);
			else
				Owner.OffsetTarget = new Point(Owner.OffsetTarget.X, offsetTarget);

			bool updatePanningFrameworkElementLength =
				MinOffset != minOffset ||
				MaxOffset != maxOffset ||
				ViewportLength != viewportLength;

			MinOffset = minOffset;
			Offset = offset;
			MaxOffset = maxOffset;
			ViewportLength = viewportLength;

			if (updatePanningFrameworkElementLength && !panningInfo.UpdatePanningFrameworkElementLength(minOffset, maxOffset, viewportLength))
			{
				panningInfo.UpdatePanningElementOffsetMultiplier();
			}

			// Potentially changed MinOffset / MaxOffset value(s) may have an effect
			// on the read-only IScrollController.CanScroll property.
			UpdateCanScroll();
		}

		public CompositionAnimation GetScrollAnimation(
			int correlationId,
			Vector2 startPosition,
			Vector2 endPosition,
			CompositionAnimation defaultAnimation)
		{
			RaiseLogMessage(
				"UniScrollController: GetScrollAnimation for Orientation=" + Orientation +
				" with OffsetsChangeCorrelationId=" + correlationId + ", startPosition=" + startPosition + ", endPosition=" + endPosition);
			return defaultAnimation;
		}

		public void NotifyRequestedScrollCompleted(
			int correlationId)
		{
			RaiseLogMessage(
				"UniScrollController: NotifyRequestedScrollCompleted for Orientation=" + Orientation +
				" with OffsetsChangeCorrelationId=" + correlationId);

			ScrollCompleted?.Invoke(this, new UniScrollControllerScrollingScrollCompletedEventArgs(correlationId));
		}

		internal void SetPanningFrameworkElement(FrameworkElement value)
		{
			panningInfo.SetPanningFrameworkElement(value);
		}

		internal void UpdateCanScroll()
		{
			bool oldCanScroll = CanScroll;

			CanScroll =
				MaxOffset > MinOffset && IsScrollable && Owner.IsEnabled;

			if (oldCanScroll != CanScroll)
			{
				RaiseCanScrollChanged();
			}
		}

		internal void UpdatePanningFrameworkElementLength()
		{
			panningInfo.UpdatePanningFrameworkElementLength(MinOffset, MaxOffset, ViewportLength);
		}

		internal void RaiseCanScrollChanged()
		{
			RaiseLogMessage("UniScrollController: RaiseCanScrollChanged for Orientation=" + Orientation);

			if (CanScrollChanged != null)
			{
				CanScrollChanged(this, null);
			}
		}

		internal void RaiseIsScrollingWithMouseChanged()
		{
			RaiseLogMessage("UniScrollController: RaiseIsScrollingWithMouseChanged for Orientation=" + Orientation);

			if (IsScrollingWithMouseChanged != null)
			{
				IsScrollingWithMouseChanged(this, null);
			}
		}

		internal bool RaisePanRequested(PointerPoint pointerPoint)
		{
			RaiseLogMessage("UniScrollController: RaisePanRequested for Orientation=" + Orientation + " with pointerPoint=" + pointerPoint);

			return panningInfo.RaisePanRequested(pointerPoint);
		}

		internal int RaiseScrollToRequested(
			double offset,
			ScrollingAnimationMode animationMode)
		{
			RaiseLogMessage("UniScrollController: RaiseScrollToRequested for Orientation=" + Orientation + " with offset=" + offset + ", animationMode=" + animationMode);

			if (ScrollToRequested != null)
			{
				ScrollControllerScrollToRequestedEventArgs e =
					new ScrollControllerScrollToRequestedEventArgs(
						offset,
						new ScrollingScrollOptions(animationMode, ScrollingSnapPointsMode.Ignore));
				ScrollToRequested(this, e);
				return e.CorrelationId;
			}
			return -1;
		}

		internal int RaiseScrollByRequested(
			double offsetDelta,
			ScrollingAnimationMode animationMode)
		{
			RaiseLogMessage("UniScrollController: RaiseScrollByRequested for Orientation=" + Orientation + " with offsetDelta=" + offsetDelta + ", animationMode=" + animationMode);

			if (ScrollByRequested != null)
			{
				ScrollControllerScrollByRequestedEventArgs e =
					new ScrollControllerScrollByRequestedEventArgs(
						offsetDelta,
						new ScrollingScrollOptions(animationMode, ScrollingSnapPointsMode.Ignore));
				ScrollByRequested(this, e);
				return e.CorrelationId;
			}
			return -1;
		}

		internal int RaiseAddScrollVelocityRequested(
			float offsetVelocity, float? inertiaDecayRate)
		{
			RaiseLogMessage("UniScrollController: RaiseAddScrollVelocityRequested for Orientation=" + Orientation + " with offsetVelocity=" + offsetVelocity + ", inertiaDecayRate=" + inertiaDecayRate);

			if (AddScrollVelocityRequested != null)
			{
				ScrollControllerAddScrollVelocityRequestedEventArgs e =
					new ScrollControllerAddScrollVelocityRequestedEventArgs(
						offsetVelocity,
						inertiaDecayRate);
				AddScrollVelocityRequested(this, e);
				return e.CorrelationId;
			}
			return -1;
		}

		private void RaiseLogMessage(string message)
		{
			Owner.RaiseLogMessage(message);
		}
	}

	private struct OperationInfo
	{
		public int CorrelationId;
		public Point RelativeOffsetChange;
		public Point OffsetTarget;

		public OperationInfo(int correlationId, Point relativeOffsetChange, Point offsetTarget) : this()
		{
			CorrelationId = correlationId;
			RelativeOffsetChange = relativeOffsetChange;
			OffsetTarget = offsetTarget;
		}
	}

	private const float SmallChangeAdditionalVelocity = 144.0f;
	private const float SmallChangeInertiaDecayRate = 0.975f;

	private List<string> lstAsyncEventMessage = new List<string>();
	private List<int> lstViewChangeCorrelationIds = new List<int>();
	private List<int> lstScrollToCorrelationIds = new List<int>();
	private List<int> lstScrollByCorrelationIds = new List<int>();
	private List<int> lstAddScrollVelocityCorrelationIds = new List<int>();
	private Dictionary<int, OperationInfo> operations = new Dictionary<int, OperationInfo>();
	private UniScrollController horizontalScrollController = null;
	private UniScrollController verticalScrollController = null;
	private RepeatButton biDecrementRepeatButton = null;
	private RepeatButton biIncrementRepeatButton = null;
	private RepeatButton horizontalIncrementVerticalDecrementRepeatButton = null;
	private RepeatButton horizontalDecrementVerticalIncrementRepeatButton = null;
	private RepeatButton horizontalDecrementRepeatButton = null;
	private RepeatButton horizontalIncrementRepeatButton = null;
	private RepeatButton verticalDecrementRepeatButton = null;
	private RepeatButton verticalIncrementRepeatButton = null;
	private bool isRailEnabled = true;
	private Point preManipulationThumbOffset;

	public event TypedEventHandler<BiDirectionalScrollController, string> LogMessage;
	public event TypedEventHandler<BiDirectionalScrollController, BiDirectionalScrollControllerScrollingScrollCompletedEventArgs> ScrollCompleted;

	public BiDirectionalScrollController()
	{
		this.DefaultStyleKey = typeof(BiDirectionalScrollController);
		this.IsScrollingWithMouse = false;
		this.horizontalScrollController = new UniScrollController(this, Orientation.Horizontal);
		this.verticalScrollController = new UniScrollController(this, Orientation.Vertical);

		this.horizontalScrollController.ScrollCompleted += UniScrollController_ScrollCompleted;
		this.verticalScrollController.ScrollCompleted += UniScrollController_ScrollCompleted;

		IsEnabledChanged += BiDirectionalScrollController_IsEnabledChanged;
		SizeChanged += BiDirectionalScrollController_SizeChanged;
	}

	public bool IsRailEnabled
	{
		get
		{
			return isRailEnabled;
		}
		set
		{
			if (isRailEnabled != value)
			{
				isRailEnabled = value;

				if (Thumb != null)
				{
					Thumb.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
					if (isRailEnabled)
					{
						Thumb.ManipulationMode |= ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY;
					}
				}

				horizontalScrollController.IsRailEnabled = isRailEnabled;
				verticalScrollController.IsRailEnabled = isRailEnabled;
			}
		}
	}

	public IScrollController HorizontalScrollController
	{
		get
		{
			return horizontalScrollController;
		}
	}

	public IScrollController VerticalScrollController
	{
		get
		{
			return verticalScrollController;
		}
	}

	public int ScrollTo(double horizontalOffset, double verticalOffset, ScrollingAnimationMode animationMode)
	{
		int viewChangeCorrelationId = RaiseScrollToRequested(
			new Point(horizontalOffset, verticalOffset),
			animationMode, false /*hookupCompletion*/);
		lstViewChangeCorrelationIds.Add(viewChangeCorrelationId);
		return viewChangeCorrelationId;
	}

	public int ScrollBy(double horizontalOffsetDelta, double verticalOffsetDelta, ScrollingAnimationMode animationMode)
	{
		int viewChangeCorrelationId = RaiseScrollByRequested(
			new Point(horizontalOffsetDelta, verticalOffsetDelta),
			animationMode, false /*hookupCompletion*/);
		lstViewChangeCorrelationIds.Add(viewChangeCorrelationId);
		return viewChangeCorrelationId;
	}

	public int AddScrollVelocity(Vector2 offsetVelocity, Vector2? inertiaDecayRate)
	{
		int viewChangeCorrelationId = RaiseAddScrollVelocityRequested(
			offsetVelocity, inertiaDecayRate, false /*hookupCompletion*/);
		lstViewChangeCorrelationIds.Add(viewChangeCorrelationId);
		return viewChangeCorrelationId;
	}

	protected override void OnApplyTemplate()
	{
		UnhookHandlers();

		base.OnApplyTemplate();

		Thumb = GetTemplateChild("Thumb") as FrameworkElement;
		biDecrementRepeatButton = GetTemplateChild("BiDecrementRepeatButton") as RepeatButton;
		biIncrementRepeatButton = GetTemplateChild("BiIncrementRepeatButton") as RepeatButton;
		horizontalIncrementVerticalDecrementRepeatButton = GetTemplateChild("HorizontalIncrementVerticalDecrementRepeatButton") as RepeatButton;
		horizontalDecrementVerticalIncrementRepeatButton = GetTemplateChild("HorizontalDecrementVerticalIncrementRepeatButton") as RepeatButton;
		horizontalDecrementRepeatButton = GetTemplateChild("HorizontalDecrementRepeatButton") as RepeatButton;
		horizontalIncrementRepeatButton = GetTemplateChild("HorizontalIncrementRepeatButton") as RepeatButton;
		verticalDecrementRepeatButton = GetTemplateChild("VerticalDecrementRepeatButton") as RepeatButton;
		verticalIncrementRepeatButton = GetTemplateChild("VerticalIncrementRepeatButton") as RepeatButton;

		if (Thumb != null)
		{
			Thumb.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
			if (IsRailEnabled)
			{
				Thumb.ManipulationMode |= ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY;
			}

			FrameworkElement parent = Thumb.Parent as FrameworkElement;

			if (parent != null)
			{
				horizontalScrollController.SetPanningFrameworkElement(Thumb);
				verticalScrollController.SetPanningFrameworkElement(Thumb);
			}
			else
			{
				horizontalScrollController.SetPanningFrameworkElement(null);
				verticalScrollController.SetPanningFrameworkElement(null);
			}
		}
		else
		{
			horizontalScrollController.SetPanningFrameworkElement(null);
			verticalScrollController.SetPanningFrameworkElement(null);
		}

		HookHandlers();
	}

	internal bool IsScrollingWithMouse
	{
		get;
		private set;
	}

	internal Point OffsetTarget
	{
		get;
		set;
	}

	private FrameworkElement Thumb
	{
		get;
		set;
	}

	private double HorizontalThumbOffset
	{
		get
		{
			if (Thumb != null)
			{
				double parentWidth = 0.0;
				FrameworkElement parent = Thumb.Parent as FrameworkElement;
				if (parent != null)
				{
					parentWidth = parent.ActualWidth;
				}
				if (horizontalScrollController.MaxOffset != horizontalScrollController.MinOffset)
				{
					return horizontalScrollController.Offset / (horizontalScrollController.MaxOffset - horizontalScrollController.MinOffset) * (parentWidth - Thumb.Width);
				}
			}

			return 0.0;
		}
	}

	private double VerticalThumbOffset
	{
		get
		{
			if (Thumb != null)
			{
				double parentHeight = 0.0;
				FrameworkElement parent = Thumb.Parent as FrameworkElement;
				if (parent != null)
				{
					parentHeight = parent.ActualHeight;
				}
				if (verticalScrollController.MaxOffset != verticalScrollController.MinOffset)
				{
					return verticalScrollController.Offset / (verticalScrollController.MaxOffset - verticalScrollController.MinOffset) * (parentHeight - Thumb.Height);
				}
			}

			return 0.0;
		}
	}

	private void HookHandlers()
	{
		if (biDecrementRepeatButton != null)
		{
			biDecrementRepeatButton.Click += BiDecrementRepeatButton_Click;
		}

		if (biIncrementRepeatButton != null)
		{
			biIncrementRepeatButton.Click += BiIncrementRepeatButton_Click;
		}

		if (horizontalIncrementVerticalDecrementRepeatButton != null)
		{
			horizontalIncrementVerticalDecrementRepeatButton.Click += HorizontalIncrementVerticalDecrementRepeatButton_Click;
		}

		if (horizontalDecrementVerticalIncrementRepeatButton != null)
		{
			horizontalDecrementVerticalIncrementRepeatButton.Click += HorizontalDecrementVerticalIncrementRepeatButton_Click;
		}

		if (horizontalDecrementRepeatButton != null)
		{
			horizontalDecrementRepeatButton.Click += HorizontalDecrementRepeatButton_Click;
		}

		if (horizontalIncrementRepeatButton != null)
		{
			horizontalIncrementRepeatButton.Click += HorizontalIncrementRepeatButton_Click;
		}

		if (verticalDecrementRepeatButton != null)
		{
			verticalDecrementRepeatButton.Click += VerticalDecrementRepeatButton_Click;
		}

		if (verticalIncrementRepeatButton != null)
		{
			verticalIncrementRepeatButton.Click += VerticalIncrementRepeatButton_Click;
		}

		if (Thumb != null)
		{
			Thumb.PointerPressed += Thumb_PointerPressed;
			Thumb.ManipulationStarting += Thumb_ManipulationStarting;
			Thumb.ManipulationDelta += Thumb_ManipulationDelta;
			Thumb.ManipulationCompleted += Thumb_ManipulationCompleted;

			FrameworkElement parent = Thumb.Parent as FrameworkElement;

			if (parent != null)
			{
				parent.PointerPressed += Parent_PointerPressed;
			}
		}
	}

	private void UnhookHandlers()
	{
		if (biDecrementRepeatButton != null)
		{
			biDecrementRepeatButton.Click -= BiDecrementRepeatButton_Click;
		}

		if (biIncrementRepeatButton != null)
		{
			biIncrementRepeatButton.Click -= BiIncrementRepeatButton_Click;
		}

		if (horizontalIncrementVerticalDecrementRepeatButton != null)
		{
			horizontalIncrementVerticalDecrementRepeatButton.Click -= HorizontalIncrementVerticalDecrementRepeatButton_Click;
		}

		if (horizontalDecrementVerticalIncrementRepeatButton != null)
		{
			horizontalDecrementVerticalIncrementRepeatButton.Click -= HorizontalDecrementVerticalIncrementRepeatButton_Click;
		}

		if (horizontalDecrementRepeatButton != null)
		{
			horizontalDecrementRepeatButton.Click -= HorizontalDecrementRepeatButton_Click;
		}

		if (horizontalIncrementRepeatButton != null)
		{
			horizontalIncrementRepeatButton.Click -= HorizontalIncrementRepeatButton_Click;
		}

		if (verticalDecrementRepeatButton != null)
		{
			verticalDecrementRepeatButton.Click -= VerticalDecrementRepeatButton_Click;
		}

		if (verticalIncrementRepeatButton != null)
		{
			verticalIncrementRepeatButton.Click -= VerticalIncrementRepeatButton_Click;
		}

		if (Thumb != null)
		{
			Thumb.PointerPressed -= Thumb_PointerPressed;
			Thumb.ManipulationStarting -= Thumb_ManipulationStarting;
			Thumb.ManipulationDelta -= Thumb_ManipulationDelta;
			Thumb.ManipulationCompleted -= Thumb_ManipulationCompleted;

			FrameworkElement parent = Thumb.Parent as FrameworkElement;
			if (parent != null)
			{
				parent.PointerPressed -= Parent_PointerPressed;
			}
		}
	}

	private void BiDecrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: BiDecrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(-SmallChangeAdditionalVelocity), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void BiIncrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: BiIncrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(SmallChangeAdditionalVelocity), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void HorizontalIncrementVerticalDecrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: HorizontalIncrementVerticalDecrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(SmallChangeAdditionalVelocity, -SmallChangeAdditionalVelocity), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void HorizontalDecrementVerticalIncrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: HorizontalDecrementVerticalIncrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(-SmallChangeAdditionalVelocity, SmallChangeAdditionalVelocity), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void HorizontalDecrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: HorizontalDecrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(-SmallChangeAdditionalVelocity, 0), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void HorizontalIncrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: HorizontalIncrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(SmallChangeAdditionalVelocity, 0), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void VerticalDecrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: VerticalDecrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(0, -SmallChangeAdditionalVelocity), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void VerticalIncrementRepeatButton_Click(object sender, RoutedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: VerticalIncrementRepeatButton_Click");

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		int viewChangeCorrelationId =
			RaiseAddScrollVelocityRequested(new Vector2(0, SmallChangeAdditionalVelocity), new Vector2(SmallChangeInertiaDecayRate), true /*hookupCompletion*/);
	}

	private void Thumb_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		Point pt = e.GetCurrentPoint(sender as UIElement).Position;
		RaiseLogMessage("BiDirectionalScrollController: Thumb_PointerPressed with position=" + pt);

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		switch (e.Pointer.PointerDeviceType)
		{
			case Microsoft.UI.Input.PointerDeviceType.Touch:
			case Microsoft.UI.Input.PointerDeviceType.Pen:
				RaisePanRequested(e.GetCurrentPoint(null));
				break;
			case Microsoft.UI.Input.PointerDeviceType.Mouse:
				if (!IsScrollingWithMouse)
				{
					IsScrollingWithMouse = true;

					if (HorizontalScrollController.CanScroll)
						horizontalScrollController.RaiseIsScrollingWithMouseChanged();
					if (VerticalScrollController.CanScroll)
						verticalScrollController.RaiseIsScrollingWithMouseChanged();
				}
				break;
		}
	}

	private void Parent_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		Point pt = e.GetCurrentPoint(sender as UIElement).Position;
		RaiseLogMessage("BiDirectionalScrollController: Parent_PointerPressed with position=" + pt);

		if (!HorizontalScrollController.CanScroll &&
			!VerticalScrollController.CanScroll)
			return;

		Point maxThumbOffset = MaxThumbOffset();
		double targetThumbHorizontalOffset = pt.X - Thumb.ActualWidth / 2.0;
		double targetThumbVerticalOffset = pt.Y - Thumb.ActualHeight / 2.0;

		targetThumbHorizontalOffset = Math.Max(targetThumbHorizontalOffset, 0.0);
		targetThumbVerticalOffset = Math.Max(targetThumbVerticalOffset, 0.0);

		targetThumbHorizontalOffset = Math.Min(targetThumbHorizontalOffset, maxThumbOffset.X);
		targetThumbVerticalOffset = Math.Min(targetThumbVerticalOffset, maxThumbOffset.Y);

		Point targetThumbOffset = new Point(targetThumbHorizontalOffset, targetThumbVerticalOffset);
		Point targetScrollPresenterOffset = ScrollPresenterOffsetFromThumbOffset(targetThumbOffset);

		int viewChangeCorrelationId = RaiseScrollToRequested(targetScrollPresenterOffset, ScrollingAnimationMode.Auto, true /*hookupCompletion*/);
		if (viewChangeCorrelationId != -1 && !operations.ContainsKey(viewChangeCorrelationId))
		{
			operations.Add(
				viewChangeCorrelationId,
				new OperationInfo(viewChangeCorrelationId, new Point(targetScrollPresenterOffset.X - HorizontalThumbOffset, targetScrollPresenterOffset.Y - VerticalThumbOffset), targetScrollPresenterOffset));
		}
	}

	private void Thumb_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
	{
		if (IsScrollingWithMouse)
		{
			preManipulationThumbOffset = new Point(HorizontalThumbOffset, VerticalThumbOffset);
		}
	}

	private void Thumb_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
	{
		if (IsScrollingWithMouse)
		{
			Point targetThumbOffset = new Point(
				preManipulationThumbOffset.X + e.Cumulative.Translation.X,
				preManipulationThumbOffset.Y + e.Cumulative.Translation.Y);
			Point scrollPresenterOffset = ScrollPresenterOffsetFromThumbOffset(targetThumbOffset);

			int viewChangeCorrelationId = RaiseScrollToRequested(
				scrollPresenterOffset, ScrollingAnimationMode.Disabled, true /*hookupCompletion*/);
		}
	}

	private void Thumb_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	{
		if (IsScrollingWithMouse)
		{
			IsScrollingWithMouse = false;

			if (HorizontalScrollController.CanScroll)
				horizontalScrollController.RaiseIsScrollingWithMouseChanged();
			if (VerticalScrollController.CanScroll)
				verticalScrollController.RaiseIsScrollingWithMouseChanged();
		}
	}

	private void BiDirectionalScrollController_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: IsEnabledChanged with IsEnabled=" + IsEnabled);

		horizontalScrollController.UpdateCanScroll();
		verticalScrollController.UpdateCanScroll();
	}

	private void BiDirectionalScrollController_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		RaiseLogMessage("BiDirectionalScrollController: SizeChanged with NewSize=" + e.NewSize);

		horizontalScrollController.UpdatePanningFrameworkElementLength();
		verticalScrollController.UpdatePanningFrameworkElementLength();
	}

	private Point ScrollPresenterOffsetFromThumbOffset(Point thumbOffset)
	{
		Point scrollPresenterOffset = new Point();

		if (Thumb != null)
		{
			Size parentSize = new Size();
			Size thumbSize = new Size(Thumb.ActualWidth, Thumb.ActualHeight);
			FrameworkElement parent = Thumb.Parent as FrameworkElement;
			if (parent != null)
			{
				parentSize = new Size(parent.ActualWidth, parent.ActualHeight);
			}
			if (parentSize.Width != thumbSize.Width || parentSize.Height != thumbSize.Height)
			{
				scrollPresenterOffset = new Point(
					parentSize.Width == thumbSize.Width ? 0 : (thumbOffset.X * (horizontalScrollController.MaxOffset - horizontalScrollController.MinOffset) / (parentSize.Width - thumbSize.Width)),
					parentSize.Height == thumbSize.Height ? 0 : (thumbOffset.Y * (verticalScrollController.MaxOffset - verticalScrollController.MinOffset) / (parentSize.Height - thumbSize.Height)));
			}
		}

		return scrollPresenterOffset;
	}

	private Point MaxThumbOffset()
	{
		Point maxThumbOffset = new Point();

		if (Thumb != null)
		{
			Size parentSize = new Size();
			Size thumbSize = new Size(Thumb.ActualWidth, Thumb.ActualHeight);
			FrameworkElement parent = Thumb.Parent as FrameworkElement;
			if (parent != null)
			{
				parentSize = new Size(parent.ActualWidth, parent.ActualHeight);
			}
			maxThumbOffset = new Point(
				Math.Max(0.0, parentSize.Width - thumbSize.Width),
				Math.Max(0.0, parentSize.Height - thumbSize.Height));
		}

		return maxThumbOffset;
	}

	private void UniScrollController_ScrollCompleted(IScrollController sender, UniScrollControllerScrollingScrollCompletedEventArgs args)
	{
		if (lstScrollToCorrelationIds.Contains(args.OffsetChangeCorrelationId))
		{
			lstScrollToCorrelationIds.Remove(args.OffsetChangeCorrelationId);

			Point relativeOffsetChange;

			if (operations.ContainsKey(args.OffsetChangeCorrelationId))
			{
				OperationInfo oi = operations[args.OffsetChangeCorrelationId];
				relativeOffsetChange = oi.RelativeOffsetChange;
				operations.Remove(args.OffsetChangeCorrelationId);
			}

			RaiseLogMessage("BiDirectionalScrollController: ScrollToRequest completed. OffsetChangeCorrelationId=" + args.OffsetChangeCorrelationId);
		}
		else if (lstScrollByCorrelationIds.Contains(args.OffsetChangeCorrelationId))
		{
			lstScrollByCorrelationIds.Remove(args.OffsetChangeCorrelationId);

			Point relativeOffsetChange;

			if (operations.ContainsKey(args.OffsetChangeCorrelationId))
			{
				OperationInfo oi = operations[args.OffsetChangeCorrelationId];
				relativeOffsetChange = oi.RelativeOffsetChange;
				operations.Remove(args.OffsetChangeCorrelationId);
			}

			RaiseLogMessage("BiDirectionalScrollController: ScrollByRequest completed. OffsetChangeCorrelationId=" + args.OffsetChangeCorrelationId);
		}
		else if (lstAddScrollVelocityCorrelationIds.Contains(args.OffsetChangeCorrelationId))
		{
			lstAddScrollVelocityCorrelationIds.Remove(args.OffsetChangeCorrelationId);

			RaiseLogMessage("BiDirectionalScrollController: AddScrollVelocityRequest completed. OffsetChangeCorrelationId=" + args.OffsetChangeCorrelationId);
		}

		if (lstViewChangeCorrelationIds.Contains(args.OffsetChangeCorrelationId))
		{
			lstViewChangeCorrelationIds.Remove(args.OffsetChangeCorrelationId);

			RaiseScrollCompleted(args.OffsetChangeCorrelationId);
		}
	}

	private void RaisePanRequested(PointerPoint pointerPoint)
	{
		RaiseLogMessage("BiDirectionalScrollController: RaisePanRequested with pointerPoint=" + pointerPoint);
		bool handled = horizontalScrollController.RaisePanRequested(pointerPoint);
		RaiseLogMessage("BiDirectionalScrollController: RaisePanRequested result Handled=" + handled);
	}

	private int RaiseScrollToRequested(
		Point offset,
		ScrollingAnimationMode animationMode,
		bool hookupCompletion)
	{
		RaiseLogMessage("BiDirectionalScrollController: RaiseScrollToRequested with offset=" + offset + ", animationMode=" + animationMode);

		int horizontalOffsetChangeCorrelationId = horizontalScrollController.RaiseScrollToRequested(
			offset.X, animationMode);

		if (horizontalOffsetChangeCorrelationId != -1)
			RaiseLogMessage("BiDirectionalScrollController: Horizontal ScrollToRequest started. CorrelationId=" + horizontalOffsetChangeCorrelationId);

		int verticalOffsetChangeCorrelationId = verticalScrollController.RaiseScrollToRequested(
			offset.Y, animationMode);

		if (verticalOffsetChangeCorrelationId != -1)
			RaiseLogMessage("BiDirectionalScrollController: Vertical ScrollToRequest started. CorrelationId=" + verticalOffsetChangeCorrelationId);

		int offsetChangeCorrelationId = -1;

		if (horizontalOffsetChangeCorrelationId != -1 && verticalOffsetChangeCorrelationId != -1 && horizontalOffsetChangeCorrelationId == verticalOffsetChangeCorrelationId)
		{
			offsetChangeCorrelationId = horizontalOffsetChangeCorrelationId;
		}
		else if (horizontalOffsetChangeCorrelationId != -1 && verticalOffsetChangeCorrelationId != -1 && horizontalOffsetChangeCorrelationId != verticalOffsetChangeCorrelationId)
		{
			RaiseLogMessage("BiDirectionalScrollController: ScrollToRequest CorrelationIds do not match.");
		}
		else if (horizontalOffsetChangeCorrelationId != -1)
		{
			offsetChangeCorrelationId = horizontalOffsetChangeCorrelationId;
		}
		else if (verticalOffsetChangeCorrelationId != -1)
		{
			offsetChangeCorrelationId = verticalOffsetChangeCorrelationId;
		}
		else
		{
			RaiseLogMessage("BiDirectionalScrollController: ScrollToRequest CorrelationIds are -1.");
		}

		if (hookupCompletion && offsetChangeCorrelationId != -1 && !lstScrollToCorrelationIds.Contains(offsetChangeCorrelationId))
		{
			lstScrollToCorrelationIds.Add(offsetChangeCorrelationId);
		}

		return offsetChangeCorrelationId;
	}

	private int RaiseScrollByRequested(
		Point offsetDelta,
		ScrollingAnimationMode animationMode,
		bool hookupCompletion)
	{
		RaiseLogMessage("BiDirectionalScrollController: RaiseScrollByRequested with offsetDelta=" + offsetDelta + ", animationMode=" + animationMode);

		int horizontalOffsetChangeCorrelationId = horizontalScrollController.RaiseScrollByRequested(
			offsetDelta.X, animationMode);

		if (horizontalOffsetChangeCorrelationId != -1)
			RaiseLogMessage("BiDirectionalScrollController: Horizontal ScrollByRequest started. CorrelationId=" + horizontalOffsetChangeCorrelationId);

		int verticalOffsetChangeCorrelationId = verticalScrollController.RaiseScrollByRequested(
			offsetDelta.Y, animationMode);

		if (verticalOffsetChangeCorrelationId != -1)
			RaiseLogMessage("BiDirectionalScrollController: Vertical ScrollByRequest started. CorrelationId=" + verticalOffsetChangeCorrelationId);

		int offsetChangeCorrelationId = -1;

		if (horizontalOffsetChangeCorrelationId != -1 && verticalOffsetChangeCorrelationId != -1 && horizontalOffsetChangeCorrelationId == verticalOffsetChangeCorrelationId)
		{
			offsetChangeCorrelationId = horizontalOffsetChangeCorrelationId;
		}
		else if (horizontalOffsetChangeCorrelationId != -1 && verticalOffsetChangeCorrelationId != -1 && horizontalOffsetChangeCorrelationId != verticalOffsetChangeCorrelationId)
		{
			RaiseLogMessage("BiDirectionalScrollController: ScrollByRequest CorrelationIds do not match.");
		}
		else if (horizontalOffsetChangeCorrelationId != -1)
		{
			offsetChangeCorrelationId = horizontalOffsetChangeCorrelationId;
		}
		else if (verticalOffsetChangeCorrelationId != -1)
		{
			offsetChangeCorrelationId = verticalOffsetChangeCorrelationId;
		}
		else
		{
			RaiseLogMessage("BiDirectionalScrollController: ScrollByRequest CorrelationIds are -1.");
		}

		if (hookupCompletion && offsetChangeCorrelationId != -1 && !lstScrollByCorrelationIds.Contains(offsetChangeCorrelationId))
		{
			lstScrollByCorrelationIds.Add(offsetChangeCorrelationId);
		}

		return offsetChangeCorrelationId;
	}

	private int RaiseAddScrollVelocityRequested(
		Vector2 additionalVelocity, Vector2? inertiaDecayRate, bool hookupCompletion)
	{
		RaiseLogMessage("BiDirectionalScrollController: RaiseAddScrollVelocityRequested with additionalVelocity=" + additionalVelocity + ", inertiaDecayRate=" + inertiaDecayRate);

		int horizontalOffsetChangeCorrelationId = -1;
		int verticalOffsetChangeCorrelationId = -1;

		if (additionalVelocity.X != 0.0f)
		{
			horizontalOffsetChangeCorrelationId = horizontalScrollController.RaiseAddScrollVelocityRequested(
				additionalVelocity.X, inertiaDecayRate == null ? (float?)null : inertiaDecayRate.Value.X);

			if (horizontalOffsetChangeCorrelationId != -1)
				RaiseLogMessage("BiDirectionalScrollController: AddScrollVelocityRequest started. CorrelationId=" + horizontalOffsetChangeCorrelationId);
		}

		if (additionalVelocity.Y != 0.0f)
		{
			verticalOffsetChangeCorrelationId = verticalScrollController.RaiseAddScrollVelocityRequested(
				additionalVelocity.Y, inertiaDecayRate == null ? (float?)null : inertiaDecayRate.Value.Y);

			if (verticalOffsetChangeCorrelationId != -1)
				RaiseLogMessage("BiDirectionalScrollController: AddScrollVelocityRequest started. CorrelationId=" + verticalOffsetChangeCorrelationId);
		}

		int offsetChangeCorrelationId = -1;

		if (horizontalOffsetChangeCorrelationId != -1 && verticalOffsetChangeCorrelationId != -1 && horizontalOffsetChangeCorrelationId == verticalOffsetChangeCorrelationId)
		{
			offsetChangeCorrelationId = horizontalOffsetChangeCorrelationId;
		}
		else if (horizontalOffsetChangeCorrelationId != -1 && verticalOffsetChangeCorrelationId != -1 && horizontalOffsetChangeCorrelationId != verticalOffsetChangeCorrelationId)
		{
			RaiseLogMessage("BiDirectionalScrollController: AddScrollVelocityRequest operations do not match.");
		}
		else if (horizontalOffsetChangeCorrelationId != -1)
		{
			offsetChangeCorrelationId = horizontalOffsetChangeCorrelationId;
		}
		else if (verticalOffsetChangeCorrelationId != -1)
		{
			offsetChangeCorrelationId = verticalOffsetChangeCorrelationId;
		}
		else
		{
			RaiseLogMessage("BiDirectionalScrollController: AddScrollVelocityRequest operations are null.");
		}

		if (hookupCompletion && offsetChangeCorrelationId != -1 && !lstAddScrollVelocityCorrelationIds.Contains(offsetChangeCorrelationId))
		{
			lstAddScrollVelocityCorrelationIds.Add(offsetChangeCorrelationId);
		}

		return offsetChangeCorrelationId;
	}

	internal int OperationsCount
	{
		get
		{
			return operations.Count;
		}
	}

	internal void RaiseLogMessage(string message)
	{
		if (LogMessage != null)
		{
			LogMessage(this, message);
		}
	}

	private void RaiseScrollCompleted(int viewChangeCorrelationId)
	{
		if (ScrollCompleted != null)
		{
			BiDirectionalScrollControllerScrollingScrollCompletedEventArgs args = new BiDirectionalScrollControllerScrollingScrollCompletedEventArgs(viewChangeCorrelationId);

			ScrollCompleted(this, args);
		}
	}
}
