// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_SnapPoints.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal bool AreHorizontalSnapPointsRegular => m_snapPointState.AreSnapPointsRegular(Orientation.Horizontal);

	internal bool AreVerticalSnapPointsRegular => m_snapPointState.AreSnapPointsRegular(Orientation.Vertical);

	internal IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		=> m_snapPointState.GetIrregularSnapPoints(orientation, alignment);

	internal void GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset, out float spacing)
		=> m_snapPointState.GetRegularSnapPoints(orientation, alignment, out offset, out spacing);

	private void UpdateSnapPoints()
	{
		Orientation orientation;
		bool hasRegularSnapPoints = false;
		float nearOffset = 0.0f;
		float farOffset = 0.0f;
		float regularSpacing = 0.0f;
		bool snapPointsChanged = false;

		orientation = m_spLayoutStrategy.VirtualizationDirection;

		m_spLayoutStrategy.GetRegularSnapPoints(out nearOffset, out farOffset, out regularSpacing, out hasRegularSnapPoints);

		if (hasRegularSnapPoints)
		{
			snapPointsChanged = m_snapPointState.SetRegularSnapPoints(orientation, regularSpacing, nearOffset, farOffset);
		}
		else
		{
			// Time to loop and tally up the irregular snap points
			List<float>[] rawPoints = new List<float>[ElementType_Count];
			List<float> mergedPoints = new();

			// Use transform to mutate the Rects into FLOATs
			for (int rawPointsIndex = 0; rawPointsIndex < m_arrangeRects.Length; ++rawPointsIndex)
			{
				List<Rect> original = m_arrangeRects[rawPointsIndex];
				List<float> result = new(original.Count + 1);

				// Copy over the element of interest, skipping duplicates
				foreach (Rect rect in original)
				{
					float point = orientation == Orientation.Horizontal ? (float)rect.X : (float)rect.Y;
					if (result.Count == 0 || !DoubleUtil.AreClose(result[result.Count - 1], point))
					{
						result.Add(point);
					}
				}

				// Grab the far edge of the last one
				if (original.Count != 0)
				{
					Rect last = original[original.Count - 1];
					if (orientation == Orientation.Horizontal)
					{
						if (!DoubleUtil.AreClose(0, last.Width))
						{
							result.Add((float)(last.X + last.Width));
						}
					}
					else
					{
						if (!DoubleUtil.AreClose(0, last.Height))
						{
							result.Add((float)(last.Y + last.Height));
						}
					}
				}

				rawPoints[rawPointsIndex] = result;
			}

			// Since we're getting the starting points plus the end of the last element, we will have an end point for both headers and containers
			// We only want one, so trim off the extra
			List<float> headerPoints = rawPoints[(int)ElementType.GroupHeader];
			List<float> containerPoints = rawPoints[(int)ElementType.ItemContainer];
			if (headerPoints.Count != 0 && containerPoints.Count != 0)
			{
				if (headerPoints[headerPoints.Count - 1] <= containerPoints[containerPoints.Count - 1])
				{
					headerPoints.RemoveAt(headerPoints.Count - 1);
				}
				else
				{
					containerPoints.RemoveAt(containerPoints.Count - 1);
				}
			}

			// Merge the two
			mergedPoints.Capacity = headerPoints.Count + containerPoints.Count;
			int headerIndex = 0;
			int containerIndex = 0;
			while (headerIndex < headerPoints.Count && containerIndex < containerPoints.Count)
			{
				if (containerPoints[containerIndex] < headerPoints[headerIndex])
				{
					mergedPoints.Add(containerPoints[containerIndex++]);
				}
				else
				{
					mergedPoints.Add(headerPoints[headerIndex++]);
				}
			}
			while (headerIndex < headerPoints.Count)
			{
				mergedPoints.Add(headerPoints[headerIndex++]);
			}
			while (containerIndex < containerPoints.Count)
			{
				mergedPoints.Add(containerPoints[containerIndex++]);
			}

			snapPointsChanged = m_snapPointState.SetIrregularSnapPoints(orientation, mergedPoints);
		}

		// If the snap points changed, notify listeners
		if (snapPointsChanged)
		{
			if (orientation == Orientation.Horizontal)
			{
				// Raise the event
				HorizontalSnapPointsChanged?.Invoke(this, null!);
			}
			else
			{
				// Raise the event
				VerticalSnapPointsChanged?.Invoke(this, null!);
			}
		}
	}

	internal partial class SnapPointState
	{
		internal SnapPointState()
		{
			m_snapPointsSet = false;
			m_isRegular = false;
			m_orientation = Orientation.Horizontal;
			m_irregularSnapPointKeys = new();
			m_regularSpacing = 0.0f;
			m_nearOffset = 0.0f;
			m_farOffset = 0.0f;
		}

		internal bool SetIrregularSnapPoints(Orientation orientation, List<float> keys)
		{
			bool changed = !m_snapPointsSet || m_isRegular ||
				(m_orientation != orientation) || !m_irregularSnapPointKeys.SequenceEqual(keys);

			m_snapPointsSet = true;
			m_orientation = orientation;
			m_isRegular = false;

			m_irregularSnapPointKeys = keys;

			return changed;
		}

		internal IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			IReadOnlyList<float>? returnValue = null;

			if (orientation == m_orientation)
			{
				List<float>? spSnapPointsVTV = null;

				if (m_isRegular)
				{
					throw new InvalidOperationException();
				}

				// We're assuming that we have both a beginning and end snap point to handle both alignments
				// Any particular alignment request will only return one of the endpoints
				if (m_irregularSnapPointKeys.Count >= 2)
				{
					switch (alignment)
					{
						case SnapPointsAlignment.Near:
							// Grab all the front edges
							spSnapPointsVTV = m_irregularSnapPointKeys.GetRange(0, m_irregularSnapPointKeys.Count - 1);
							break;

						case SnapPointsAlignment.Center:
							{
								// Need to grab the middle of each piece
								List<float> tempResult = new(m_irregularSnapPointKeys.Count);

								// adjacent_difference by default fills the output with the first element, followed
								// by the difference of each of the adjacent elements. We're going to override
								// the operation with a lambda to fill with the average of each pair
								for (int i = 0; i < m_irregularSnapPointKeys.Count; ++i)
								{
									if (i == 0)
									{
										tempResult.Add(m_irregularSnapPointKeys[0]);
									}
									else
									{
										tempResult.Add((m_irregularSnapPointKeys[i] + m_irregularSnapPointKeys[i - 1]) / 2);
									}
								}

								// Hooray! Now we just copy this into the view, skipping the first item because it's just
								// a copy of the first point
								spSnapPointsVTV = tempResult.GetRange(1, tempResult.Count - 1);
							}
							break;

						case SnapPointsAlignment.Far:
							// Grab all the back edges
							spSnapPointsVTV = m_irregularSnapPointKeys.GetRange(1, m_irregularSnapPointKeys.Count - 1);
							break;
					}
				}

				returnValue = spSnapPointsVTV;
			}

			// IScrollSnapPointsInfo callers treat null as "no snap points", as in WinUI.
			return returnValue!;
		}

		internal bool SetRegularSnapPoints(Orientation orientation, float spacing, float nearOffset, float farOffset)
		{
			bool changed = !m_snapPointsSet || !m_isRegular || (m_orientation != orientation) ||
				(m_regularSpacing != spacing) || (m_nearOffset != nearOffset) || (m_farOffset != farOffset);
			m_snapPointsSet = true;
			m_regularSpacing = spacing;
			m_nearOffset = nearOffset;
			m_farOffset = farOffset;
			m_isRegular = true;
			m_orientation = orientation;

			return changed;
		}

		internal void GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset, out float spacing)
		{
			offset = 0.0f;
			spacing = 0.0f;

			if (orientation == m_orientation)
			{
				if (!m_isRegular)
				{
					throw new InvalidOperationException();
				}

				switch (alignment)
				{
					case SnapPointsAlignment.Near:
						offset = m_nearOffset;
						break;

					case SnapPointsAlignment.Center:
						offset = m_nearOffset + (m_regularSpacing / 2);
						break;

					case SnapPointsAlignment.Far:
						offset = m_farOffset;
						break;
				}

				spacing = m_regularSpacing;
			}
		}
	}
}
