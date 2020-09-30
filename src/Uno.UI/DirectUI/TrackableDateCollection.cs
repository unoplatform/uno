// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using Windows.Globalization;
using Uno.Extensions;
using DateTime = System.DateTimeOffset;

namespace DirectUI
{
	partial class TrackableDateCollection
	{
		public TrackableDateCollection()
		{
			m_dateComparer = new DateComparer();
			m_lessThanComparer = (m_dateComparer.LessThanComparer);
			m_areEquivalentComparer = (m_dateComparer.AreEquivalentComparer);
			m_addedDates = new DateSetType(m_lessThanComparer);
			m_removedDates = new DateSetType(m_lessThanComparer);
		}

		internal void SetCalendarForComparison(Calendar pCalendar)
		{
			// addedDates and removedDates must be empty while we could change the comparison function.
			global::System.Diagnostics.Debug.Assert(m_addedDates.Empty() && m_removedDates.Empty());

			m_dateComparer.SetCalendarForComparison(pCalendar);

			return;
		}


		public void FetchAndResetChange(
			 DateSetType addedDates,
			 DateSetType removedDates)
		{

#if DEBUG
			//{
			//	// currently there is no case could cause addedDates and removedDates overlap.
			//	// just for sure to double check

			//	foreach (var it in m_addedDates)
			//	{
			//		global::System.Diagnostics.Debug.Assert(!m_removedDates.Any(d => m_lessThanComparer(it, d))); // TODO UNO d, it ?
			//	}
			//}
#endif
			foreach (var date in m_addedDates)
			{
				addedDates.Add(date);
			}
			m_addedDates.Clear();

			foreach (var date in m_removedDates)
			{
				removedDates.Add(date);
			}
			m_removedDates.Clear();
		}

		public override void RemoveAt(uint index)
		{
			RaiseCollectionChanging(CollectionChanging.ItemRemoving, default);

			DateTime date;

			date = GetAt(index);
			m_removedDates.Add(date);

			base.RemoveAt(index);

			return;
		}

		public override void Clear()
		{
			RaiseCollectionChanging(CollectionChanging.Resetting, default);

			var count = Count;

			for (uint i = 0; i < count; ++i)
			{
				DateTime date;

				date = GetAt(i);
				m_removedDates.Add(date);
			}

			base.Clear();

			return;
		}

		public override void Append(DateTime item)
		{
			RaiseCollectionChanging(CollectionChanging.ItemInserting, item);

			m_addedDates.Add(item);

			base.Append(item);
			return;
		}

		public override void SetAt(uint index, DateTime item)
		{
			RaiseCollectionChanging(CollectionChanging.ItemChanging, item);

			DateTime date;

			date = GetAt(index);

			// here is the only possible case could cause addedDates and removedDates overlap
			// e.g. replace a date by the same date.
			// in this case we just don't record the changes hence we don't raise SelectedDatesChangedEvent

			if (!m_areEquivalentComparer(date, item))
			{
				m_addedDates.Add(item);
				m_removedDates.Add(date);
			}

			base.SetAt(index, item);

			return;
		}

		public override void InsertAt(uint index, DateTime item)
		{
			RaiseCollectionChanging(CollectionChanging.ItemInserting, item);

			m_addedDates.Add(item);

			base.InsertAt(index, item);

			return;
		}

		//private void IndexOf(DateTime value, out uint index, out bool found)
		//{
		//	index = 0;
		//	found = false;

		//	CheckThread();

		//	for (uint i = 0; i < m_vector.Count(); ++i)
		//	{
		//		if (m_areEquivalentComparer(m_vector[(int)i], value))
		//		{
		//			index = i;
		//			found = true;
		//			break;
		//		}
		//	}

		//	return;
		//}

		internal void CountOf(DateTime value, out uint pCount)
		{
			uint count = 0;
			pCount = 0;

			for (uint i = 0; i < m_vector.Count(); ++i)
			{
				if (m_areEquivalentComparer(m_vector[(int)i], value))
				{
					count++;
				}
			}

			pCount = count;
			return;
		}

		internal void RemoveAll(DateTime value, uint? pFromHint = null)
		{
			int from = (int)(pFromHint.HasValue ? pFromHint : 0);
			int i = (int)(m_vector.Count()) - 1;

			for (; i >= from; --i)
			{
				if (m_areEquivalentComparer(m_vector[i], value))
				{
					RemoveAt(i);
				}
			}

			return;
		}

		private void RaiseCollectionChanging(CollectionChanging action, DateTime addingDate)
		{
			if (m_collectionChanging is {})
			{
				m_collectionChanging(action, addingDate);
			}
			return;
		}
	}
}
