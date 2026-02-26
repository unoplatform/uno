// MUX Reference ItemIndexRangeHelper.cpp

using System.Collections.Generic;

namespace DirectUI.Components;

internal static partial class ItemIndexRangeHelper
{
	#region helper functions
	// used to check if an index is within a range defined by a start and an end
	internal static bool IndexInRange(
		int startIndex,
		int endIndex,
		int index)
	{
		return (index >= startIndex && index <= endIndex);
	}

	// used to check if two ranges are equal
	internal static bool AreRangesEqual(
		int startIndex1,
		uint length1,
		int startIndex2,
		uint length2)
	{
		return (startIndex1 == startIndex2 && length1 == length2);
	}

	// returns the length of the continuous indices starting at the given index
	internal static uint GetContinousIndicesLengthStartingAtIndex(
		IList<uint> indices,
		int startIndex)
	{
		var size = indices.Count;
		uint startValue = indices[startIndex];
		uint length = 1;

		for (int i = startIndex + 1; i < size; ++i, ++length)
		{
			if (indices[i] - startValue != length)
			{
				break;
			}
		}

		return length;
	}

	// used to check the location of an index with respect to a range defined by a start and an end
	internal static IndexLocation IndexWithRespectToRange(
		int startIndex,
		int endIndex,
		int index)
	{
		if (index < startIndex)
		{
			return IndexLocation.Before;
		}

		if (index > endIndex)
		{
			return IndexLocation.After;
		}

		return IndexLocation.Inside;
	}

	// used to check if range1 is inside range2
	internal static bool IsFirstRangeInsideSecondRange(
		int startIndex1,
		uint length1,
		int startIndex2,
		uint length2)
	{
		int endIndex2 = (int)(startIndex2 + length2 - 1);

		return IndexInRange(startIndex2, endIndex2, startIndex1) && IndexInRange(startIndex2, endIndex2, (int)(startIndex1 + length1 - 1));
	}

	// used to check if range1 is adjacent to range2
	// i.e. end of range1 is start of range2 - 1
	internal static bool IsFirstRangeAdjacentToSecondRange(
		int startIndex1,
		uint length1,
		int startIndex2)
	{
		return (startIndex1 + length1 == startIndex2);
	}
	#endregion

	#region range selection

	internal partial class RangeSelection
	{
		internal void ItemInsertedAt(int index)
		{
			bool found = false;
			int rangeIndex = 0;

			FindIndexInSelectedRanges(index, out rangeIndex, out found);

			// if the index is inside the range, then we split the range into two (if possible)
			if (found)
			{
				SplitRangeAt(index, true /* forInsertion */, ref rangeIndex);
			}

			// we go through all the ranges that come after the index and increment their firstIndex by 1
			for (var itr = rangeIndex + 1; itr < m_selectedRanges.Count; ++itr)
			{
				m_selectedRanges[itr] = new Range(m_selectedRanges[itr].FirstIndex + 1, m_selectedRanges[itr].Length);
			}
		}

		internal void ItemRemovedAt(int index)
		{
			FindIndexInSelectedRanges(index, out var rangeIndex, out var found);

			// if the index is inside the range, then we decrement the length by 1
			if (found)
			{
				m_selectedRanges[rangeIndex] = new Range(m_selectedRanges[rangeIndex].FirstIndex, m_selectedRanges[rangeIndex].Length - 1);
				if (m_selectedRanges[rangeIndex].Length == 0)
				{
					m_selectedRanges.RemoveAt(rangeIndex);
					--rangeIndex;
				}
			}
			else if (rangeIndex >= 0 && (uint)(rangeIndex + 1) < m_selectedRanges.Count)
			{
				Range currentRange = m_selectedRanges[rangeIndex];
				Range nextRange = m_selectedRanges[rangeIndex + 1];

				// check if the removed item happened to be between two ranges
				// that makes them adjacent ranges -> merge into one
				if (IsFirstRangeAdjacentToSecondRange(currentRange.FirstIndex, currentRange.Length, nextRange.FirstIndex - 1))
				{
					currentRange = new Range(currentRange.FirstIndex, currentRange.Length + nextRange.Length);
					m_selectedRanges[rangeIndex] = currentRange;
					m_selectedRanges.RemoveAt(rangeIndex + 1);
				}
			}

			// we go through all the ranges that come after the index and decrement their firstIndex by 1
			for (var itr = rangeIndex + 1; itr < m_selectedRanges.Count; ++itr)
			{
				m_selectedRanges[itr] = new Range(m_selectedRanges[itr].FirstIndex - 1, m_selectedRanges[itr].Length);
			}
		}

