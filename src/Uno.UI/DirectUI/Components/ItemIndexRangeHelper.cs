// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace DirectUI.Components;

internal static partial class ItemIndexRangeHelper // ItemIndexRangeHelper.h
{
	//private enum IndexLocation
	//{
	//	Before,
	//	After,
	//	Inside
	//};

	#region helper functions
	//    // used to check if an index is within range defined by a start and an end
	//    bool IndexInRange(
	//        int startIndex,
	//        int endIndex,
	//        int index);

	//    // used to check if two ranges are equal
	//    bool AreRangesEqual(
	//        int startIndex1,
	//        unsigned int length1,
	//        int startIndex2,
	//        unsigned int length2);

	//    // returns the length of the continuous indices starting at the given index
	//    unsigned int GetContinousIndicesLengthStartingAtIndex(
	//        const std::vector<unsigned int>& indices,
	//        int startIndex);

	//    // used to check the location of an index with respect to a range defined by a start and an end
	//    IndexLocation IndexWithRespectToRange(
	//        int startIndex,
	//        int endIndex,
	//        int index);

	//    // used to check if range1 is inside range2
	//    bool IsFirstRangeInsideSecondRange(
	//        int startIndex1,
	//        unsigned int length1,
	//        int startIndex2,
	//        unsigned int length2);

	//    // used to check if range1 is adjacent to range2
	//    // i.e. end of range1 is start of range2 - 1
	//    bool IsFirstRangeAdjacentToSecondRange(
	//        int startIndex1,
	//        unsigned int length1,
	//        int startIndex2);
	#endregion

	#region range struct
	//    struct Range
	//    {
	//        int firstIndex;
	//        unsigned int length;
	//        __declspec(property(get = getlastIndex)) int lastIndex;

	//        Range(int FirstIndex, unsigned int Length)
	//            : firstIndex(FirstIndex)
	//            , length(Length)
	//        {
	//        }

	//        int getlastIndex()
	//        {
	//            return firstIndex + length - 1;
	//        }
	//    };
	#endregion

	#region range selection
	//    class RangeSelection
	//    {
	//    public:
	//        void ItemInsertedAt(
	//            _In_ int index);

	//        void ItemRemovedAt(
	//            _In_ int index);

	//        void ItemChangedAt(
	//            _In_ int index);

	//        void SelectAll(
	//            _In_ unsigned int length,
	//            _Inout_ std::vector<Range>& addedRanges);

	//        void SelectRange(
	//            _In_ int firstIndex,
	//            _In_ unsigned int length,
	//            _Inout_ std::vector<Range>& addedRanges);

	//        void DeselectRange(
	//            _In_ int firstIndex,
	//            _In_ unsigned int length,
	//            _Inout_ std::vector<Range>& removedRanges);

	//        void DeselectAll(
	//            _Out_ std::vector<Range>& removedRanges);

	//        // used to access the private selected ranges vector
	//        std::vector<Range>::iterator begin()
	//        {
	//            return m_selectedRanges.begin();
	//        }

	//        std::vector<Range>::iterator end()
	//        {
	//            return m_selectedRanges.end();
	//        }

	//        unsigned int size()
	//        {
	//            return m_selectedRanges.size();
	//        }

	//    private:
	//        // returns true if the index is inside a range inside the selected ranges
	//        // in case of true, it returns the index of the range it is contained in
	//        // in case of false, it returns the index of the last range it lies after
	//        void FindIndexInSelectedRanges(
	//            _In_ int index,
	//            _Out_ int* pRangeIndex,
	//            _Out_ bool* pFound);

	//        // Used to split a range into two ranges (if possible) using the index passed
	//        // in the case of adding a new range, rangeIndex is incremented to point to the new range
	//        void SplitRangeAt(
	//            _In_ int index,
	//            _In_ bool forInsertion,
	//            _Inout_ int* rangeIndex);

