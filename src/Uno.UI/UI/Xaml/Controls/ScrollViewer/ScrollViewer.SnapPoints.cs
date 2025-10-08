using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		//private const float ScrollViewerSnapPointLocationTolerance = 0.0001f;

		private IScrollSnapPointsInfo _snapPointsInfo;

		internal void AdjustOffsetsForSnapPoints(ref double? horizontalOffset, ref double? verticalOffset, float? zoomFactor, bool canBypassSingle = false)
		{
			if (horizontalOffset is { } hOffset)
			{
				var viewportPixelWidth = AdjustPixelViewportDim(ViewportWidth);
				var maxOffset = Math.Max(0d, ExtentWidth - viewportPixelWidth);
				var currentOffset = canBypassSingle ? hOffset : HorizontalOffset;

				AdjustOffsetWithMandatorySnapPoints(
					isForHorizontalOffset: true,
					minOffset: 0d,
					maxOffset: maxOffset,
					currentOffset,
					ExtentWidth,
					viewportPixelWidth,
					zoomFactor ?? ZoomFactor,
					ref hOffset);
				horizontalOffset = hOffset;
			}

			if (verticalOffset is { } vOffset)
			{
				var viewportPixelHeight = AdjustPixelViewportDim(ViewportHeight);
				var maxOffset = Math.Max(0d, ExtentHeight - viewportPixelHeight);
				var currentOffset = canBypassSingle ? vOffset : VerticalOffset;

				AdjustOffsetWithMandatorySnapPoints(
					isForHorizontalOffset: false,
					minOffset: 0d,
					maxOffset: maxOffset,
					currentOffset,
					ExtentHeight,
					viewportPixelHeight,
					zoomFactor ?? ZoomFactor,
					ref vOffset);
				verticalOffset = vOffset;
			}

#if __SKIA__
			(horizontalOffset, verticalOffset) = ClampOffsetsToFocusedTextBox(horizontalOffset, verticalOffset);
#endif
		}

		private double AdjustPixelViewportDim(double pixelViewportDim)
		{
			// Round to the closest lower integer.
			// +3.000 --> +3.0
			// +8.731 --> +8.0
			// +5.999 --> +5.0
			return (double)(long)pixelViewportDim;
		}

		internal partial bool ShouldSnapToTouchTextBox();
#if !__SKIA__
		internal partial bool ShouldSnapToTouchTextBox() => false;
