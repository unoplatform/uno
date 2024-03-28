// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Globalization;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using ControlFocusEngagedEventCallback = Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Controls.Control, Windows.UI.Xaml.Controls.FocusEngagedEventArgs>;

namespace Windows.UI.Xaml.Controls
{
	//struct VisibleIndicesUpdatedTraits
	//{
	//	typedef IModernCollectionBasePanel event_interface;

	//	static _Check_return_ HRESULT attach_handler(
	//		IModernCollectionBasePanel* pSource,
	//		wf::IEventHandler<IInspectable*>* pHandler,
	//		EventRegistrationToken* pToken);

	//	static _Check_return_ HRESULT detach_handler(
	//		IModernCollectionBasePanel* pSource,
	//		EventRegistrationToken token);
	//};

	delegate void VisibleIndicesUpdatedEventCallback(object pSender, object pArgs);

	//internal typedef ctl::event_handler<
	//	wf::ITypedEventHandler<
	//	xaml_controls::Control*,
	//	xaml_controls::FocusEngagedEventArgs*>,
	//	xaml_controls::IControl,
	//	xaml_controls::IFocusEngagedEventArgs,
	//	ControlFocusEngagedTraits> ControlFocusEngagedEventCallback;

	internal abstract partial class CalendarViewGeneratorHost : DependencyObject
	{
		private ScrollViewer m_tpScrollViewer;
		private VisibleIndicesUpdatedEventCallback m_epVisibleIndicesUpdatedHandler;
		private ControlFocusEngagedEventCallback m_epScrollViewerFocusEngagedEventHandler;

		// remember the last Visible indices, we need to update OutOfScope state from that range
		private int[] m_lastVisibleIndicesPair = new int[2];

		protected DateTime m_minDateOfCurrentScope;
		protected DateTime m_maxDateOfCurrentScope;
		protected (DateTime first, int second) m_lastVisitedDateAndIndex;
		protected string m_pHeaderText;
		protected uint m_size;

		protected CalendarView m_pOwnerNoRef;

		protected CalendarPanel m_tpPanel;

		// stores all the possible strings in this view, we need them to determine the biggest item
		// for MonthView, it will contain 31 day strings
		// for YearView, it will contain up to 13 month strings
		// for DecadeView, we can't include all the possible string, here we'll use only one string.
		protected List<string> m_possibleItemStrings = new List<string>();

		protected internal void ResetPossibleItemStrings()
		{
			m_possibleItemStrings.Clear();
		}

		internal abstract bool GetIsFirstItemInScope(int index);
		internal abstract void UpdateLabel(CalendarViewBaseItem pItem, bool isLabelVisible);
		internal abstract int CompareDate(DateTime lhs, DateTime rhs);

		protected abstract CalendarViewBaseItem GetContainer(object pItem, DependencyObject pRecycledContainer);
		protected abstract long GetAverageTicksPerUnit();
		protected internal abstract int GetMaximumScopeSize();
		protected abstract int GetUnit();
		protected abstract void SetUnit(int value);
		protected abstract void AddUnits(int value);
		protected abstract void AddScopes(int value);
		protected abstract int GetFirstUnitInThisScope();
		protected abstract int GetLastUnitInThisScope();
		protected abstract void OnScopeChanged();

#if false
		// IGeneratorHost

		private IVector<DependencyObject> View
		{
			get
			{
				CalendarViewGeneratorHost spThis = this;

				return new TrackerCollection<DependencyObject>() { spThis };
			}
		}

		private ICollectionView CollectionView
		{
			get
			{
				// return null so MCBP knows there is no group.
				ICollectionView ppCollectionView = null;

				return ppCollectionView;
			}
		}

		private bool IsItemItsOwnContainer(
			DependencyObject pItem)
		{
			// our item is DateTime, not the container
			var pIsOwnContainer = false;
			return pIsOwnContainer;
		}
#endif

		internal virtual DependencyObject GetContainerForItem(
			object pItem,
			DependencyObject pRecycledContainer)
		{
			CalendarViewBaseItem spContainer;

			spContainer = GetContainer(pItem, pRecycledContainer);
			spContainer.SetParentCalendarView(Owner);

			DependencyObject ppContainer = spContainer;

			return ppContainer;
		}