	//        // used when select range happens to overlap two or more selected ranges
	//        // this function goes through the overlapped ranges and appends to the AddedRanges the ranges in between them
	//        void AppendGapsToAddedRanges(
	//            _In_ int startRangeIndex,
	//            _In_ int endRangeIndex,
	//            _Inout_ std::vector<Range>& addedRanges);

	//        // a helper to find the first intersecting or adjacent range from the front or to find the last intersecting or adjacent range from the end
	//        void SelectRangeFindRangeHelper(
	//            _In_ bool front,
	//            _In_ int index,
	//            _Out_ int* pRangeIndex);

	//        // a helper to split the range if the deselected range lies inside the current range
	//        void DeselectRangeInsideHelper(
	//            _In_ int firstIndex,
	//            _In_ unsigned int length,
	//            _In_ int currentRangeIndex,
	//            _Inout_ std::vector<Range>& removedRanges);

	//        // a helper if the deselected range is intersecting with the current range from the front or end
	//        void DeselectRangeIntersectionHelper(
	//            _In_ int firstIndex,
	//            _In_ unsigned int length,
	//            _In_ bool frontIntersection,
	//            _Inout_ int& currentRangeIndex,
	//            _Inout_ std::vector<Range>& removedRanges);

	//        std::vector<Range> m_selectedRanges;
	//    };
	#endregion
}
partial class ItemIndexRangeHelper // ItemIndexRangeHelper.cpp
{
	#region helper functions
	//// used to check if an index is within range defined by a start and an end
	//bool ItemIndexRangeHelper::IndexInRange(
	//	int startIndex,
	//	int endIndex,
	//	int index)
	//{
	//	return (index >= startIndex && index <= endIndex);
	//}

	//// used to check if two ranges are equal
	//bool ItemIndexRangeHelper::AreRangesEqual(
	//	int startIndex1,
	//	unsigned int length1,
	//	int startIndex2,
	//	unsigned int length2)
	//{
	//	return (startIndex1 == startIndex2 && length1 == length2);
	//}

	/// <summary>
	/// returns the length of the continuous indices starting at the given index
	/// </summary>
	/// <param name="indices"></param>
	/// <param name="startIndex"></param>
	/// <returns></returns>
	public static int GetContinousIndicesLengthStartingAtIndex(IReadOnlyList<int> indices, int startIndex)
	{
		var size = indices.Count;
		var startValue = indices[startIndex];
		var length = 1;

		for (var i = startIndex + 1; i < size; ++i, ++length)
		{
			if (indices[i] - startValue != length)
			{
				break;
			}
		}

		return length;
	}

	//// used to check the location of an index with respect to a range defined by a start and an end
	//ItemIndexRangeHelper::IndexLocation ItemIndexRangeHelper::IndexWithRespectToRange(
	//	int startIndex,
	//	int endIndex,
	//	int index)
	//{
	//	if (index < startIndex)
	//	{
	//		return IndexLocation::Before;
	//	}

	//	if (index > endIndex)
	//	{
	//		return IndexLocation::After;
	//	}

	//	return IndexLocation::Inside;
	//}

	//// used to check if range1 is inside range2
	//bool ItemIndexRangeHelper::IsFirstRangeInsideSecondRange(
	//	int startIndex1,
	//	unsigned int length1,
	//	int startIndex2,
	//	unsigned int length2)
	//{
	//	int endIndex2 = startIndex2 + length2 - 1;

	//	return (IndexInRange(startIndex2, endIndex2, startIndex1) && IndexInRange(startIndex2, endIndex2, startIndex1 + length1 - 1));
	//}

	//// used to check if range1 is adjacent to range2
	//// i.e. end of range1 is start of range2 - 1
	//bool ItemIndexRangeHelper::IsFirstRangeAdjacentToSecondRange(
	//	int startIndex1,
	//	unsigned int length1,
	//	int startIndex2)
	//{
	//	return (startIndex1 + length1 == startIndex2);
	//}
	#endregion