		internal void ItemChangedAt(int index)
		{
			FindIndexInSelectedRanges(index, out var rangeIndex, out var found);

			// if the index is inside the range, then we split the range into two (if possible)
			if (found)
			{
				SplitRangeAt(index, false /* forInsertion */, ref rangeIndex);
			}
		}

		internal void SelectAll(uint length, ref List<Range> addedRanges)
		{
			addedRanges.Clear();

			if (m_selectedRanges.Count > 0)
			{
				Range firstRange = m_selectedRanges[0];
				Range lastRange = m_selectedRanges[m_selectedRanges.Count - 1];
				int lastRangeLastIndex = lastRange.LastIndex;

				if (firstRange.FirstIndex > 0)
				{
					addedRanges.Add(new Range(0, (uint)firstRange.FirstIndex));
				}

				AppendGapsToAddedRanges(0, m_selectedRanges.Count - 1, ref addedRanges);

				if ((uint)(lastRangeLastIndex) < length - 1)
				{
					addedRanges.Add(new Range(lastRangeLastIndex + 1, (uint)(length - lastRangeLastIndex - 1)));
				}

				m_selectedRanges.Clear();
			}
			else
			{
				addedRanges.Add(new Range(0, length));
			}

			m_selectedRanges.Add(new Range(0, length));
		}

		internal void SelectRange(
			int firstIndex,
			uint length,
			ref List<Range> addedRanges)
		{
			int firstRangeIndex = 0;
			int lastRangeIndex = 0;
			int lastIndex = (int)(firstIndex + length - 1);

			if (length == 0)
			{
				return;
			}

			// finds the first range that is intersecting, adjacent or after the firstIndex
			SelectRangeFindRangeHelper(true /* front */, firstIndex, out firstRangeIndex);
			// finds the last range that is intersecting, adjacent or before the lastIndex
			SelectRangeFindRangeHelper(false /* front */, lastIndex, out lastRangeIndex);

			// new range is not intersecting with any range -> insert it
			if (firstRangeIndex - lastRangeIndex == 1)
			{
				Range newRange = new(firstIndex, length);

				// insert new range to selected ranges and append it to the added ranges
				m_selectedRanges.Insert(firstRangeIndex, newRange);
				addedRanges.Add(newRange);

				return;
			}

			// probability of new range being completely inside an existing range
			if (firstRangeIndex == lastRangeIndex)
			{
				Range currentRange = m_selectedRanges[firstRangeIndex];

				// checking to see if the newly selected range is inside the previously selected range (boundaries included) -> no need to do anything
				if (currentRange.FirstIndex <= firstIndex && currentRange.LastIndex >= lastIndex)
				{
					return;
				}
			}

			// handle the remaining cases
			// front/end intersection
			// front/end adjacent
			// overlap
			Range firstRange = m_selectedRanges[firstRangeIndex];
			int lastRangeLastIndex = m_selectedRanges[lastRangeIndex].LastIndex;
			int modifiedFirstIndex = firstIndex;
			int modifiedLastIndex = lastIndex;

			// add the first part of the new range that is not intersecting with the first range
			if (modifiedFirstIndex < firstRange.FirstIndex)
			{
				addedRanges.Add(new Range(modifiedFirstIndex, (uint)(firstRange.FirstIndex - modifiedFirstIndex)));
			}
			else
			{
				modifiedFirstIndex = firstRange.FirstIndex;
			}

			// add the gaps in between the first and last ranges to the added ranges
			if (firstRangeIndex < lastRangeIndex)
			{
				AppendGapsToAddedRanges(firstRangeIndex, lastRangeIndex, ref addedRanges);
			}

			// add the last part of the new range that is not intersecting with the last range
			if (modifiedLastIndex > lastRangeLastIndex)
			{
				addedRanges.Add(new Range(lastRangeLastIndex + 1, (uint)(modifiedLastIndex - lastRangeLastIndex)));
			}
			else
			{
				modifiedLastIndex = lastRangeLastIndex;
			}

			// update the first range with the new information
			firstRange = new Range(modifiedFirstIndex, (uint)(modifiedLastIndex - modifiedFirstIndex + 1));
			m_selectedRanges[firstRangeIndex] = firstRange;

			// remove all ranges after the first range up to the last range
			var removeCount = ((lastRangeIndex + 1) - (firstRangeIndex + 1));
			if (removeCount > 0)
			{
				m_selectedRanges.RemoveRange(firstRangeIndex + 1, removeCount);
			}
		}

