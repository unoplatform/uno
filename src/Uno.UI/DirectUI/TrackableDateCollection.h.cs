// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using DateTime = System.DateTimeOffset;

namespace DirectUI
{
	internal partial class TrackableDateCollection : ValueTypeObservableCollection<DateTime>
	{
		// keep this outside of class TrackableDateCollection so other class can easily
		// forward declare this enum class. (so far we can't forward declar a nested type)
		public enum CollectionChanging
		{
			Resetting = 0,
			ItemInserting = 1,
			ItemRemoving = 2,
			ItemChanging = 3
		};


		//public:

		public class DateSetType : SortedSet<DateTime>
		{
			public DateSetType(Func<DateTime, DateTime, bool> comparer)
				: base(new ComparerAdapter(comparer))
			{
			}

			private class ComparerAdapter : IComparer<DateTime>
			{
				private readonly Func<DateTime, DateTime, bool> _compare;

				public ComparerAdapter(Func<DateTime, DateTime, bool> compare) => _compare = compare;

				public int Compare(DateTime x, DateTime y) => _compare(x, y) ? -1 : 1;
			}
		}

		//	TrackableDateCollection();

		//	// grab the changes since last time we call this, and reset the changes.
		//	void FetchAndResetChange(
		//		 DateSetType& addedDates,
		//		 DateSetType& removedDates);

		//	private void SetCalendarForComparison( wg.ICalendar pCalendar);

		//	private void CountOf( DateTime value, out unsigned pCount);

		//	private void RemoveAll( DateTime value,  unsigned pFromHint = null);

		public delegate void CollectionChangingCallback(CollectionChanging action,  DateTime addingDate);

		public void SetCollectionChangingCallback(CollectionChangingCallback callback)
		{
			m_collectionChanging = callback;
		}

		//public:
		//	IFACEMETHOD(RemoveAt)( Uint index) override;

		//	IFACEMETHOD(Clear)() override;


		//	IFACEMETHOD(Append)(  DateTime item) override;

		//	IFACEMETHOD(InsertAt)( Uint index,  DateTime item) override;

		//	IFACEMETHOD(SetAt)( Uint index,  DateTime item) override;

		//	IFACEMETHOD(IndexOf)( DateTime value, out Uint index, out bool found) override;

		// private void RaiseCollectionChanging(CollectionChanging action,  DateTime addingDate);

		private DateComparer m_dateComparer;
		private Func<DateTime, DateTime, bool> m_lessThanComparer;
		private Func<DateTime, DateTime, bool> m_areEquivalentComparer;
		private DateSetType m_addedDates;
		private DateSetType m_removedDates;
		private CollectionChangingCallback m_collectionChanging;
	}
}