	#region range selection
	//void ItemIndexRangeHelper::RangeSelection::ItemInsertedAt(
	//	_In_ int index)
	//{
	//	bool found = false;
	//	int rangeIndex = 0;

	//	FindIndexInSelectedRanges(index, &rangeIndex, &found);

	//	// if the index is inside the range, then we split the range into two (if possible)
	//	if (found)
	//	{
	//		SplitRangeAt(index, true /* forInsertion */, &rangeIndex);
	//	}

	//	// we go through all the ranges that come after the index and increment their firstIndex by 1
	//	for (auto itr = m_selectedRanges.begin() + rangeIndex + 1; itr != m_selectedRanges.end(); ++itr)
	//	{
	//		++(*itr).firstIndex;
	//	}
	//}

	//void ItemIndexRangeHelper::RangeSelection::ItemRemovedAt(
	//	_In_ int index)
	//{
	//	bool found = false;
	//	int rangeIndex = 0;

	//	FindIndexInSelectedRanges(index, &rangeIndex, &found);

	//	// if the index is inside the range, then we decrement the length by 1
	//	if (found)
	//	{
	//		if (--m_selectedRanges[rangeIndex].length == 0)
	//		{
	//			m_selectedRanges.erase(m_selectedRanges.begin() + rangeIndex);
	//			--rangeIndex;
	//		}
	//	}
	//	else
	//	if (static_cast<unsigned int>(rangeIndex + 1) < m_selectedRanges.size())
	//	{
	//		Range& currentRange = m_selectedRanges[rangeIndex];
	//		Range& nextRange = m_selectedRanges[rangeIndex + 1];

	//		// check if the removed item happened to be between two ranges
	//		// that makes them adjacent ranges -> merge into one
	//		if (IsFirstRangeAdjacentToSecondRange(currentRange.firstIndex, currentRange.length, nextRange.firstIndex - 1))
	//		{
	//			currentRange.length += nextRange.length;
	//			m_selectedRanges.erase(m_selectedRanges.begin() + rangeIndex + 1);
	//		}
	//	}

	//	// we go through all the ranges that come after the index and decrement their firstIndex by 1
	//	for (auto itr = m_selectedRanges.begin() + rangeIndex + 1; itr != m_selectedRanges.end(); ++itr)
	//	{
	//		--(*itr).firstIndex;
	//	}
	//}

	//void ItemIndexRangeHelper::RangeSelection::ItemChangedAt(
	//	_In_ int index)
	//{
	//	bool found = false;
	//	int rangeIndex = 0;

	//	FindIndexInSelectedRanges(index, &rangeIndex, &found);

	//	// if the index is inside the range, then we split the range into two (if possible)
	//	if (found)
	//	{
	//		SplitRangeAt(index, false /* forInsertion */, &rangeIndex);
	//	}
	//}

	//void ItemIndexRangeHelper::RangeSelection::SelectAll(
	//	_In_ unsigned int length,
	//	_Inout_ std::vector<Range>& addedRanges)
	//{
	//	addedRanges.clear();

	//	if (m_selectedRanges.size())
	//	{
	//		Range& firstRange = m_selectedRanges[0];
	//		Range& lastRange = m_selectedRanges[m_selectedRanges.size() - 1];
	//		int lastRangeLastIndex = lastRange.lastIndex;

	//		if (firstRange.firstIndex > 0)
	//		{
	//			addedRanges.push_back(Range(0, firstRange.firstIndex));
	//		}

	//		AppendGapsToAddedRanges(0, m_selectedRanges.size() - 1, addedRanges);

	//		if (static_cast<unsigned int>(lastRangeLastIndex) < length - 1)
	//		{
	//			addedRanges.push_back(Range(lastRangeLastIndex + 1, length - lastRangeLastIndex - 1));
	//		}