		internal virtual void PrepareItemContainer(
			DependencyObject pContainer,
			object pItem)
		{
			// All calendar items have same scope logical, handle it here:
			CalendarViewBaseItem spContainer = ((CalendarViewBaseItem)(pContainer));

			spContainer.SetIsOutOfScope(false);

			// today state
			{
				DateTime date;
				bool isToday = false;
				int result = 0;

				date = (DateTime)pItem;

				result = CompareDate(date, Owner.Today);
				if (result == 0)
				{
					bool isTodayHighlighted = false;

					isTodayHighlighted = Owner.IsTodayHighlighted;

					isToday = isTodayHighlighted;
				}

				spContainer.SetIsToday(isToday);
			}

			return;
		}

		internal virtual void ClearContainerForItem(
			DependencyObject pContainer,
			object pItem)
		{
			return;
		}

#if false
		private bool IsHostForItemContainer(
			DependencyObject pContainer)
		{
			throw new NotImplementedException();
		}

		private GroupStyle GetGroupStyle(
			CollectionViewGroup pGroup,
			uint level)
		{
			// The modern panel is always going to ask for a GroupStyle.
			// Fortunately, it's perfectly valid to return null
			GroupStyle ppGroupStyle = null;
			return ppGroupStyle;
		}

		private void SetIsGrouping(
			bool isGrouping)
		{
			global::System.Diagnostics.Debug.Assert(!isGrouping);
			return;
		}

		// we don't expose this publicly, there is an override for our own controls
		// to mirror the public api
		private DependencyObject GetHeaderForGroup(
			DependencyObject pGroup)
		{
			throw new NotImplementedException();
		}

		private void PrepareGroupContainer(
			DependencyObject pContainer,
			CollectionViewGroup pGroup)
		{
			throw new NotImplementedException();
		}

		private void ClearGroupContainerForGroup(
			DependencyObject pContainer,
			CollectionViewGroup pItem)
		{
			throw new NotImplementedException();
		}
#endif

		internal bool CanRecycleContainer(
			DependencyObject pContainer)
		{
			var pCanRecycleContainer = true;
			return pCanRecycleContainer;
		}

#if false
		private DependencyObject SuggestContainerForContainerFromItemLookup()
		{
			// CalendarViewGeneratorHost has no clue
			DependencyObject ppContainer = null;
			return ppContainer;
		}
#endif

		public CalendarViewGeneratorHost()
		{
			m_size = 0;
			m_pOwnerNoRef = null;

			ResetScope();
		}

		~CalendarViewGeneratorHost()
		{
			DetachScrollViewerFocusEngagedEvent();
			DetachVisibleIndicesUpdatedEvent();

			if (m_tpPanel is { } panel)
			{
				(panel as CalendarPanel).Owner = null;
				(panel as CalendarPanel).SetSnapPointFilterFunction(null);
			}

			if (m_tpScrollViewer is { } scrollviewer)
			{
				(scrollviewer as ScrollViewer).SetDirectManipulationStateChangeHandler(null);
			}
		}


		internal Calendar GetCalendar()
		{
			return Owner.Calendar;
		}

		internal void ResetScope()
		{
			// when scope is enabled, the current scope means the current Month for monthview, current year for yearView and current decade for decadeview
			m_minDateOfCurrentScope = default;//.ToUniversalTime() = 0; TODO UNO
			m_maxDateOfCurrentScope = default; //.ToUniversalTime() = 0; TODO UNO
			m_pHeaderText = null;
			m_lastVisibleIndicesPair[0] = -1;
			m_lastVisibleIndicesPair[1] = -1;
			m_lastVisitedDateAndIndex.first.UniversalTime = 0;
			m_lastVisitedDateAndIndex.second = -1;

		}

		// compute how many items we have in this view, basically the number of items equals to the index of max date + 1
		internal void ComputeSize()
		{
			int index = 0;

			m_lastVisitedDateAndIndex.first = Owner.GetMinDate();
			m_lastVisitedDateAndIndex.second = 0;

			global::System.Diagnostics.Debug.Assert(!Owner.DateComparer.LessThan(Owner.GetMaxDate(), Owner.GetMinDate()));

			index = CalculateOffsetFromMinDate(Owner.GetMaxDate());

			m_size = (uint)(index) + 1;

		}