		internal void DeselectRange(
			int firstIndex,
			uint length,
			ref List<Range> removedRanges)
		{
			bool firstIndexInside = false;
			bool lastIndexInside = false;
			int firstRangeIndex = 0;
			int lastRangeIndex = 0;
			int lastIndex = (int)(firstIndex + length - 1);

			if (length == 0)
			{
				return;
			}

			// find the first index' location
			FindIndexInSelectedRanges(firstIndex, out firstRangeIndex, out firstIndexInside);

			// find the last index' location
			FindIndexInSelectedRanges(lastIndex, out lastRangeIndex, out lastIndexInside);

			// we increment the last range index if the lastIndex is not inside it
			// in a way, from the end, we want to find the last range it is before rather than the provided function (last range it lies after)
			if (!lastIndexInside)
			{
				++lastRangeIndex;
			}

			// deselected range is completely inside a selected range
			if (firstIndexInside && lastIndexInside && firstRangeIndex == lastRangeIndex)
			{
				DeselectRangeInsideHelper(firstIndex, length, firstRangeIndex, ref removedRanges);
			}
			else
			{
				int modifiedFirstRangeIndex = firstRangeIndex;
				int modifiedLastRangeIndex = lastRangeIndex;

				// unselect the part that is intersecting with the front of the deselected range
				// if the range should be removed, removeFirst will be true
				if (firstIndexInside)
				{
					DeselectRangeIntersectionHelper(firstIndex, length, true /* frontIntersection */, ref modifiedFirstRangeIndex, ref removedRanges);
				}

				// append all the selected ranges in between to the removed ranges (might include the first range)
				for (int i = firstRangeIndex + 1; i < lastRangeIndex; ++i)
				{
					removedRanges.Add(m_selectedRanges[i]);
				}

				// unselect the part that is intersecting with the end of the deselected range
				// if the range should be removed, removeLast will be true
				if (lastIndexInside)
				{
					DeselectRangeIntersectionHelper(firstIndex, length, false /* frontIntersection */, ref modifiedLastRangeIndex, ref removedRanges);
				}

				// delete the selected ranges (might include the first and last ranges)
				var removeCount = modifiedLastRangeIndex - (modifiedFirstRangeIndex + 1);
				if (removeCount > 0)
				{
					m_selectedRanges.RemoveRange(modifiedFirstRangeIndex + 1, removeCount);
				}
			}
		}

		internal void DeselectAll(out List<Range> removedRanges)
		{
			removedRanges = m_selectedRanges;
			m_selectedRanges = new();
		}