	//		m_selectedRanges.clear();
	//	}
	//	else
	//	{
	//		addedRanges.push_back(Range(0, length));
	//	}

	//	m_selectedRanges.push_back(Range(0, length));
	//}

	//void ItemIndexRangeHelper::RangeSelection::SelectRange(
	//	_In_ int firstIndex,
	//	_In_ unsigned int length,
	//	_Inout_ std::vector<Range>& addedRanges)
	//{
	//	int firstRangeIndex = 0;
	//	int lastRangeIndex = 0;
	//	int lastIndex = firstIndex + length - 1;

	//	if (length == 0)
	//	{
	//		return;
	//	}

	//	// finds the first range that is intersecting, adjacent or after the firstIndex
	//	SelectRangeFindRangeHelper(true /* front */, firstIndex, &firstRangeIndex);
	//	// finds the last range that is intersecting, adjacent or before the lastIndex
	//	SelectRangeFindRangeHelper(false /* front */, lastIndex, &lastRangeIndex);

	//	// new range is not intersecting with any range -> insert it
	//	if (firstRangeIndex - lastRangeIndex == 1)
	//	{
	//		Range newRange(firstIndex, length);

	//		// insert new range to selected ranges and append it to the added ranges
	//		m_selectedRanges.insert(m_selectedRanges.begin() + firstRangeIndex, newRange);
	//		addedRanges.push_back(newRange);

	//		return;
	//	}

	//	// probability of new range being completely inside an existing a range
	//	if (firstRangeIndex == lastRangeIndex)
	//	{
	//		Range& currentRange = m_selectedRanges[firstRangeIndex];

	//		// checking to see if the newly selected range is inside the previously selected range (boundaries included) -> no need to do anything
	//		if (currentRange.firstIndex <= firstIndex && currentRange.lastIndex >= lastIndex)
	//		{
	//			return;
	//		}
	//	}

	//	// handle the remaining cases
	//	// front/end intersection
	//	// front/end adjacent
	//	// overlap
	//	Range& firstRange = m_selectedRanges[firstRangeIndex];
	//	int lastRangeLastIndex = m_selectedRanges[lastRangeIndex].lastIndex;
	//	int modifiedFirstIndex = firstIndex;
	//	int modifiedLastIndex = lastIndex;

	//	// add the first part of the new range that is not intersecting with the first range
	//	if (modifiedFirstIndex < firstRange.firstIndex)
	//	{
	//		addedRanges.push_back(Range(modifiedFirstIndex, firstRange.firstIndex - modifiedFirstIndex));
	//	}
	//	else
	//	{
	//		modifiedFirstIndex = firstRange.firstIndex;
	//	}

	//	// add the gaps in between the first and last ranges to the added ranges
	//	if (firstRangeIndex < lastRangeIndex)
	//	{
	//		AppendGapsToAddedRanges(firstRangeIndex, lastRangeIndex, addedRanges);
	//	}

	//	// add the last part of the new range that is not intersecting with the last range
	//	if (modifiedLastIndex > lastRangeLastIndex)
	//	{
	//		addedRanges.push_back(Range(lastRangeLastIndex + 1, modifiedLastIndex - lastRangeLastIndex));
	//	}
	//	else
	//	{
	//		modifiedLastIndex = lastRangeLastIndex;
	//	}

	//	// update the first range with the new information
	//	firstRange.firstIndex = modifiedFirstIndex;
	//	firstRange.length = modifiedLastIndex - modifiedFirstIndex + 1;

	//	// remove all ranges after the first range up to the last range
	//	m_selectedRanges.erase(m_selectedRanges.begin() + firstRangeIndex + 1, m_selectedRanges.begin() + lastRangeIndex + 1);
	//}

	//void ItemIndexRangeHelper::RangeSelection::DeselectRange(
	//	_In_ int firstIndex,
	//	_In_ unsigned int length,
	//	_Inout_ std::vector<Range>& removedRanges)
	//{
	//	bool firstIndexInside = false;
	//	bool lastIndexInside = false;
	//	int firstRangeIndex = 0;
	//	int lastRangeIndex = 0;
	//	int lastIndex = firstIndex + length - 1;