		// Add scopes to the given date.
		internal void AddScopes(DateTime date, int scopes)
		{
			var pCalendar = GetCalendar();

			pCalendar.SetDateTime(date);
			AddScopes(scopes);
			date = pCalendar.GetDateTime();

			// We coerce and check if the date is in Calendar's limit where this gets called.

		}

		internal void AddUnits(DateTime date, int units)
		{
			var pCalendar = GetCalendar();

			pCalendar.SetDateTime(date);
			AddUnits(units);
			date = pCalendar.GetDateTime();

			// We coerce and check if the date is in Calendar's limit where this gets called.

			return;
		}

		// AddDays/AddMonths/AddYears takes O(N) time but given that at most time we
		// generate the items continuously so we can cache the result from last call and
		// call AddUnits from the cache - this way N is small enough
		// time cost: amortized O(1)

		internal DateTime GetDateAt(uint index)
		{
			DateTime date = default;
			var pCalendar = GetCalendar();

			global::System.Diagnostics.Debug.Assert(m_lastVisitedDateAndIndex.second != -1);

			pCalendar.SetDateTime(m_lastVisitedDateAndIndex.first);
			AddUnits((int)(index) - m_lastVisitedDateAndIndex.second);
			date = pCalendar.GetDateTime();
			m_lastVisitedDateAndIndex.first = date;
			m_lastVisitedDateAndIndex.second = (int)(index);
			var pDate = date;

			return pDate;
		}

		// to get the distance of two days, here are the amortized O(1) method
		//1. Estimate the offset of Date2 from Date1 by dividing their UTC difference by 24 hours
		//2. Call Globalization API AddDays(Date1, offset) to get an estimated date, let’s say EstimatedDate, here offset comes from step1
		//3. Compute the distance between EstimatedDate and Date2(keep adding 1 day on the smaller one, until we hit the another date),
		//   if this distance is still big, we can do step 1 and 2 one more time
		//4. Return the sum of results from step1 and step3.

		internal int CalculateOffsetFromMinDate(DateTime date)
		{
			var pIndex = 0;
			DateTime estimatedDate = m_lastVisitedDateAndIndex.first;

			var pCalendar = GetCalendar();
			global::System.Diagnostics.Debug.Assert(m_lastVisitedDateAndIndex.second != -1);

			int estimatedOffset = 0;
			long diffInUTC = 0;
			int diffInUnit = 0;

			int minDistanceToEstimate = 3; // the min estimated distance that we should do estimation.

			pCalendar.SetDateTime(estimatedDate);

			// step 1: estimation. mostly we only need to up to 2 times, but if we are targeting the calendar's boundaries
			// we could need more times (uncommon scenario)
			var averageTicksPerUnit = GetAverageTicksPerUnit();
#if DEBUG
			int maxEstimationRetryCount = 3; // the max times that we should estimate
			int maxReboundCount = 3; // the max times that we should reduce the step when the estimation is over the boundary.
			int estimationCount = 0;
#endif
			while (true)
			{
				diffInUTC = date.UniversalTime - estimatedDate.UniversalTime;

				// round to the nearest integer
				diffInUnit = (int)(diffInUTC / averageTicksPerUnit);

				if (Math.Abs(diffInUnit) < minDistanceToEstimate)
				{
					// if two dates are close enough, we can start to check if a correction is needed.
					break;
				}
#if DEBUG
				if (estimationCount++ > maxEstimationRetryCount)
				{
					global::System.Diagnostics.Debug.WriteLine("CalendarViewGeneartorHost.CalculateOffsetFromMinDate[{0}]:  estimationCount = {1}.", this, estimationCount);
					global::System.Diagnostics.Debug.Assert(false);
				}
#endif

				// when we are targeting the calendar's boundaries, it is possible the estimation will
				// cross the boundary, in this case we should reduce the length of step.
#if DEBUG
				int retryCount = 0;
#endif
				while (true)
				{
					try
					{
						AddUnits(diffInUnit);
						break;
					}
					catch (Exception) { }

#if DEBUG
					if (retryCount++ > maxReboundCount)
					{
						global::System.Diagnostics.Debug.WriteLine("CalendarViewGeneartorHost.CalculateOffsetFromMinDate[{0}]: over boundary, retryCount = {1}.", this, retryCount);
						global::System.Diagnostics.Debug.Assert(false);
					}
#endif
					// we crossed the boundary! reduce the length and restart from estimatedDate
					//
					// mostly a bad estimation could happen on two dates that have a huge difference (e.g. jump to 100 years ago),
					// to fix the estimation we only need to slightly reduce the diff.

					pCalendar.SetDateTime(estimatedDate);
					diffInUnit = diffInUnit * 99 / 100;
					global::System.Diagnostics.Debug.Assert(diffInUnit != 0);
				} //while (true)

				estimatedOffset += diffInUnit;

				estimatedDate = pCalendar.GetDateTime();
			} //while (true)

			// step 2: after estimation, we'll check if a correction is needed or not.
			// this will be done in O(N) time but given that we have a good enough
			// estimation, here N will be very small (most likely <= 2)
			int offsetCorrection = 0;
			while (true)
			{
				int result = 0;
				int step = 1;
				result = CompareDate(estimatedDate, date);
				if (result == 0)
				{
					// end the loop when meeting the target date
					break;
				}
				else if (result > 0)
				{
					step = -1;
				}

				AddUnits(step);
				offsetCorrection += step;
				estimatedDate = pCalendar.GetDateTime();
			}

			// base + estimatedDiff + correction
			pIndex = m_lastVisitedDateAndIndex.second + estimatedOffset + offsetCorrection;

			return pIndex;
		}

