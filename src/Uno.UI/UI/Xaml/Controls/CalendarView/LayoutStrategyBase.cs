// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Contains common logic between the stacking and wrapping layout
// strategies.

using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class LayoutStrategyBase
	{
		#region Getters (UWP)
		protected float PointInNonVirtualizingDirection(Point point)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)point.Y;
				case Orientation.Vertical:
					return (float)point.X;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected float PointInVirtualizingDirection(Point point)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)point.X;
				case Orientation.Vertical:
					return (float)point.Y;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected float SizeInNonVirtualizingDirection(Size size)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)size.Height;
				case Orientation.Vertical:
					return (float)size.Width;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected float SizeInVirtualizingDirection(Size size)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)size.Width;
				case Orientation.Vertical:
					return (float)size.Height;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected float PointFromRectInNonVirtualizingDirection(Rect rect)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)rect.Y;
				case Orientation.Vertical:
					return (float)rect.X;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected float PointFromRectInVirtualizingDirection(Rect rect)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)rect.X;
				case Orientation.Vertical:
					return (float)rect.Y;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected float SizeFromRectInNonVirtualizingDirection(Rect rect)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)rect.Height;
				case Orientation.Vertical:
					return (float)rect.Width;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected float SizeFromRectInVirtualizingDirection(Rect rect)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					return (float)rect.Width;
				case Orientation.Vertical:
					return (float)rect.Height;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}
		#endregion

		#region Setters (UNO only)
		protected void SetPointInNonVirtualizingDirection(ref Point point, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					point.Y = value;
					break;
				case Orientation.Vertical:
					point.X = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected void SetPointInVirtualizingDirection(ref Point point, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					point.X = value;
					break;
				case Orientation.Vertical:
					point.Y = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected void SetSizeInNonVirtualizingDirection(ref Size size, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					size.Height = value;
					break;
				case Orientation.Vertical:
					size.Width = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected void SetSizeInVirtualizingDirection(ref Size size, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					size.Width = value;
					break;
				case Orientation.Vertical:
					size.Height = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected void SetPointFromRectInNonVirtualizingDirection(ref Rect rect, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					rect.Y = value;
					break;
				case Orientation.Vertical:
					rect.X = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected void SetPointFromRectInVirtualizingDirection(ref Rect rect, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					rect.X = value;
					break;
				case Orientation.Vertical:
					rect.Y = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected void SetSizeFromRectInNonVirtualizingDirection(ref Rect rect, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					rect.Height = value;
					break;
				case Orientation.Vertical:
					rect.Width = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}

		protected void SetSizeFromRectInVirtualizingDirection(ref Rect rect, float value)
		{
			switch (m_virtualizationDirection)
			{
				case Orientation.Horizontal:
					rect.Width = value;
					break;
				case Orientation.Vertical:
					rect.Height = value;
					break;
				default:
					throw new InvalidOperationException("XAML_FAIL_FAST");
			}
		}
		#endregion

		protected Size GetGroupPaddingAtStart()
		{
			return new Size((float)(m_groupPadding.Left), (float)(m_groupPadding.Top));
		}

		protected Size GetGroupPaddingAtEnd()
		{
			return new Size((float)(m_groupPadding.Right), (float)(m_groupPadding.Bottom));
		}

		//static
		protected static int GetRemainingGroups(
			int referenceGroupIndex,
			int totalGroups,
			RelativePosition positionOfReference)
		{
			global::System.Diagnostics.Debug.Assert(0 <= totalGroups);
			global::System.Diagnostics.Debug.Assert(0 <= referenceGroupIndex && referenceGroupIndex <= totalGroups);

			switch (positionOfReference)
			{
				case RelativePosition.Before:
					// Our reference is before the region we're counting
					// Notice no "count-1" here. Groups and containers after and including our reference are still considered valid traversal candidates
					return totalGroups - referenceGroupIndex;

				case RelativePosition.After:
					return referenceGroupIndex;

				default:
					return 0;
			}
		}

		//static
		protected static int GetRemainingItems(
			int referenceItemIndex,
			int totalItems,
			RelativePosition positionOfReference)
		{
			global::System.Diagnostics.Debug.Assert(0 <= totalItems);
			global::System.Diagnostics.Debug.Assert(0 <= referenceItemIndex && referenceItemIndex <= totalItems);

			switch (positionOfReference)
			{
				case RelativePosition.Before:
					// Our reference is before the region we're counting
					// Notice no "count-1" here. Groups and containers after and including our reference are still considered valid traversal candidates
					return totalItems - referenceItemIndex;

				case RelativePosition.After:
					return referenceItemIndex;

				default:
					return 0;
			}
		}

		// Determine if a point is inside the window, or is before or after it in the virtualizing direction.
		protected RelativePosition GetReferenceDirectionFromWindow(
			Rect referenceRect,
			Rect window)
		{
			float firstReferenceEdge = PointFromRectInVirtualizingDirection(referenceRect);
			float lastReferenceEdge = firstReferenceEdge + SizeFromRectInVirtualizingDirection(referenceRect);
			float firstWindowEdge = PointFromRectInVirtualizingDirection(window);
			float lastWindowEdge = firstWindowEdge + SizeFromRectInVirtualizingDirection(window);

			RelativePosition result;

			if (lastReferenceEdge < firstWindowEdge)
			{
				result = RelativePosition.Before;
			}
			else if (lastWindowEdge < firstReferenceEdge)
			{
				result = RelativePosition.After;
			}
			else
			{
				result = RelativePosition.Inside;
			}

			return result;
		}
	}
}