	//	if (length == 0)
	//	{
	//		return;
	//	}

	//	// find the first index' location
	//	FindIndexInSelectedRanges(firstIndex, &firstRangeIndex, &firstIndexInside);

	//	// find the last index' location
	//	FindIndexInSelectedRanges(lastIndex, &lastRangeIndex, &lastIndexInside);

	//	// we increment the last range index if the lastIndex is not inside it
	//	// in a way, from the end, we want to find the last range it is before rather than the provided function (last range it lies after)
	//	if (!lastIndexInside)
	//	{
	//		++lastRangeIndex;
	//	}

	//	// deselected range is completely inside a selected range
	//	if (firstIndexInside && lastIndexInside && firstRangeIndex == lastRangeIndex)
	//	{
	//		DeselectRangeInsideHelper(firstIndex, length, firstRangeIndex, removedRanges);
	//	}
	//	else
	//	{
	//		int modifiedFirstRangeIndex = firstRangeIndex;
	//		int modifiedLastRangeIndex = lastRangeIndex;

	//		// unselect the part that is intersecting with the front of the deselected range
	//		// if the range should be removed, removeFirst will be true
	//		if (firstIndexInside)
	//		{
	//			DeselectRangeIntersectionHelper(firstIndex, length, true /* frontIntersection */, modifiedFirstRangeIndex, removedRanges);
	//		}

	//		// append all the selected ranges in between to the removed ranges (might include the first range)
	//		for (int i = firstRangeIndex + 1; i < lastRangeIndex; ++i)
	//		{
	//			removedRanges.push_back(m_selectedRanges[i]);
	//		}

	//		// unselect the part that is intersecting with the end of the deselected range
	//		// if the range should be removed, removeLast will be true
	//		if (lastIndexInside)
	//		{
	//			DeselectRangeIntersectionHelper(firstIndex, length, false /* frontIntersection */, modifiedLastRangeIndex, removedRanges);
	//		}

	//		// delete the selected ranges (might include the first and last ranges)
	//		m_selectedRanges.erase(m_selectedRanges.begin() + modifiedFirstRangeIndex + 1, m_selectedRanges.begin() + modifiedLastRangeIndex);
	//	}
	//}

	//void ItemIndexRangeHelper::RangeSelection::DeselectAll(
	//	_Out_ std::vector<Range>& removedRanges)
	//{
	//	removedRanges.clear();

	//	removedRanges.swap(m_selectedRanges);
	//}

	//// returns true if the index is inside a range inside the selected ranges
	//// in case of true, it returns the index of the range it is contained in
	//// in case of false, it returns the index of the last range it lies after
	//void ItemIndexRangeHelper::RangeSelection::FindIndexInSelectedRanges(
	//	_In_ int index,
	//	_Out_ int* pRangeIndex,
	//	_Out_ bool* pFound)
	//{
	//	int minRangeIndex = 0;
	//	int maxRangeIndex = 0;

	//	*pRangeIndex = minRangeIndex - 1;
	//	*pFound = false;

	//	maxRangeIndex = m_selectedRanges.size() - 1;

	//	while (minRangeIndex <= maxRangeIndex)
	//	{
	//		unsigned int currentRangeIndex = (minRangeIndex + maxRangeIndex) / 2;
	//		Range& currentRange = m_selectedRanges[currentRangeIndex];

	//		IndexLocation indexLocation = IndexWithRespectToRange(currentRange.firstIndex, currentRange.lastIndex, index);

	//		switch (indexLocation)
	//		{
	//		case IndexLocation::Before:
	//			maxRangeIndex = currentRangeIndex - 1;
	//			break;