		// return the first date of next scope.
		// parameter dateOfFirstVisibleItem is the first visible item, it could be in
		// current scope, or in previous scope.
		internal void GetFirstDateOfNextScope(
			DateTime dateOfFirstVisibleItem,
			bool forward,
			out DateTime pFirstDateOfNextScope)
		{
			int adjustScopes = 0;
			DateTime firstDateOfNextScope = default;

			// set to the first date of current scope
			GetCalendar().SetDateTime(m_minDateOfCurrentScope);

			if (!Owner.DateComparer.LessThan(m_minDateOfCurrentScope, dateOfFirstVisibleItem))
			{
				// current scope starts from the first visible line
				// in this case, we simply jump to previous or next scope
				adjustScopes = forward ? 1 : -1;
			}
			else
			{
				// current scope starts before the first visible line,
				// so when we go backwards, we go to the beginning of this scope.
				// when go forwards, we still go to the next scope
				adjustScopes = forward ? 1 : 0;
			}

			if (adjustScopes != 0)
			{
				AddScopes(adjustScopes);

				int firstUnit = 0;
				firstUnit = GetFirstUnitInThisScope();
				SetUnit(firstUnit);
			}

			firstDateOfNextScope = GetCalendar().GetDateTime();

			// when the navigation button is enabled, we should always be able to navigate to the desired scope.
			global::System.Diagnostics.Debug.Assert(!Owner.DateComparer.LessThan(firstDateOfNextScope, Owner.GetMinDate()));
			global::System.Diagnostics.Debug.Assert(!Owner.DateComparer.LessThan(Owner.GetMaxDate(), firstDateOfNextScope));

			//Cleanup:
			pFirstDateOfNextScope = firstDateOfNextScope;
		}