		// returns true if the index is inside a range inside the selected ranges
		// in case of true, it returns the index of the range it is contained in
		// in case of false, it returns the index of the last range it lies after
		private void FindIndexInSelectedRanges(
			int index,
			out int pRangeIndex,
			out bool pFound)
		{
			int minRangeIndex = 0;
			int maxRangeIndex = 0;

			pRangeIndex = minRangeIndex - 1;
			pFound = false;

			maxRangeIndex = m_selectedRanges.Count - 1;

			while (minRangeIndex <= maxRangeIndex)
			{
				int currentRangeIndex = (minRangeIndex + maxRangeIndex) / 2;
				Range currentRange = m_selectedRanges[currentRangeIndex];

				IndexLocation indexLocation = IndexWithRespectToRange(currentRange.FirstIndex, currentRange.LastIndex, index);

				switch (indexLocation)
				{
					case IndexLocation.Before:
						maxRangeIndex = currentRangeIndex - 1;
						break;

					case IndexLocation.After:
						pRangeIndex = currentRangeIndex;
						minRangeIndex = currentRangeIndex + 1;
						break;

					case IndexLocation.Inside:
						pRangeIndex = currentRangeIndex;
						pFound = true;
						return;
				}
			}
		}

		// Used to split a range into two ranges (if possible) using the index passed
		// in the case of adding a new range, rangeIndex is incremented to point to the new range
		private void SplitRangeAt(
			int index,
			bool forInsertion,
			ref int rangeIndex)
		{
			int updatedLength1 = 0;
			int updatedLength2 = 0;
			Range currentRange = m_selectedRanges[rangeIndex];

			updatedLength1 = index - currentRange.FirstIndex;
			updatedLength2 = currentRange.LastIndex - index;

			if (forInsertion)
			{
				++updatedLength2;
			}

			// remove range
			if (updatedLength1 == 0 && updatedLength2 == 0)
			{
				m_selectedRanges.RemoveAt(rangeIndex);
			}
			else if (updatedLength1 == 0)
			{
				// update current to second part
				currentRange = new Range(index + 1, (uint)updatedLength2);
				m_selectedRanges[rangeIndex] = currentRange;
			}
			else
			{
				// update current to first part
				currentRange = new Range(currentRange.FirstIndex, (uint)updatedLength1);
				m_selectedRanges[rangeIndex] = currentRange;

				// insert new range after current range
				if (updatedLength2 != 0)
				{
					// increment the rangeIndex
					++rangeIndex;
					m_selectedRanges.Insert(rangeIndex, new Range(index + 1, (uint)updatedLength2));
				}
			}
		}

		// used when select range happens to overlap two or more selected ranges
		// this function goes through the selected ranges and appends to the AddedRanges the ranges (gap) in between them
		// this happens when we are selecting a range that overlaps multiple ranges
		// i.e. if selectedRanges contained (1,3) & (6,2) and we call SelectRange(2,5), selectedRanges will be (1,7) and addedRanges will be (4,2)
		private void AppendGapsToAddedRanges(
			int startRangeIndex,
			int endRangeIndex,
			ref List<Range> addedRanges)
		{
			for (int i = startRangeIndex; i < endRangeIndex; ++i)
			{
				int currentLastIndex = m_selectedRanges[i].LastIndex;
				addedRanges.Add(new Range(currentLastIndex + 1, (uint)(m_selectedRanges[i + 1].FirstIndex - currentLastIndex - 1)));
			}
		}

