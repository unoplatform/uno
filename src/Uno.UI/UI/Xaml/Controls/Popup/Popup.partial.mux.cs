// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Popup_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using DirectUI;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Popup
{
	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
#if HAS_UNO
		if (args.Property == AllowFocusOnInteractionProperty ||
			args.Property == AllowFocusWhenDisabledProperty)
		{
			PropagateFocusProperties();
		}

		// TODO Uno: The child is parented to the PopupPanel, so that is where it inherits the popup's flow direction from.
		if (args.Property == FlowDirectionProperty && PopupPanel is { } popupPanel)
		{
			popupPanel.FlowDirection = (FlowDirection)args.NewValue;
		}
#endif

		base.OnPropertyChanged2(args);

		if (args.Property == IsOpenProperty)
		{
			if ((bool)args.NewValue) //is now open
			{
#if HAS_UNO
				m_everOpened = true;
#endif

				// If we haven't already attached event handlers for the placement target's LayoutUpdated event
				// and the child's Loaded and SizeChanged events, let's do so now.
				var placementTarget = PlacementTarget;

				if (placementTarget is not null && m_placementTargetLayoutUpdatedToken.Disposable is null)
				{
					AttachPlacementTargetLayoutUpdated(placementTarget);
				}

				if (Child is FrameworkElement childAsFE && m_childSizeChangedToken.Disposable is null)
				{
					AttachChildHandlers(childAsFE);
				}

				SetPositionFromPlacement();
			}
			else //is now closed
			{
				// We'll invalidate the calculated placement and justification when we close, since it may be different when we reopen.
				m_placementAndJustificationCalculated = false;
			}
		}
		else if (args.Property == DesiredPlacementProperty || args.Property == PlacementTargetProperty)
		{
			// Either of these invalidates the calculated placement and justification since we're intentionally moving the popup.
			m_placementAndJustificationCalculated = false;

			// If we're updating the placement target, we should attach a SizeChanged event handler
			// so we ensure we always snap to wherever the target's bounds are.
			if (args.Property == PlacementTargetProperty)
			{
				m_placementTargetLayoutUpdatedToken.Disposable = null;

				if (args.NewValue is FrameworkElement newPlacementTarget)
				{
					AttachPlacementTargetLayoutUpdated(newPlacementTarget);
				}
			}

			// Either of these changing should cause us to reset the adjustments on the placement target's parent popup, if it has one.
			var placementTarget = PlacementTarget;

			// We won't have a chance to run layout again, so we'll save off the original adjustments and subtract them off in our positioning calculations.
			float xPlacementTargetAdjustment = 0.0f;
			float yPlacementTargetAdjustment = 0.0f;

			if (placementTarget is not null)
			{
				if (GetClosestPopupAncestor(placementTarget) is { } placementTargetPopupCore)
				{
					xPlacementTargetAdjustment = placementTargetPopupCore.m_hAdjustment;
					yPlacementTargetAdjustment = placementTargetPopupCore.m_vAdjustment;

					placementTargetPopupCore.m_hAdjustment = 0.0f;
					placementTargetPopupCore.m_vAdjustment = 0.0f;
				}
			}

			SetPositionFromPlacement(xPlacementTargetAdjustment, yPlacementTargetAdjustment);
		}
		else if (args.Property == ChildProperty)
		{
			// When the popup's child loads or is resized, we'll want to re-evaluate positioning,
			// so let's attach event handlers to do that.
			m_childSizeChangedToken.Disposable = null;
			m_childLoadedToken.Disposable = null;

			if (args.NewValue is FrameworkElement newChild)
			{
				AttachChildHandlers(newChild);
			}

			SetPositionFromPlacement();
		}
		else if (args.Property == ShouldConstrainToRootBoundsProperty)
		{
			bool shouldConstrainToRootBounds = (bool)args.NewValue;

			// If the popup has already been unconstrained from root bounds, that can't be undone.
			if (IsWindowed && shouldConstrainToRootBounds)
			{
				throw new InvalidOperationException("ShouldConstrainToRootBounds cannot be changed after the popup has been opened.");
			}

			// To simplify implementation, ShouldConstrainToRootBounds needs to be set before the popup is ever opened.
			if (WasEverOpened && !shouldConstrainToRootBounds)
			{
				throw new InvalidOperationException("ShouldConstrainToRootBounds cannot be changed after the popup has been opened.");
			}
		}
	}

	private void AttachPlacementTargetLayoutUpdated(FrameworkElement placementTarget)
	{
		placementTarget.LayoutUpdated += OnPlacementTargetLayoutUpdated;
		m_placementTargetLayoutUpdatedToken.Disposable = Disposable.Create(() => placementTarget.LayoutUpdated -= OnPlacementTargetLayoutUpdated);
	}

	private void AttachChildHandlers(FrameworkElement child)
	{
		child.SizeChanged += OnChildSizeChanged;
		m_childSizeChangedToken.Disposable = Disposable.Create(() => child.SizeChanged -= OnChildSizeChanged);

		child.Loaded += OnChildLoaded;
		m_childLoadedToken.Disposable = Disposable.Create(() => child.Loaded -= OnChildLoaded);
	}

	private bool GetIsConstrainedToRootBounds()
	{
		// If we haven't been windowed yet, then we won't know for sure whether we'll constrain ourselves to root bounds.
		// However, we want apps to be able to rely on the value of this property at any time, so in those cases,
		// we'll predict whether we will be constrained on the basis of whether ShouldConstrainToRootBounds
		// is set to false and of whether the current platform supports windowed popups.
		// If both of those are the case, then we'll return false, as we know that the popup won't be constrained
		// to root bounds if nothing changes.
		if (IsWindowed)
		{
			return false;
		}

		return !DoesPlatformSupportWindowedPopup() || ShouldConstrainToRootBounds;
	}

	private void SetPositionFromPlacement(float xPlacementTargetAdjustment = 0.0f, float yPlacementTargetAdjustment = 0.0f)
	{
		float originalCalculatedHOffset = m_calculatedHOffset;
		float originalCalculatedVOffset = m_calculatedVOffset;

		var isOpen = IsOpen;
		var child = Child;
		var placementTarget = PlacementTarget;
		var desiredPlacement = DesiredPlacement;

		Popup? placementTargetPopupCore = null;
		bool placementTargetPopupMoved = false;

		if (placementTarget is not null)
		{
			placementTargetPopupCore = GetClosestPopupAncestor(placementTarget);
		}

		var actualPlacement = desiredPlacement;

		// If we aren't open or have no child, then we have no position to set.  Otherwise, if both
		// PlacementTarget and Placement have been set, then we need to calculate
		// the popup's position from those values.
		if (isOpen && child is not null && placementTarget is not null && desiredPlacement != PopupPlacementMode.Auto)
		{
			// Since we'll need to know momentarily whether or not the popup is windowed,
			// let's make it windowed now if it's going to be.
			SetIsWindowedIfNeeded();

			// This isn't a flyout if the above values are set, but we can reuse the same logic
			// to get the available window rect.
			Rect availableRect = FlyoutBase.CalculateAvailableWindowRect(
				this,
				placementTarget,
				false /* hasTargetPosition */,
				default,
				false /* isFull */);

			var flowDirection = FlowDirection;

			var childSizeVector = child.ActualSize;

			var childSize = new Size(childSizeVector.X, childSizeVector.Y);

			// If the child hasn't been sized yet, there's nothing for us to do.
			if (childSize.Width > 0 && childSize.Height > 0)
			{
				Point position = default;

				Rect targetBounds = placementTarget.TransformToVisual(null).TransformBounds(new Rect(0, 0, placementTarget.ActualWidth, placementTarget.ActualHeight));

				targetBounds.X -= xPlacementTargetAdjustment;
				targetBounds.Y -= yPlacementTargetAdjustment;

				Point originalPosition;
				bool tryMovingParentPlacementPopup = false;

				MajorPlacementMode majorPlacement;
				PreferredJustification justification;

				if (m_placementAndJustificationCalculated)
				{
					majorPlacement = m_calculatedMajorPlacement;
					justification = m_calculatedJustification;

					SetPositionFromMajorPlacementAndJustification(
						ref position,
						childSize,
						targetBounds,
						flowDirection,
						m_calculatedMajorPlacement,
						m_calculatedJustification);

					originalPosition = position;

					// We'll check whether we're out of bounds.  We don't actually want to update the placement and justification,
					// so we'll save those values off.
					var dummyPlacement = majorPlacement;
					var dummyJustification = justification;

					FlipMajorPlacementAndJustificationIfOutOfBounds(
						ref dummyPlacement,
						ref dummyJustification,
						position,
						childSize,
						availableRect,
						out bool valueChanged);

					// If so, then if the placement target is itself in a popup that's placed at an absolute position,
					// we'll try moving that popup to bring this popup positioned relative to it into view.
					tryMovingParentPlacementPopup = valueChanged;
				}
				else
				{
					majorPlacement = GetMajorPlacementFromPlacement(desiredPlacement, flowDirection);
					justification = GetJustificationFromPlacement(desiredPlacement, flowDirection);

					SetPositionFromMajorPlacementAndJustification(
						ref position,
						childSize,
						targetBounds,
						flowDirection,
						majorPlacement,
						justification);

					originalPosition = position;

					// If the popup is out of bounds in the x- or y-direction, change which side we're showing it on
					// to put it back into bounds.
					FlipMajorPlacementAndJustificationIfOutOfBounds(
						ref majorPlacement,
						ref justification,
						position,
						childSize,
						availableRect,
						out bool valueChanged);

					if (valueChanged)
					{
						SetPositionFromMajorPlacementAndJustification(
							ref position,
							childSize,
							targetBounds,
							flowDirection,
							majorPlacement,
							justification);

						// Now we'll check whether we're still out of bounds after flipping sides.
						FlipMajorPlacementAndJustificationIfOutOfBounds(
							ref majorPlacement,
							ref justification,
							position,
							childSize,
							availableRect,
							out valueChanged);

						// If so, we'll try one last thing - if the placement target is itself in a popup that's placed at an
						// absolute position, we'll try moving that popup to bring this popup positioned relative to it into view.
						tryMovingParentPlacementPopup = valueChanged;
					}

					m_calculatedMajorPlacement = majorPlacement;
					m_calculatedJustification = justification;

					m_placementAndJustificationCalculated = true;
				}

				actualPlacement = GetPlacementFromMajorPlacementAndJustification(majorPlacement, justification, flowDirection);

				if (tryMovingParentPlacementPopup)
				{
					if (placementTargetPopupCore is not null)
					{
						if (placementTargetPopupCore.IsOpen && placementTargetPopupCore.DesiredPlacement == PopupPlacementMode.Auto)
						{
							float xAdjustment = 0.0f;
							float yAdjustment = 0.0f;

							if (majorPlacement == MajorPlacementMode.Left ||
								majorPlacement == MajorPlacementMode.Right)
							{
								if (originalPosition.X < availableRect.X)
								{
									xAdjustment = (float)(availableRect.X - originalPosition.X);
								}
								else if (originalPosition.X + childSize.Width > availableRect.X + availableRect.Width)
								{
									xAdjustment = (float)(availableRect.X + availableRect.Width - (originalPosition.X + childSize.Width));
								}
							}

							if (majorPlacement == MajorPlacementMode.Top ||
								majorPlacement == MajorPlacementMode.Bottom)
							{
								if (originalPosition.Y < availableRect.Y)
								{
									yAdjustment = (float)(availableRect.Y - originalPosition.Y);
								}
								else if (originalPosition.Y + childSize.Height > availableRect.Y + availableRect.Height)
								{
									yAdjustment = (float)(availableRect.Y + availableRect.Height - (originalPosition.Y + childSize.Height));
								}
							}

							const float epsilon = 0.1f;

							if (Math.Abs(xAdjustment) > epsilon)
							{
								placementTargetPopupCore.m_hAdjustment += xAdjustment;
								targetBounds.X += xAdjustment;
								placementTargetPopupMoved = true;
							}

							if (Math.Abs(yAdjustment) > epsilon)
							{
								placementTargetPopupCore.m_vAdjustment += yAdjustment;
								targetBounds.Y += yAdjustment;
								placementTargetPopupMoved = true;
							}
						}
					}

					SetPositionFromMajorPlacementAndJustification(
						ref position,
						childSize,
						targetBounds,
						flowDirection,
						majorPlacement,
						justification);
				}

				// We need to subtract off the layout offset between this element and the root,
				// since the placement position should override the layout position.
				Point topLeftCorner = TransformToVisual(null).TransformPoint(default);

				m_calculatedHOffset = (float)(position.X - topLeftCorner.X);
				m_calculatedVOffset = (float)(position.Y - topLeftCorner.Y);
			}
			else
			{
				m_calculatedHOffset = 0.0f;
				m_calculatedVOffset = 0.0f;

				if (placementTargetPopupCore is not null)
				{
					placementTargetPopupCore.m_hAdjustment = 0.0f;
					placementTargetPopupCore.m_vAdjustment = 0.0f;
				}
			}
		}
		else
		{
			// If we aren't open or aren't calculating the popup position from its placement and target,
			// then we'll revert these values back to zero to allow normal layout to resume.
			m_calculatedHOffset = 0.0f;
			m_calculatedVOffset = 0.0f;

			if (placementTargetPopupCore is not null)
			{
				placementTargetPopupCore.m_hAdjustment = 0.0f;
				placementTargetPopupCore.m_vAdjustment = 0.0f;
			}
		}

		// If we're open and the placement we arrived upon was different than the previously used one,
		// then we'll report to the app the fact that it changed.
		if (isOpen)
		{
			ActualPlacement = actualPlacement;
		}

		// If we updated the popup's position, then we need to invalidate arrange for the popup root
		// to have it pick that up.
		if (!DoubleUtil.AreClose(m_calculatedHOffset, originalCalculatedHOffset) ||
			!DoubleUtil.AreClose(m_calculatedVOffset, originalCalculatedVOffset) ||
			placementTargetPopupMoved)
		{
			// TODO Uno: Each popup has its own host panel here, and a moved parent popup has to be arranged too.
			PopupPanel?.InvalidateArrange();

			if (placementTargetPopupMoved)
			{
				placementTargetPopupCore?.PopupPanel?.InvalidateArrange();
			}
		}
	}

	private static void SetPositionFromMajorPlacementAndJustification(
		ref Point position,
		Size childSize,
		Rect targetBounds,
		FlowDirection flowDirection,
		MajorPlacementMode majorPlacement,
		PreferredJustification justification)
	{
		switch (majorPlacement)
		{
			case MajorPlacementMode.Top:
				position.Y = targetBounds.Y - childSize.Height;
				break;
			case MajorPlacementMode.Bottom:
				position.Y = targetBounds.Y + targetBounds.Height;
				break;
			case MajorPlacementMode.Left:
				position.X = targetBounds.X - childSize.Width;
				break;
			case MajorPlacementMode.Right:
				position.X = targetBounds.X + targetBounds.Width;
				break;
		}

		switch (justification)
		{
			case PreferredJustification.HorizontalCenter:
				position.X = targetBounds.X + targetBounds.Width / 2 - childSize.Width / 2;
				break;
			case PreferredJustification.Left:
				position.X = targetBounds.X;
				break;
			case PreferredJustification.Right:
				position.X = targetBounds.X + targetBounds.Width - childSize.Width;
				break;
			case PreferredJustification.VerticalCenter:
				position.Y = targetBounds.Y + targetBounds.Height / 2 - childSize.Height / 2;
				break;
			case PreferredJustification.Top:
				position.Y = targetBounds.Y;
				break;
			case PreferredJustification.Bottom:
				position.Y = targetBounds.Y + targetBounds.Height - childSize.Height;
				break;
		}

		// If the flow direction is RTL, then we need the position to be the top-right corner of the popup,
		// rather than the top-left corner.
		if (flowDirection == FlowDirection.RightToLeft)
		{
			position.X += childSize.Width;
		}
	}

	private static void FlipMajorPlacementAndJustificationIfOutOfBounds(
		ref MajorPlacementMode majorPlacement,
		ref PreferredJustification justification,
		Point position,
		Size childSize,
		Rect availableRect,
		out bool valueChanged)
	{
		valueChanged = false;

		if (position.X + childSize.Width > availableRect.X + availableRect.Width)
		{
			// If we were positioned on the right, we're now positioned on the left.
			if (majorPlacement == MajorPlacementMode.Right)
			{
				majorPlacement = MajorPlacementMode.Left;
				valueChanged = true;
			}

			// If we were justified left, we're now justified right.
			if (justification == PreferredJustification.Left)
			{
				justification = PreferredJustification.Right;
				valueChanged = true;
			}
		}
		else if (position.X < availableRect.X)
		{
			// If we were positioned on the left, we're now positioned on the right.
			if (majorPlacement == MajorPlacementMode.Left)
			{
				majorPlacement = MajorPlacementMode.Right;
				valueChanged = true;
			}

			// If we were justified right, we're now justified left.
			if (justification == PreferredJustification.Right)
			{
				justification = PreferredJustification.Left;
				valueChanged = true;
			}
		}

		if (position.Y < availableRect.Y)
		{
			// If we were positioned on the top, we're now positioned on the bottom.
			if (majorPlacement == MajorPlacementMode.Top)
			{
				majorPlacement = MajorPlacementMode.Bottom;
				valueChanged = true;
			}

			// If we were justified bottom, we're now justified top.
			if (justification == PreferredJustification.Bottom)
			{
				justification = PreferredJustification.Top;
				valueChanged = true;
			}
		}
		else if (position.Y + childSize.Height > availableRect.Y + availableRect.Height)
		{
			// If we were positioned on the bottom, we're now positioned on the top.
			if (majorPlacement == MajorPlacementMode.Bottom)
			{
				majorPlacement = MajorPlacementMode.Top;
				valueChanged = true;
			}

			// If we were justified top, we're now justified bottom.
			if (justification == PreferredJustification.Top)
			{
				justification = PreferredJustification.Bottom;
				valueChanged = true;
			}
		}
	}

	private static MajorPlacementMode GetMajorPlacementFromPlacement(
		PopupPlacementMode placement,
		FlowDirection flowDirection)
	{
		switch (placement)
		{
			case PopupPlacementMode.Top:
			case PopupPlacementMode.TopEdgeAlignedLeft:
			case PopupPlacementMode.TopEdgeAlignedRight:
				return MajorPlacementMode.Top;
			case PopupPlacementMode.Bottom:
			case PopupPlacementMode.BottomEdgeAlignedLeft:
			case PopupPlacementMode.BottomEdgeAlignedRight:
				return MajorPlacementMode.Bottom;
			case PopupPlacementMode.Left:
			case PopupPlacementMode.LeftEdgeAlignedTop:
			case PopupPlacementMode.LeftEdgeAlignedBottom:
				return flowDirection == FlowDirection.LeftToRight ? MajorPlacementMode.Left : MajorPlacementMode.Right;
			case PopupPlacementMode.Right:
			case PopupPlacementMode.RightEdgeAlignedTop:
			case PopupPlacementMode.RightEdgeAlignedBottom:
				return flowDirection == FlowDirection.LeftToRight ? MajorPlacementMode.Right : MajorPlacementMode.Left;
			default:
				global::System.Diagnostics.Debug.Fail("Unsupported PopupPlacementMode");
				return MajorPlacementMode.Auto;
		}
	}

	private static PreferredJustification GetJustificationFromPlacement(
		PopupPlacementMode placement,
		FlowDirection flowDirection)
	{
		switch (placement)
		{
			case PopupPlacementMode.Top:
			case PopupPlacementMode.Bottom:
				return PreferredJustification.HorizontalCenter;
			case PopupPlacementMode.Left:
			case PopupPlacementMode.Right:
				return PreferredJustification.VerticalCenter;
			case PopupPlacementMode.TopEdgeAlignedLeft:
			case PopupPlacementMode.BottomEdgeAlignedLeft:
				return flowDirection == FlowDirection.LeftToRight ? PreferredJustification.Left : PreferredJustification.Right;
			case PopupPlacementMode.TopEdgeAlignedRight:
			case PopupPlacementMode.BottomEdgeAlignedRight:
				return flowDirection == FlowDirection.LeftToRight ? PreferredJustification.Right : PreferredJustification.Left;
			case PopupPlacementMode.LeftEdgeAlignedTop:
			case PopupPlacementMode.RightEdgeAlignedTop:
				return PreferredJustification.Top;
			case PopupPlacementMode.LeftEdgeAlignedBottom:
			case PopupPlacementMode.RightEdgeAlignedBottom:
				return PreferredJustification.Bottom;
			default:
				global::System.Diagnostics.Debug.Fail("Unsupported PopupPlacementMode");
				return PreferredJustification.Auto;
		}
	}

	private static PopupPlacementMode GetPlacementFromMajorPlacementAndJustification(
		MajorPlacementMode majorPlacement,
		PreferredJustification justification,
		FlowDirection flowDirection)
	{
		bool isLtr = flowDirection == FlowDirection.LeftToRight;

		switch (majorPlacement)
		{
			case MajorPlacementMode.Top:
				switch (justification)
				{
					case PreferredJustification.HorizontalCenter:
						return PopupPlacementMode.Top;
					case PreferredJustification.Left:
						return isLtr ? PopupPlacementMode.TopEdgeAlignedLeft : PopupPlacementMode.TopEdgeAlignedRight;
					case PreferredJustification.Right:
						return isLtr ? PopupPlacementMode.TopEdgeAlignedRight : PopupPlacementMode.TopEdgeAlignedLeft;
					default:
						global::System.Diagnostics.Debug.Fail("Invalid MajorPlacementMode/PreferredJustification combination");
						return PopupPlacementMode.Auto;
				}
			case MajorPlacementMode.Bottom:
				switch (justification)
				{
					case PreferredJustification.HorizontalCenter:
						return PopupPlacementMode.Bottom;
					case PreferredJustification.Left:
						return isLtr ? PopupPlacementMode.BottomEdgeAlignedLeft : PopupPlacementMode.BottomEdgeAlignedRight;
					case PreferredJustification.Right:
						return isLtr ? PopupPlacementMode.BottomEdgeAlignedRight : PopupPlacementMode.BottomEdgeAlignedLeft;
					default:
						global::System.Diagnostics.Debug.Fail("Invalid MajorPlacementMode/PreferredJustification combination");
						return PopupPlacementMode.Auto;
				}
			case MajorPlacementMode.Left:
				switch (justification)
				{
					case PreferredJustification.VerticalCenter:
						return isLtr ? PopupPlacementMode.Left : PopupPlacementMode.Right;
					case PreferredJustification.Top:
						return isLtr ? PopupPlacementMode.LeftEdgeAlignedTop : PopupPlacementMode.RightEdgeAlignedTop;
					case PreferredJustification.Bottom:
						return isLtr ? PopupPlacementMode.LeftEdgeAlignedBottom : PopupPlacementMode.RightEdgeAlignedBottom;
					default:
						global::System.Diagnostics.Debug.Fail("Invalid MajorPlacementMode/PreferredJustification combination");
						return PopupPlacementMode.Auto;
				}
			case MajorPlacementMode.Right:
				switch (justification)
				{
					case PreferredJustification.VerticalCenter:
						return isLtr ? PopupPlacementMode.Right : PopupPlacementMode.Left;
					case PreferredJustification.Top:
						return isLtr ? PopupPlacementMode.RightEdgeAlignedTop : PopupPlacementMode.LeftEdgeAlignedTop;
					case PreferredJustification.Bottom:
						return isLtr ? PopupPlacementMode.RightEdgeAlignedBottom : PopupPlacementMode.LeftEdgeAlignedBottom;
					default:
						global::System.Diagnostics.Debug.Fail("Invalid MajorPlacementMode/PreferredJustification combination");
						return PopupPlacementMode.Auto;
				}
			default:
				global::System.Diagnostics.Debug.Fail("Invalid MajorPlacementMode");
				return PopupPlacementMode.Auto;
		}
	}

	private void OnPlacementTargetLayoutUpdated(object? sender, object args) => SetPositionFromPlacement();

	private void OnChildSizeChanged(object sender, SizeChangedEventArgs args) => SetPositionFromPlacement();

	private void OnChildLoaded(object sender, RoutedEventArgs args) => SetPositionFromPlacement();
}