		// Give a date range (it may contain multiple scopes, the scope is a month for MonthView),
		// find the scope that has higher item coverage percentage, and use it as current scope.
		internal void UpdateScope(
			DateTime firstDate,
			DateTime lastDate,
			out bool isScopeChanged)
		{
			DateTime lastDateOfFirstScope;
			DateTime minDateOfCurrentScope;
			DateTime maxDateOfCurrentScope;
			int firstUnit = 0;
			int firstUnitOfFirstScope = 0;
			int lastUnitOfFirstScope = 0;

			isScopeChanged = false;
			var pCalendar = GetCalendar();

			global::System.Diagnostics.Debug.Assert(!Owner.DateComparer.LessThan(lastDate, firstDate));

			pCalendar.SetDateTime(firstDate);
			firstUnit = GetUnit();
			AdjustToLastUnitInThisScope(out lastDateOfFirstScope, ref lastUnitOfFirstScope);

			if (!Owner.DateComparer.LessThan(lastDateOfFirstScope, lastDate))
			{
				// The given range has only one scope, so this is the current scope
				maxDateOfCurrentScope = lastDateOfFirstScope;
				AdjustToFirstUnitInThisScope(out minDateOfCurrentScope);
			}
			else
			{
				// The given range has more than one scopes, let's check the first one and second one.
				DateTime lastDateOfSecondScope;
				int itemCountOfFirstScope = lastUnitOfFirstScope - firstUnit + 1;
				int itemCountOfSecondScope = 0;

				DateTime dateToDetermineCurrentScope; // we'll pick a date from first scope or second scope to determine the current scope.
				int firstUnitOfSecondScope = 0;
				int lastUnitOfSecondScope = 0;

				firstUnitOfFirstScope = GetFirstUnitInThisScope();

				// We are on the last unit of first scope, add 1 unit will move to the second scope
				AddUnits(1);

				firstUnitOfSecondScope = GetFirstUnitInThisScope();

				// Read the last date of second scope, check if it is inside the given range.
				AdjustToLastUnitInThisScope(out lastDateOfSecondScope, ref lastUnitOfSecondScope);

				if (!Owner.DateComparer.LessThan(lastDate, lastDateOfSecondScope))
				{
					// The given range has the whole 2nd scope
					itemCountOfSecondScope = lastUnitOfSecondScope - firstUnitOfSecondScope + 1;
				}
				else
				{
					// The given range has only a part of the 2nd scope
					int lastUnit = 0;
					pCalendar.SetDateTime(lastDate);
					lastUnit = GetUnit();
					itemCountOfSecondScope = lastUnit - firstUnitOfSecondScope + 1;
				}

				double firstScopePercentage = (double)itemCountOfFirstScope / (lastUnitOfFirstScope - firstUnitOfFirstScope + 1);
				double secondScopePercentage = (double)itemCountOfSecondScope / (lastUnitOfSecondScope - firstUnitOfSecondScope + 1);

				if (firstScopePercentage < secondScopePercentage)
				{
					// second scope wins
					dateToDetermineCurrentScope = lastDateOfSecondScope;
				}
				else
				{
					// first scope wins
					dateToDetermineCurrentScope = firstDate;
				}

				pCalendar.SetDateTime(dateToDetermineCurrentScope);
				AdjustToFirstUnitInThisScope(out minDateOfCurrentScope);
				AdjustToLastUnitInThisScope(out maxDateOfCurrentScope);
			}

			// in case we start from a day other than first day, we need to adjust the scope.
			// in case we end at a day other than the last day of this month, we need to adjust the scope.
			Owner.CoerceDate(ref minDateOfCurrentScope);
			Owner.CoerceDate(ref maxDateOfCurrentScope);

			if (minDateOfCurrentScope.UniversalTime != m_minDateOfCurrentScope.UniversalTime ||
				maxDateOfCurrentScope.UniversalTime != m_maxDateOfCurrentScope.UniversalTime)
			{
				m_minDateOfCurrentScope = minDateOfCurrentScope;
				m_maxDateOfCurrentScope = maxDateOfCurrentScope;
				isScopeChanged = true;

				OnScopeChanged();
			}

		}

		internal void AdjustToFirstUnitInThisScope(out DateTime pDate)
		{
			int _ = 0;
			AdjustToFirstUnitInThisScope(out pDate, ref _);
		}

		private void AdjustToFirstUnitInThisScope(out DateTime pDate, ref int pUnit /* = null */)
		{
			int firstUnit = 0;

			//if (pUnit)
			//{
			//	pUnit = 0;
			//}

			pDate = default;

			firstUnit = GetFirstUnitInThisScope();
			SetUnit(firstUnit);
			pDate = GetCalendar().GetDateTime();

			//if (pUnit)
			//{
			//	pUnit = firstUnit;
			//}
		}

		private void AdjustToLastUnitInThisScope(out DateTime pDate)
		{
			int _ = 0;
			AdjustToLastUnitInThisScope(out pDate, ref _);
		}

		private void AdjustToLastUnitInThisScope(out DateTime pDate, ref int pUnit /* = null */)
		{
			int lastUnit = 0;

			//if (pUnit)
			//{
			//	pUnit = 0;
			//}

			pDate = default;

			lastUnit = GetLastUnitInThisScope();
			SetUnit(lastUnit);
			pDate = GetCalendar().GetDateTime();

			//if (pUnit)
			//{
			pUnit = lastUnit;
			//}
		}