	//		case IndexLocation::After:
	//			*pRangeIndex = currentRangeIndex;
	//			minRangeIndex = currentRangeIndex + 1;
	//			break;

	//		case IndexLocation::Inside:
	//			*pRangeIndex = currentRangeIndex;
	//			*pFound = true;
	//			return;
	//			break;
	//		}
	//	}
	//}

	//// Used to split a range into two ranges (if possible) using the index passed
	//// in the case of adding a new range, rangeIndex is incremented to point to the new range
	//void ItemIndexRangeHelper::RangeSelection::SplitRangeAt(
	//	_In_ int index,
	//	_In_ bool forInsertion,
	//	_Inout_ int* rangeIndex)
	//{
	//	int updatedLength1 = 0;
	//	int updatedLength2 = 0;
	//	Range& currentRange = m_selectedRanges[*rangeIndex];

	//	updatedLength1 = index - currentRange.firstIndex;
	//	updatedLength2 = currentRange.lastIndex - index;

	//	if (forInsertion)
	//	{
	//		++updatedLength2;
	//	}

	//	// remove range
	//	if (updatedLength1 == 0 && updatedLength2 == 0)
	//	{
	//		m_selectedRanges.erase(m_selectedRanges.begin() + *rangeIndex);
	//	}
	//	else if (updatedLength1 == 0)
	//	{
	//		// update current to second part
	//		currentRange.firstIndex = index + 1;
	//		currentRange.length = updatedLength2;
	//	}
	//	else
	//	{
	//		// update current to first part
	//		currentRange.length = updatedLength1;

	//		// insert new range after current range
	//		if (updatedLength2 != 0)
	//		{
	//			// increment the rangeIndex
	//			++*rangeIndex;
	//			m_selectedRanges.insert(m_selectedRanges.begin() + *rangeIndex, Range(index + 1, updatedLength2));
	//		}
	//	}
	//}

	//// used when select range happens to overlap two or more selected ranges
	//// this function goes through the selected ranges and appends to the AddedRanges the ranges (gap) in between them
	//// this happens when we are selecting a range that overlaps multiple ranges
	//// i.e. if selectedRanges contained (1,3) & (6,2) and we call SelectRange(2,5), selectedRanges will be (1,7) and addedRanges will be (4,2)
	//void ItemIndexRangeHelper::RangeSelection::AppendGapsToAddedRanges(
	//	_In_ int startRangeIndex,
	//	_In_ int endRangeIndex,
	//	_Inout_ std::vector<Range>& addedRanges)
	//{
	//	for (int i = startRangeIndex; i < endRangeIndex; ++i)
	//	{
	//		int currentLastIndex = m_selectedRanges[i].lastIndex;
	//		addedRanges.push_back(Range(currentLastIndex + 1, m_selectedRanges[i + 1].firstIndex - currentLastIndex - 1));
	//	}
	//}

	//// a helper to find the first intersecting or adjacent range from the front or to find the last intersecting or adjacent range from the end
	//void ItemIndexRangeHelper::RangeSelection::SelectRangeFindRangeHelper(
	//	_In_ bool front,
	//	_In_ int index,
	//	_Out_ int* pRangeIndex)
	//{
	//	bool indexInside = false;
	//	int rangeIndex = 0;

	//	*pRangeIndex = -1;

	//	// find the last index' location
	//	FindIndexInSelectedRanges(index, &rangeIndex, &indexInside);

	//	*pRangeIndex = rangeIndex;

	//	if (front)
	//	{
	//		// special case when the returned range index is -1, this means that index is either before the first range or the ranges list is empty
	//		// no need to do the other tests
	//		if (rangeIndex == -1)
	//		{
	//			*pRangeIndex = 0;
	//			return;
	//		}

