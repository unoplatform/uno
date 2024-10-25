using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	internal static class ToolTipPositioning
	{
		// UNO TODO
		internal static bool IsLefthandedUser() => true;

		private static Size ConstrainSize(
			Size size,
			double xMax,
			double yMax)
		{
			// Apply constraint
			var sizeShrunk = size;

			// Calc horizontal constraint
			if (size.Width > xMax)
			{
				sizeShrunk.Width = xMax;
			}
			// Calc vertical constraint
			if (size.Height > yMax)
			{
				sizeShrunk.Height = yMax;
			}
			return sizeShrunk;
		}

		private static Rect HorizontallyCenterRect(
			Rect container,
			Rect rcToCenter)
		{
			var dx = ((container.Left - rcToCenter.Left) + (container.Right - rcToCenter.Right)) / 2;
			var rcCentered = rcToCenter.OffsetRect(dx, 0);
			return rcCentered;
		}

		private static Rect VerticallyCenterRect(
				Rect container,
				Rect rcToCenter)
		{
			var dy = ((container.Top - rcToCenter.Top) + (container.Bottom - rcToCenter.Bottom)) / 2;
			var rcCentered = rcToCenter.OffsetRect(0, dy);
			return rcCentered;
		}

		private static Rect MoveNearRect(
			Rect rcWindow,
			Rect rcWindowToTract,
			PlacementMode nSide)
		{
			var ptOffset = default(Point);
			switch (nSide)
			{
				case PlacementMode.Left:
					ptOffset.Y = 0;
					ptOffset.X = rcWindowToTract.Left - rcWindow.Right;
					break;
				case PlacementMode.Right:
					ptOffset.Y = 0;
					ptOffset.X = rcWindowToTract.Right - rcWindow.Left;
					break;
				case PlacementMode.Top:
					ptOffset.Y = rcWindowToTract.Top - rcWindow.Bottom;
					ptOffset.X = 0;
					break;
				case PlacementMode.Bottom:
					ptOffset.Y = rcWindowToTract.Bottom - rcWindow.Top;
					ptOffset.X = 0;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			var rcOffset = rcWindow.OffsetRect(ptOffset);
			return rcOffset;
		}

		private static bool IsContainedInRect(
			Rect container,
			Rect rc)
		{
			bool isContained = true;
			if (rc.Left > rc.Right || rc.Top > rc.Bottom)
			{
				typeof(ToolTipPositioning).Log().LogError("This rect is ill formed.", false);
				isContained = false;
			}
			if (rc.Left < container.Left || rc.Right > container.Right || rc.Top < container.Top || rc.Bottom > container.Bottom)
			{
				isContained = false;
			}

			return isContained;
		}

		private static Rect MoveRectToPoint(
			Rect rc,
			double x,
			double y)
		{
			var rcMoved = rc.OffsetRect(x - rc.Left, y - rc.Top);
			return rcMoved;
		}

		private static Rect ShiftRectIntoContainer(
				Rect container,
				Rect rcToShift)
		{
			Debug.Assert((rcToShift.Width <= container.Width) && (rcToShift.Height <= container.Height),
				"A rect must be fit in a container in order to be shifted into it");

			var rcShifted = rcToShift;
			if (rcShifted.Left < container.Left)
			{
				rcShifted = MoveRectToPoint(rcShifted, container.Left, rcShifted.Top);
			}
			else if (rcShifted.Right > container.Right)
			{
				rcShifted = MoveRectToPoint(rcShifted, container.Right - rcShifted.Width, rcShifted.Top);
			}

			if (rcShifted.Top < container.Top)
			{
				rcShifted = MoveRectToPoint(rcShifted, rcShifted.Left, container.Top);
			}
			else if (rcShifted.Bottom > container.Bottom)
			{
				rcShifted = MoveRectToPoint(rcShifted, rcShifted.Left, container.Bottom - rcShifted.Height);
			}
			Debug.Assert(IsContainedInRect(container, rcShifted));
			return rcShifted;
		}

		private static bool CanPositionRelativeOnSide(
					Rect windowToTrack,
					Rect rcWindow,
					PlacementMode nSide,
					Rect constraint)
		{
			var nAvailableWidth = 0d;
			var nAvailableHeight = 0d;
			switch (nSide)
			{
				case PlacementMode.Left:
					{
						nAvailableWidth = windowToTrack.Left - constraint.Left;
						nAvailableHeight = constraint.Height;
					}
					break;

				case PlacementMode.Top:
					{
						nAvailableWidth = constraint.Width;
						nAvailableHeight = windowToTrack.Top - constraint.Top;
					}
					break;

				case PlacementMode.Right:
					{
						nAvailableWidth = constraint.Right - windowToTrack.Right;
						nAvailableHeight = constraint.Height;
					}
					break;

				case PlacementMode.Bottom:
					{
						nAvailableWidth = constraint.Width;
						nAvailableHeight = constraint.Bottom - windowToTrack.Bottom;
					}
					break;
			}
			return ((nAvailableWidth >= rcWindow.Width) &&
					(nAvailableHeight >= rcWindow.Height));
		}

		private static Rect PositionRelativeOnSide(
			Rect windowToTrack,
			Rect rcWindow,
			PlacementMode nSide,
			Rect constraint)
		{
			var rcAdjusted = default(Rect);
			switch (nSide)
			{
				case PlacementMode.Left:
				case PlacementMode.Right:
					{
						rcAdjusted = VerticallyCenterRect(windowToTrack, MoveNearRect(rcWindow, windowToTrack, nSide));
					}
					break;

				case PlacementMode.Top:
				case PlacementMode.Bottom:
					{
						rcAdjusted = HorizontallyCenterRect(windowToTrack, MoveNearRect(rcWindow, windowToTrack, nSide));
					}
					break;
			}

			if (!IsContainedInRect(constraint, rcAdjusted))
			{
				rcAdjusted = ShiftRectIntoContainer(constraint, rcAdjusted);
			}
			return rcAdjusted;
		}

		// Adjusts a window to use relative positioning.
		// -  constraint is the rectangle which should fully contain the Flyout, in screen coordinates
		// -  sizeFlyout is the proposed SIZE of the Flyout, in screen coordinates
		// -  rcDockTo is the RECT Flyout should not obscure, in screen coordinates
		// -  sidePreferred is the side the Flyout should appear on
		//
		// Returns a RECT which should be used for the Flyout, in screen coordinates
		internal static (Rect rect, PlacementMode sideChosen) QueryRelativePosition(
			Rect constraint,
			Size sizeFlyout,
			Rect rcDockTo,
			PlacementMode nSidePreferred
		)
		{
			var typographic = constraint;
			var nSideChosen = ToolTip.DefaultPlacementMode;

			var sizeUnit = ConstrainSize(sizeFlyout, typographic.Width, typographic.Height);

			var rcFlyout = new Rect(0, 0, sizeUnit.Width, sizeUnit.Height);

			var constraintDockTo = rcDockTo;

			var rcAdjusted = default(Rect);
			if (CanPositionRelativeOnSide(constraintDockTo, rcFlyout, nSidePreferred, typographic))
			{
				rcAdjusted = PositionRelativeOnSide(constraintDockTo, rcFlyout, nSidePreferred, typographic);
				nSideChosen = nSidePreferred;
			}
			else
			{
				// fTriedRight isnt needed since if we know the other three are true then it must be that
				// we didnt try the right side
				bool fTriedLeft = false;
				bool fTriedAbove = false;
				bool fTriedBelow = false;

				var nSideOpposite = PlacementMode.Top;
				switch (nSidePreferred)
				{
					case PlacementMode.Top:
						nSideOpposite = PlacementMode.Bottom;
						fTriedAbove = true;
						fTriedBelow = true;
						break;

					case PlacementMode.Bottom:
						nSideOpposite = PlacementMode.Top;
						fTriedAbove = true;
						fTriedBelow = true;
						break;

					case PlacementMode.Left:
						nSideOpposite = PlacementMode.Right;
						fTriedLeft = true;
						break;

					case PlacementMode.Right:
						nSideOpposite = PlacementMode.Left;
						fTriedLeft = true;
						break;
				}

				if (CanPositionRelativeOnSide(constraintDockTo, rcFlyout, nSideOpposite, typographic))
				{
					rcAdjusted = PositionRelativeOnSide(constraintDockTo, rcFlyout, nSideOpposite, typographic);
					nSideChosen = nSideOpposite;
				}
				else
				{
					PlacementMode nSideAxialBest;
					if ((nSidePreferred == PlacementMode.Left) || (nSidePreferred == PlacementMode.Right))
					{
						nSideAxialBest = PlacementMode.Top;
						fTriedAbove = true;
					}
					else
					{
						if (IsLefthandedUser())
						{
							nSideAxialBest = PlacementMode.Right;
						}
						else
						{
							nSideAxialBest = PlacementMode.Left;
							fTriedLeft = true;
						}
					}

					if (CanPositionRelativeOnSide(constraintDockTo, rcFlyout, nSideAxialBest, typographic))
					{
						rcAdjusted = PositionRelativeOnSide(constraintDockTo, rcFlyout, nSideAxialBest, typographic);
						nSideChosen = nSideAxialBest;
					}
					else
					{
						PlacementMode nSideRemaining;
						if (!fTriedAbove)
						{
							nSideRemaining = PlacementMode.Top;
						}
						else if (!fTriedBelow)
						{
							nSideRemaining = PlacementMode.Bottom;
						}
						else if (!fTriedLeft)
						{
							nSideRemaining = PlacementMode.Left;
						}
						else
						{
							nSideRemaining = PlacementMode.Right;
						}

						if (CanPositionRelativeOnSide(constraintDockTo, rcFlyout, nSideRemaining, typographic))
						{
							rcAdjusted = PositionRelativeOnSide(constraintDockTo, rcFlyout, nSideRemaining, typographic);
							nSideChosen = nSideRemaining;
						}
						else
						{
							switch (nSidePreferred)
							{
								case PlacementMode.Left:
									{
										var pt = default(Point);
										pt.X = typographic.Left;
										pt.Y = rcFlyout.Top;
										rcAdjusted = MoveRectToPoint(rcFlyout, pt.X, pt.Y);
										rcAdjusted = VerticallyCenterRect(constraintDockTo, rcAdjusted);
									}
									break;

								case PlacementMode.Top:
									{
										var pt = default(Point);
										pt.X = rcFlyout.Left;
										pt.Y = typographic.Top;
										rcAdjusted = MoveRectToPoint(rcFlyout, pt.X, pt.Y);
										rcAdjusted = HorizontallyCenterRect(constraintDockTo, rcAdjusted);
									}
									break;

								case PlacementMode.Right:
									{
										var pt = default(Point);
										pt.X = typographic.Right - rcFlyout.Width;
										pt.Y = rcFlyout.Top;
										rcAdjusted = MoveRectToPoint(rcFlyout, pt.X, pt.Y);
										rcAdjusted = VerticallyCenterRect(constraintDockTo, rcAdjusted);
									}
									break;

								case PlacementMode.Bottom:
									{
										var pt = default(Point);
										pt.X = rcFlyout.Left;
										pt.Y = typographic.Bottom - rcFlyout.Height;
										rcAdjusted = MoveRectToPoint(rcFlyout, pt.X, pt.Y);
										rcAdjusted = HorizontallyCenterRect(constraintDockTo, rcAdjusted);
									}
									break;
							}

							rcAdjusted = ShiftRectIntoContainer(typographic, rcAdjusted);
							nSideChosen = nSidePreferred;
						}
					}
				}
			}

			return (rcAdjusted, nSideChosen);
		}
	}
}