		// a helper to find the first intersecting or adjacent range from the front or to find the last intersecting or adjacent range from the end
		private void SelectRangeFindRangeHelper(
			bool front,
			int index,
			out int pRangeIndex)
		{
			bool indexInside = false;
			int rangeIndex = 0;

			pRangeIndex = -1;

			// find the last index' location
			FindIndexInSelectedRanges(index, out rangeIndex, out indexInside);

			pRangeIndex = rangeIndex;

			if (front)
			{
				// particular case when the returned range index is -1, this means that the index is either before the first range or the ranges list is empty
				// no need to do the other tests
				if (rangeIndex == -1)
				{
					pRangeIndex = 0;
					return;
				}

				// if it lies inside or adjacent to the found range, we use the first index from the found range
				if (!indexInside && m_selectedRanges[rangeIndex].LastIndex != index - 1)
				{
					// increment to point to the next range (indicates that the first index is located before the potentially intersecting range)
					++pRangeIndex;
				}
			}
			else
			{
				// if it lies inside, we use the last index from the current range
				if (!indexInside)
				{
					// we check to see if the index is adjacent to the range after it
					// if they are adjacent, we use the last index from the current range
					if ((uint)(++rangeIndex) < m_selectedRanges.Count && m_selectedRanges[rangeIndex].FirstIndex == index + 1)
					{
						pRangeIndex = rangeIndex;
					}
				}
			}
		}

		// a helper to split the range if the deselected range lies inside the current range
		private void DeselectRangeInsideHelper(
			int firstIndex,
			uint length,
			int currentRangeIndex,
			ref List<Range> removedRanges)
		{
			int lastIndex = (int)(firstIndex + length - 1);
			int updatedLength1 = 0;
			int updatedLength2 = 0;
			Range currentRange = m_selectedRanges[currentRangeIndex];

			updatedLength1 = firstIndex - currentRange.FirstIndex;
			updatedLength2 = currentRange.LastIndex - lastIndex;

			// remove range
			if (updatedLength1 == 0 && updatedLength2 == 0)
			{
				m_selectedRanges.RemoveAt(currentRangeIndex);
			}
			// update current to second part
			else if (updatedLength1 == 0)
			{
				currentRange = new Range(lastIndex + 1, (uint)updatedLength2);
				m_selectedRanges[currentRangeIndex] = currentRange;
			}
			else
			// update current to first part
			{
				currentRange = new Range(currentRange.FirstIndex, (uint)updatedLength1);
				m_selectedRanges[currentRangeIndex] = currentRange;

				// insert new range
				if (updatedLength2 != 0)
				{
					m_selectedRanges.Insert(currentRangeIndex + 1, new Range(lastIndex + 1, (uint)updatedLength2));
				}
			}

			// append to removed ranges
			removedRanges.Add(new Range(firstIndex, length));
		}

		// a helper if the deselected range is intersecting with the current range from the front or end
		private void DeselectRangeIntersectionHelper(
			int firstIndex,
			uint length,
			bool frontIntersection,
			ref int currentRangeIndex,
			ref List<Range> removedRanges)
		{
			uint updatedLength = 0;
			Range currentRange = m_selectedRanges[currentRangeIndex];

			if (frontIntersection)
			{
				updatedLength = (uint)(firstIndex - currentRange.FirstIndex);

				// append the removed range
				removedRanges.Add(new Range(firstIndex, (uint)(currentRange.LastIndex - firstIndex + 1)));

				// decrement the range index to assign it for deletion
				if (updatedLength == 0)
				{
					--currentRangeIndex;
				}
				else
				{
					// update the length
					currentRange = new Range(currentRange.FirstIndex, updatedLength);
					m_selectedRanges[currentRangeIndex] = currentRange;
				}
			}
			else
			{
				int lastIndex = (int)(firstIndex + length - 1);
				updatedLength = (uint)(currentRange.LastIndex - lastIndex);

				// we append to the removed ranges
				removedRanges.Add(new Range(currentRange.FirstIndex, (uint)(lastIndex - currentRange.FirstIndex + 1)));

				// increment the range index to assign it for deletion
				if (updatedLength == 0)
				{
					++currentRangeIndex;
				}
				else
				{
					// update the current first index and length
					currentRange = new Range(lastIndex + 1, updatedLength);
					m_selectedRanges[currentRangeIndex] = currentRange;
				}
			}
		}
	}
	#endregion
}
