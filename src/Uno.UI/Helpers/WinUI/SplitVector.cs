// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the SplitVector.cpp file from WinUI controls.
//

// MUX Reference SplitDataSourceBase.h, commit 65718e2813

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.UI.Xaml;

namespace Uno.UI.Helpers.WinUI
{
	internal class SplitVector<T, TVectorId>
	{
		public SplitVector(TVectorId id, Func<T, int> indexOfFunction)
		{
			m_vectorID = id;
			m_indexFunctionFromDataSource = indexOfFunction;

			//m_vector.set(make < Vector < typename T, MakeVectorParam<VectorFlag::Observable, VectorFlag::DependencyObjectBase>() >> (
			//	[this](const typename T&value)
			//	   {
			//	return IndexOf(value);
			//}));
		}

		public TVectorId GetVectorIDForItem() { return m_vectorID; }

		public IList<T> GetVector() { return m_vector; }

		public void OnRawDataRemove(int indexInOriginalVector, TVectorId vectorID)
		{
			if (m_vectorID.Equals(vectorID))
			{
				RemoveAt(indexInOriginalVector);
			}

			for (int i = 0; i < m_indexesInOriginalVector.Count; i++)
			{
				if (m_indexesInOriginalVector[i] > indexInOriginalVector)
				{
					m_indexesInOriginalVector[i]--;
				}
			}
		}

		public void OnRawDataInsert(int preferIndex, int indexInOriginalVector, T value, TVectorId vectorID)
		{
			for (int i = 0; i < m_indexesInOriginalVector.Count; i++)
			{
				if (m_indexesInOriginalVector[i] >= indexInOriginalVector)
				{
					m_indexesInOriginalVector[i]++;
				}
			}

			if (m_vectorID.Equals(vectorID))
			{
				InsertAt(preferIndex, indexInOriginalVector, value);
			}
		}

		public void InsertAt(int preferIndex, int indexInOriginalVector, T value)
		{
			global::System.Diagnostics.Debug.Assert(preferIndex >= 0);
			global::System.Diagnostics.Debug.Assert(indexInOriginalVector >= 0);
			m_vector.Insert(preferIndex, value);
			m_indexesInOriginalVector.Insert(preferIndex, indexInOriginalVector);
		}

		public void Replace(int indexInOriginalVector, T value)
		{
			global::System.Diagnostics.Debug.Assert(indexInOriginalVector >= 0);

			var index = IndexFromIndexInOriginalVector(indexInOriginalVector);
			var vector = m_vector;
			vector.RemoveAt(index);
			vector.Insert(index, value);
		}

		public void Clear()
		{
			m_vector.Clear();
			m_indexesInOriginalVector.Clear();
		}

		public void RemoveAt(int indexInOriginalVector)
		{
			global::System.Diagnostics.Debug.Assert(indexInOriginalVector >= 0);
			var index = IndexFromIndexInOriginalVector(indexInOriginalVector);
			global::System.Diagnostics.Debug.Assert(index < m_indexesInOriginalVector.Count);
			m_vector.RemoveAt(index);
			m_indexesInOriginalVector.RemoveAt(index);
		}

		public int IndexOf(T value)
		{
			int indexInOriginalVector = m_indexFunctionFromDataSource(value);
			return IndexFromIndexInOriginalVector(indexInOriginalVector);
		}

		public int IndexToIndexInOriginalVector(int index)
		{
			global::System.Diagnostics.Debug.Assert(index >= 0 && index < Size());
			return m_indexesInOriginalVector[index];
		}

		public int IndexFromIndexInOriginalVector(int indexInOriginalVector)
		{
			var pos = m_indexesInOriginalVector.IndexOf(indexInOriginalVector);
			if (pos != -1)
			{
				return pos;
			}
			return -1;
		}

		public int Size() { return m_indexesInOriginalVector.Count; }

		TVectorId m_vectorID;
		IList<T> m_vector = new ObservableCollection<T>();
		List<int> m_indexesInOriginalVector = new List<int>();
		Func<T, int> m_indexFunctionFromDataSource;
	}
}