		void IDirectManipulationStateChangeHandler.NotifyStateChange(
			DMManipulationState state,
			float xCumulativeTranslation,
			float yCumulativeTranslation,
			float zCumulativeFactor,
			float xCenter,
			float yCenter,
			bool isInertial,
			bool isTouchConfigurationActivated,
			bool isBringIntoViewportConfigurationActivated)
		{
			switch (state)
			{
				// we change items' scope state to InScope when DMManipulation is in progress to achieve better visual effect.
				// note we only change when there is an actual move (e.g. Manipulation Started, not Starting), because user
				// tapping to select an item also causes Manipulation starting, in this case we should not change scope state.
				case DMManipulationState.DMManipulationStarted:
					Owner.UpdateItemsScopeState(this,
							true,  /* ignoreWhenIsOutOfScopeDisabled */
						false /* ignoreInDirectManipulation */);
					break;
				case DMManipulationState.DMManipulationCompleted:
					Owner.UpdateItemsScopeState(this,
							false,  /* ignoreWhenIsOutOfScopeDisabled */ // in case we changed IsOutOfScopeEnabled to false during DManipulation
						false /* ignoreInDirectManipulation */);
					break;
				default:
					break;
			}

			return;
		}

		internal void AttachVisibleIndicesUpdatedEvent()
		{
			if (m_tpPanel is { })
			{
				m_epVisibleIndicesUpdatedHandler ??= new VisibleIndicesUpdatedEventCallback((object pSender, object pArgs) =>
				{
					Owner.OnVisibleIndicesUpdated(this);
				});

				(m_tpPanel as CalendarPanel).VisibleIndicesUpdated += m_epVisibleIndicesUpdatedHandler;
			}

			return;
		}

		internal void DetachVisibleIndicesUpdatedEvent()
		{
			if (m_epVisibleIndicesUpdatedHandler is { } && m_tpPanel is { })
			{
				m_tpPanel.VisibleIndicesUpdated -= m_epVisibleIndicesUpdatedHandler;
			}
		}

		internal void AttachScrollViewerFocusEngagedEvent()
		{
			if (m_tpPanel is { } && m_tpScrollViewer is { })
			{
				ScrollViewer sv = (m_tpScrollViewer as ScrollViewer);
				m_epScrollViewerFocusEngagedEventHandler ??= new ControlFocusEngagedEventCallback(
					(Control pSender, FocusEngagedEventArgs pArgs) =>
					{
						Owner.OnScrollViewerFocusEngaged(pArgs);
					});

				(sv as Control).FocusEngaged += m_epScrollViewerFocusEngagedEventHandler;
			}

			return;
		}

		internal void DetachScrollViewerFocusEngagedEvent()
		{
			if (m_epScrollViewerFocusEngagedEventHandler is { } && m_tpScrollViewer is { })
			{
				m_tpScrollViewer.FocusEngaged -= m_epScrollViewerFocusEngagedEventHandler;
			}
		}

		internal CalendarPanel Panel
		{
			set
			{
				var pPanel = value;
				if (pPanel is { })
				{
					m_tpPanel = pPanel;
					(m_tpPanel as CalendarPanel).Owner = this;
				}
				else if (m_tpPanel is { })
				{
					(m_tpPanel as CalendarPanel).Owner = null;
					m_tpPanel = null;
				}

				return;
			}
			get
			{
				return m_tpPanel as CalendarPanel;
			}
		}

		internal ScrollViewer ScrollViewer
		{
			set
			{
				var pScrollViewer = value;
				if (pScrollViewer is { })
				{
					m_tpScrollViewer = pScrollViewer;
				}
				else
				{
					m_tpScrollViewer = null;
				}
			}
			get
			{
				return m_tpScrollViewer as ScrollViewer;
			}
		}

		internal void OnPrimaryPanelDesiredSizeChanged()
		{
			Owner.OnPrimaryPanelDesiredSizeChanged(this);
		}

		public CalendarView Owner
		{
			get
			{
				global::System.Diagnostics.Debug.Assert(m_pOwnerNoRef is { });
				return m_pOwnerNoRef;
			}
			set => m_pOwnerNoRef = value;
		}
	}
}