	//		// if it lies inside or adjacent to the found range, we use the first index from the found range
	//		if (!indexInside && m_selectedRanges[rangeIndex].lastIndex != index - 1)
	//		{
	//			// increment to point to the next range (indicates that the first index is located before the potentially intersecting range)
	//			++*pRangeIndex;
	//		}
	//	}
	//	else
	//	{
	//		// if it lies inside, we use the last index from the current range
	//		if (!indexInside)
	//		{
	//			// we check to see if the index is adjacent to the range after it
	//			// if they are adjacent, we use the last index from the current range
	//			if (static_cast<unsigned int>(++rangeIndex) < m_selectedRanges.size() && m_selectedRanges[rangeIndex].firstIndex == index + 1)
	//			{
	//				*pRangeIndex = rangeIndex;
	//			}
	//		}
	//	}
	//}

	//// a helper to split the range if the deselected range lies inside the current range
	//void ItemIndexRangeHelper::RangeSelection::DeselectRangeInsideHelper(
	//	_In_ int firstIndex,
	//	_In_ unsigned int length,
	//	_In_ int currentRangeIndex,
	//	_Inout_ std::vector<Range>& removedRanges)
	//{
	//	int lastIndex = firstIndex + length - 1;
	//	int updatedLength1 = 0;
	//	int updatedLength2 = 0;
	//	Range& currentRange = m_selectedRanges[currentRangeIndex];

	//	updatedLength1 = firstIndex - currentRange.firstIndex;
	//	updatedLength2 = currentRange.lastIndex - lastIndex;

	//	// remove range
	//	if (updatedLength1 == 0 && updatedLength2 == 0)
	//	{
	//		m_selectedRanges.erase(m_selectedRanges.begin() + currentRangeIndex);
	//	}
	//	else
	//	// update current to second part
	//	if (updatedLength1 == 0)
	//	{
	//		currentRange.firstIndex = lastIndex + 1;
	//		currentRange.length = updatedLength2;
	//	}
	//	else
	//	// update current to first part
	//	{
	//		currentRange.length = updatedLength1;

	//		// insert new range
	//		if (updatedLength2 != 0)
	//		{
	//			m_selectedRanges.insert(m_selectedRanges.begin() + currentRangeIndex + 1, Range(lastIndex + 1, updatedLength2));
	//		}
	//	}

	//	// append to removed ranges
	//	removedRanges.push_back(Range(firstIndex, length));
	//}

	//// a helper if the deselected range is intersecting with the current range from the front or end
	//void ItemIndexRangeHelper::RangeSelection::DeselectRangeIntersectionHelper(
	//	_In_ int firstIndex,
	//	_In_ unsigned int length,
	//	_In_ bool frontIntersection,
	//	_Inout_ int& currentRangeIndex,
	//	_Inout_ std::vector<Range>& removedRanges)
	//{
	//	unsigned int updatedLength = 0;
	//	Range& currentRange = m_selectedRanges[currentRangeIndex];

	//	if (frontIntersection)
	//	{
	//		updatedLength = firstIndex - currentRange.firstIndex;

	//		// append the removed range
	//		removedRanges.push_back(Range(firstIndex, currentRange.lastIndex - firstIndex + 1));

	//		// decrement the range index to assign it for deletion
	//		if (updatedLength == 0)
	//		{
	//			--currentRangeIndex;
	//		}
	//		else
	//		{
	//			// update the length
	//			currentRange.length = updatedLength;
	//		}
	//	}
	//	else
	//	{
	//		int lastIndex = firstIndex + length - 1;
	//		updatedLength = currentRange.lastIndex - lastIndex;

	//		// we append to the removed ranges
	//		removedRanges.push_back(Range(currentRange.firstIndex, lastIndex - currentRange.firstIndex + 1));

	//		// increment the range index to assign it for deletion
	//		if (updatedLength == 0)
	//		{
	//			++currentRangeIndex;
	//		}
	//		else
	//		{
	//			// update the current first index and length
	//			currentRange.firstIndex = lastIndex + 1;
	//			currentRange.length = updatedLength;
	//		}
	//	}
	//}
	#endregion
}
