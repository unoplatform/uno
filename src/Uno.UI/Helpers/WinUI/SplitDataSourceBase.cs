// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the SplitDataSourceBase.cpp file from WinUI controls.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Helpers.WinUI
{
	internal abstract class SplitDataSourceBase<T, TVectorId, AttachedDataType>
	{
		public SplitDataSourceBase(int vectorIdSize)
		{
			m_splitVectors = new SplitVector<T, TVectorId>[vectorIdSize];
		}

		public TVectorId GetVectorIDForItem(int index)
		{
			global::System.Diagnostics.Debug.Assert(index >= 0 && index < RawDataSize());
			return m_flags[index];
		}

		public AttachedDataType AttachedData(int index)
		{
			global::System.Diagnostics.Debug.Assert(index >= 0 && index < RawDataSize());
			return m_attachedData[index];
		}

		public void AttachedData(int index, AttachedDataType attachedData)
		{
			global::System.Diagnostics.Debug.Assert(index >= 0 && index < RawDataSize());
			m_attachedData[index] = attachedData;
		}

		public void ResetAttachedData()
		{
			ResetAttachedData(DefaultAttachedData());
		}

		public void ResetAttachedData(AttachedDataType attachedData)
		{
			for (int i = 0; i < RawDataSize(); i++)
			{
				m_attachedData[i] = attachedData;
			}
		}

		public SplitVector<T, TVectorId> GetVectorForItem(int index)
		{
			if (index >= 0 && index < RawDataSize())
			{
				return m_splitVectors[(int)(object)m_flags[index]];
			}
			return null;
		}

		public void MoveItemsToVector(TVectorId newVectorID)
		{
			MoveItemsToVector(0, RawDataSize(), newVectorID);
		}

		public void MoveItemsToVector(int start, int end, TVectorId newVectorID)
		{
			global::System.Diagnostics.Debug.Assert(start >= 0 && end <= RawDataSize());
			for (int i = start; i < end; i++)
			{
				MoveItemToVector(i, newVectorID);
			}
		}

		public void MoveItemToVector(int index, TVectorId newVectorID)
		{
			global::System.Diagnostics.Debug.Assert(index >= 0 && index < RawDataSize());

			if (!m_flags[index].Equals(newVectorID))
			{
				// remove from the old vector
				var splitVector = GetVectorForItem(index);
				if (splitVector != null)
				{
					splitVector.RemoveAt(index);
				}

				// change flag
				m_flags[index] = newVectorID;

				// insert item to vector which matches with the newVectorID
				var toVector = m_splitVectors[(int)(object)newVectorID];
				if (toVector != null)
				{
					int pos = GetPreferIndex(index, newVectorID);

					var value = GetAt(index);
					toVector.InsertAt(pos, index, value);
				}
			}
		}

		public abstract int IndexOf(T value);
		public abstract T GetAt(int index);
		public abstract int Size();
		protected abstract TVectorId DefaultVectorIDOnInsert();
		protected abstract AttachedDataType DefaultAttachedData();

		protected int IndexOfImpl(T value, TVectorId vectorID)
		{
			int indexInOriginalVector = IndexOf(value);
			int index = -1;
			if (indexInOriginalVector != -1)
			{
				var vector = GetVectorForItem(indexInOriginalVector);
				if (vector != null && vector.GetVectorIDForItem().Equals(vectorID))
				{
					index = vector.IndexFromIndexInOriginalVector(indexInOriginalVector);
				}
			}
			return index;
		}

		protected void InitializeSplitVectors(params SplitVector<T, TVectorId>[] vectors)
		{
			foreach (var vector in vectors)
			{
				m_splitVectors[(int)(object)vector.GetVectorIDForItem()] = vector;
			}
		}

		protected SplitVector<T, TVectorId> GetVector(TVectorId vectorID)
		{
			return m_splitVectors[(int)(object)vectorID];
		}


		protected void OnClear()
		{
			// Clear all vectors
			foreach (var vector in m_splitVectors)
			{
				if (vector != null)
				{
					vector.Clear();
				}
			}

			m_flags.Clear();
			m_attachedData.Clear();
		}

		protected void OnRemoveAt(int startIndex, int count)
		{
			for (int i = startIndex + count-1; i >= startIndex; i--)
			{
				OnRemoveAt(i);
			}
		}

		protected void OnInsertAt(int startIndex, int count)
		{
			for (int i = startIndex; i < startIndex + count; i++)
			{
				OnInsertAt(i);
			}
		}

		protected int RawDataSize()
		{
			return m_flags.Count;
		}

		protected void SyncAndInitVectorFlagsWithID(TVectorId defaultID, AttachedDataType defaultAttachedData)
		{
			// Initialize the flags
			for (int i = 0; i < Size(); i++)
			{
				m_flags.Add(defaultID);
				m_attachedData.Add(defaultAttachedData);
			}
		}

		protected void Clear()
		{
			OnClear();
		}


		void OnRemoveAt(int index)
		{
			var vectorID = m_flags[index];

			// Update mapping on all Vectors and Remove Item on vectorID vector;
			foreach(var vector in m_splitVectors)
			{
				if (vector != null)
				{
					vector.OnRawDataRemove(index, vectorID);
				}
			}
        
			m_flags.RemoveAt(index);
			m_attachedData.RemoveAt(index);
		}

		void OnReplace(int index)
		{
			var splitVector = GetVectorForItem(index);
			if (splitVector != null)
			{
				var value = GetAt(index);
				splitVector.Replace(index, value);
			}
		}

		void OnInsertAt(int index)
		{
			var vectorID = DefaultVectorIDOnInsert();
			var defaultAttachedData = DefaultAttachedData();
			var preferIndex = GetPreferIndex(index, vectorID);
			var data = GetAt(index);

			// Update mapping on all Vectors and Insert Item on vectorID vector;
			foreach(var vector in  m_splitVectors)
			{
				if (vector != null)
				{
					vector.OnRawDataInsert(preferIndex, index, data, vectorID);
				}
			}

			m_flags.Insert(index, vectorID);
			m_attachedData.Insert(index, defaultAttachedData);
		}

		int GetPreferIndex(int index, TVectorId vectorID)
		{
			return RangeCount(0, index, vectorID);
		}

		int RangeCount(int start, int end, TVectorId vectorID)
		{
			int count = 0;
			for (int i = start; i < end; i++)
			{
				if (m_flags[i].Equals(vectorID))
				{
					count++;
				}
			}
			return count;
		}

		// length is the same as data source, and used to identify which SplitVector it belongs to.
		List<TVectorId> m_flags = new List<TVectorId>();
		List<AttachedDataType> m_attachedData = new List<AttachedDataType>();
		SplitVector<T, TVectorId>[] m_splitVectors;
}
}