#endif

		private void AdjustOffsetWithMandatorySnapPoints(
			bool isForHorizontalOffset,
			double minOffset,
			double maxOffset,
			double currentOffset,
			double targetExtentDimension,
			double viewportDimension,
			float targetZoomFactor,
			ref double targetOffset)
		{
			if (_snapPointsInfo == null)
			{
				return;
			}

			var snapPointsType = isForHorizontalOffset ? HorizontalSnapPointsType : VerticalSnapPointsType;

			if (snapPointsType != SnapPointsType.Mandatory && snapPointsType != SnapPointsType.MandatorySingle)
			{
				// Scroll snap points are not mandatory.
				return;
			}

			GetScrollSnapPoints(
				isForHorizontalOffset,
				snapPointsType,
				targetZoomFactor,
				targetZoomFactor,
				targetExtentDimension,
				viewportDimension,
				out var areSnapPointsOptional,
				out _,
				out var areSnapPointsRegular,
				out var regularOffset,
				out var regularInterval,
				out var irregularSnapPoints);


			global::System.Diagnostics.Debug.Assert(!areSnapPointsOptional);

			var useCurrentOffsetForMandatorySingleSnapPoints =
				snapPointsType == SnapPointsType.MandatorySingle &&
				targetOffset != currentOffset;
			var signFactor = (targetOffset > currentOffset) ? 1.0 : -1.0;

			var closestSnapPoint = 0d;
			var smallestDistance = double.MaxValue;

			if (areSnapPointsRegular && regularInterval > 0)
			{
				// There are regular snap points. Determine the closest one to *pTargetOffset when snapPointsType==SnapPointsType_Mandatory,
				// and the closest one to currentOffset when snapPointsType==SnapPointsType_MandatorySingle.
				if (useCurrentOffsetForMandatorySingleSnapPoints)
				{
					targetOffset = currentOffset + signFactor * regularInterval;
				}

				//if (snapCoordinate == DMSnapCoordinateMirrored)
				//{
				//	// Far alignment
				//	closestSnapPoint = targetExtentDim - viewportDim - regularOffset - DoubleUtil::Round((targetExtentDim - viewportDim - *pTargetOffset) / regularInterval, 0 /*numDecimalPlaces*/) * regularInterval;
				//	if (closestSnapPoint < 0.0)
				//	{
				//		// Handle attempt to move prior to first snap point
				//		closestSnapPoint = regularInterval - viewportDim;
				//	}
				//}
				//else
				{
					// Near and Center alignments
					if (targetOffset <= regularOffset)
					{
						closestSnapPoint = regularOffset;
					}
					else
					{
						closestSnapPoint = Math.Round((targetOffset - regularOffset) / regularInterval, 0 /*numDecimalPlaces*/) * regularInterval + regularOffset;
					}
					if (closestSnapPoint > maxOffset)
					{
						// Handle attempt to move past the last snap point
						closestSnapPoint -= regularInterval;
					}
				}
				if (closestSnapPoint >= minOffset && closestSnapPoint <= maxOffset)
				{
					targetOffset = closestSnapPoint;
				}
			}
			else if (irregularSnapPoints?.Count > 0)
			{
				// There are irregular snap points. Determine the closest one to *pTargetOffset when snapPointsType==SnapPointsType_Mandatory,
				// and the closest one to currentOffset when snapPointsType==SnapPointsType_MandatorySingle.
				if (useCurrentOffsetForMandatorySingleSnapPoints)
				{
					// When *pTargetOffset > currentOffset:
					//   Target offset is after current offset. First try to find a snap point located after currentOffset within the acceptable boundaries.
					//   Second when no snap point after currentOffset was within the acceptable boundaries. Select a snap point before currentOffset instead.
					// When *pTargetOffset < currentOffset:
					//   Target offset is before current offset. First try to find a snap point located before currentOffset within the acceptable boundaries.
					//   Second when no snap point before currentOffset was within the acceptable boundaries. Select a snap point after currentOffset instead.
					for (var iIrregularSnapPoint = 0; iIrregularSnapPoint < irregularSnapPoints.Count; iIrregularSnapPoint++)
					{
						var snapPoint = irregularSnapPoints[iIrregularSnapPoint];

						if (snapPoint >= minOffset &&
							snapPoint <= maxOffset &&
							signFactor * (snapPoint - currentOffset) > 0.0 &&
							signFactor * (snapPoint - currentOffset) < smallestDistance)
						{
							smallestDistance = signFactor * (snapPoint - currentOffset);
							closestSnapPoint = snapPoint;
						}
					}
					if (smallestDistance == double.MaxValue)
					{
						for (var iIrregularSnapPoint = 0; iIrregularSnapPoint < irregularSnapPoints.Count && smallestDistance != 0.0; iIrregularSnapPoint++)
						{
							var snapPoint = irregularSnapPoints[iIrregularSnapPoint];

							if (snapPoint >= minOffset &&
								snapPoint <= maxOffset &&
								signFactor * (currentOffset - snapPoint) >= 0.0 &&
								signFactor * (currentOffset - snapPoint) < smallestDistance)
							{
								smallestDistance = signFactor * (currentOffset - snapPoint);
								closestSnapPoint = snapPoint;
							}
						}
					}
				}
				else
				{
					for (var iIrregularSnapPoint = 0; iIrregularSnapPoint < irregularSnapPoints.Count; iIrregularSnapPoint++)
					{
						if (irregularSnapPoints[iIrregularSnapPoint] >= minOffset &&
							irregularSnapPoints[iIrregularSnapPoint] <= maxOffset &&
							Math.Abs(targetOffset - irregularSnapPoints[iIrregularSnapPoint]) < smallestDistance)
						{
							smallestDistance = Math.Abs(targetOffset - irregularSnapPoints[iIrregularSnapPoint]);
							closestSnapPoint = irregularSnapPoints[iIrregularSnapPoint];
						}
					}
				}
				targetOffset = closestSnapPoint;
			}
		}

		private void GetScrollSnapPoints(
			bool isForHorizontalSnapPoints,
			SnapPointsType snapPointsType,
			float zoomFactor,
			float staticZoomFactor,
			double targetExtentDimension,
			double viewportDimension,
			out bool areSnapPointsOptional,
			out bool areSnapPointsSingle,
			out bool areSnapPointsRegular,
			out float regularOffset,
			out float regularInterval,
			out IReadOnlyList<float> resultSnapPoints)
		{
			var shouldProcessSnapPoint = false;
			SnapPointsAlignment alignment = default;
			float offset = 0;
			float interval = 0;
			IReadOnlyList<float> irregularSnapPoints = default;

			areSnapPointsOptional = default;
			areSnapPointsSingle = default;
			regularOffset = default;
			regularInterval = default;
			resultSnapPoints = default;

			if (isForHorizontalSnapPoints)
			{
				if (snapPointsType != SnapPointsType.None)
				{
					alignment = HorizontalSnapPointsAlignment;
					areSnapPointsRegular = _snapPointsInfo.AreHorizontalSnapPointsRegular;
					if (areSnapPointsRegular)
					{
						interval = _snapPointsInfo.GetRegularSnapPoints(Orientation.Horizontal, alignment, out offset);
						irregularSnapPoints = null;
					}
					else
					{
						if (targetExtentDimension < 0)
						{
							// TODO
						}

						// TODO: cache this call
						irregularSnapPoints = _snapPointsInfo.GetIrregularSnapPoints(Orientation.Horizontal, alignment);
						regularInterval = 0;
						regularOffset = 0;
					}

					shouldProcessSnapPoint = true;
				}
				else
				{
					areSnapPointsRegular = false;
				}
			}
			else
			{
				if (snapPointsType != SnapPointsType.None)
				{
					alignment = VerticalSnapPointsAlignment;
					areSnapPointsRegular = _snapPointsInfo.AreVerticalSnapPointsRegular;
					if (areSnapPointsRegular)
					{
						interval = _snapPointsInfo.GetRegularSnapPoints(Orientation.Vertical, alignment, out offset);
						irregularSnapPoints = null;
					}
					else
					{
						if (targetExtentDimension < 0)
						{
							// TODO
						}

						// TODO: cache this call
						irregularSnapPoints = _snapPointsInfo.GetIrregularSnapPoints(Orientation.Vertical, alignment);
						regularInterval = 0;
						regularOffset = 0;
					}

					shouldProcessSnapPoint = true;
				}
				else
				{
					areSnapPointsRegular = false;
				}
			}

			if (shouldProcessSnapPoint)
			{
				if (areSnapPointsRegular)
				{
					switch (alignment)
					{
						case SnapPointsAlignment.Near:
							regularOffset = offset * staticZoomFactor;
							break;
						case SnapPointsAlignment.Center:
							// When snap points alignment is Center, the snap points need to align
							// with the center of the viewport. Adjust the offset accordingly.
							// Both static and manipulatable zoom factors need to be taken into account.
							if (interval <= 0f)
							{
								// Do not handle negative interval in this case
								interval = 0f;
							}
							else
							{
								if (viewportDimension >= interval * zoomFactor)
								{
									offset *= zoomFactor;
									offset -= (float)(viewportDimension / 2f);
									if (staticZoomFactor == 1f)
									{
										offset /= zoomFactor;
									}

									while (offset < 0)
									{
										offset += interval * staticZoomFactor;
									}
								}
								else
								{
									offset -= (float)(viewportDimension / (2f * zoomFactor));
									offset *= staticZoomFactor;
								}

								regularOffset = offset;
							}

							break;
						case SnapPointsAlignment.Far:
							regularOffset = offset * staticZoomFactor;
							break;
					}

					regularInterval = interval * staticZoomFactor;
					areSnapPointsRegular = true;
				}
				else
				{
					areSnapPointsRegular = false;

					resultSnapPoints = CopyMotionSnapPoints(
						false,
						irregularSnapPoints,
						alignment,
						viewportDimension,
						targetExtentDimension,
						zoomFactor,
						staticZoomFactor);
				}

				areSnapPointsOptional =
					snapPointsType == SnapPointsType.Optional ||
					snapPointsType == SnapPointsType.OptionalSingle;
			}
		}

		private IReadOnlyList<float> CopyMotionSnapPoints(
			bool isForZoomSnapPoints,
			IReadOnlyList<float> snapPoints,
			SnapPointsAlignment alignment,
			double viewportDimension,
			double extentDimension,
			float zoomFactor,
			float staticZoomFactor)
		{
			global::System.Diagnostics.Debug.Assert(staticZoomFactor == 1f || staticZoomFactor == zoomFactor);

			if (snapPoints is null)
			{
				return null;
			}

			var result = new List<float>(snapPoints.Count);

			if (snapPoints.Count > 0)
			{
				foreach (var sp in snapPoints)
				{
					var snapPoint = sp;

					if (isForZoomSnapPoints)
					{
						result.Add(snapPoint * staticZoomFactor);
					}
					else
					{
						// When snap points alignment is Center or Far, the irregular snap points need
						// to be adjusted based on the static or manipulatable zoom factor. In the Near case
						// it doesn't matter so do it for consistency.
						snapPoint *= zoomFactor;

						// Adjust snap point to relative(near/center/far) viewport offset, and then
						// clamp it within valid scroll range.
						var adjustedOffset = alignment switch
						{
							SnapPointsAlignment.Near => snapPoint,
							SnapPointsAlignment.Center => (float)(snapPoint - viewportDimension / 2),
							SnapPointsAlignment.Far => (float)(snapPoint - viewportDimension),
							_ => throw new IndexOutOfRangeException("alignment")
						};
						var clampedOffset = (float)Math.Max(0, Math.Min(adjustedOffset, ScrollableWidth));

						if (staticZoomFactor == 1.0f)
						{
							clampedOffset /= zoomFactor;
						}
						result.Add(clampedOffset);
					}
				}
			}

			return result;
		}
	}
}
