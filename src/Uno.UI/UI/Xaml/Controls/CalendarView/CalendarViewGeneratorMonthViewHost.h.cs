// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using DirectUI;

namespace Windows.UI.Xaml.Controls
{
	internal partial class CalendarViewGeneratorMonthViewHost : CalendarViewGeneratorHost
	{
		//protected:
		//BEGIN_INTERFACE_MAP(CalendarViewGeneratorMonthViewHost, CalendarViewGeneratorHost)
		//    INTERFACE_ENTRY(CalendarViewGeneratorMonthViewHost, DirectUI.ITreeBuilder)
		//END_INTERFACE_MAP(CalendarViewGeneratorMonthViewHost, CalendarViewGeneratorHost)

		//private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
		//{
		//    if (InlineIsEqualGUID(iid, __uuidof(DirectUI.ITreeBuilder)))
		//    {
		//        ppObject = (DirectUI.ITreeBuilder)(this);
		//    }
		//    else
		//    {
		//        return CalendarViewGeneratorHost.QueryInterfaceImpl(iid, ppObject);
		//    }

		//    AddRefOuter();
		//    return;
		//}

		//public:
		//CalendarViewGeneratorMonthViewHost();

		//// ITreeBuuilder interface
		//IFACEMETHOD(get_IsRegisteredForCallbacks)(out BOOLEAN pValue) override;
		//IFACEMETHOD(put_IsRegisteredForCallbacks)( bool value) override;
		//IFACEMETHOD(IsBuildTreeSuspended)(out BOOLEAN pReturnValue) override;
		//IFACEMETHOD(BuildTree)(out BOOLEAN pWorkLeft) override;
		//IFACEMETHOD(ShutDownDeferredWork)() override;
		//// End ITreeBuuilder interface


		// IFACEMETHOD(SetupContainerContentChangingAfterPrepare)(
		//     xaml.IDependencyObject pContainer,
		//     DependencyObject pItem,
		//     int itemIndex,
		//     wf.Size measureSize) override;

		//// the CCC version, for ListViewBase only
		// IFACEMETHOD(RegisterWorkFromArgs)(
		//     xaml_controls.IContainerContentChangingEventArgs pArgs) override { return; }

		// IFACEMETHOD(RegisterWorkForContainer)(
		//     UIElement pContainer) override;

		// IFACEMETHOD(PrepareItemContainer)(
		//     xaml.IDependencyObject pContainer,
		//     DependencyObject pItem) override;

		// IFACEMETHOD(ClearContainerForItem)(
		//     xaml.IDependencyObject pContainer,
		//     DependencyObject pItem) override;

		// IFACEMETHOD(RaiseContainerContentChangingOnRecycle)(
		//     UIElement pContainer,
		//     DependencyObject pItem) override;

		//private void GetContainer(
		//     DependencyObject pItem,
		//     xaml.IDependencyObject pRecycledContainer,
		//    out  CalendarViewBaseItem* ppContainer) override;


		//private void GetPossibleItemStrings(out   std.CalculatorList<string>** ppStrings) override;

		//private void GetIsFirstItemInScope( int index, out bool pIsFirstItemInScope) override;

		protected internal override int GetMaximumScopeSize()
		{
			return 31; // a month has 31 days in maximum.
		}

		//INT64 GetAverageTicksPerUnit() override;

		//private void GetUnit(out int pValue) override;
		//private void SetUnit( int value) override;
		//private void AddUnits( int value) override;
		//private void AddScopes( int value) override;
		//private void GetFirstUnitInThisScope(out int pValue) override;
		//private void GetLastUnitInThisScope(out int pValue) override;
		//private void OnScopeChanged() override;

		//private void UpdateLabel( CalendarViewBaseItem pItem,  bool isLabelVisible) override;

		//private void CompareDate( DateTime lhs,  DateTime rhs, out int pResult) override;

		//private:
		// IFACEMETHOD(RegisterWorkFromCICArgs)(
		//     xaml_controls.ICalendarViewDayItemChangingEventArgs pArgs);

		//private void EnsureToBeClearedContainers();

		//private void ProcessIncrementalVisualization(
		//      BudgetManager& spBudget,
		//     CalendarPanel pCalendarPanel);

		//private void ClearContainers(
		//      BudgetManager& spBudget);

		//private void RemoveToBeClearedContainer( CalendarViewDayItem pContainer);

		//private:
		// the lowest phase container we have in the queue. -1 means nothing is in the queue
		private long m_lowestPhaseInQueue;

		// ITreeBuilder member
		// Fields.
		private bool m_isRegisteredForCallbacks;
		// End ITreeBuilder member

		// budget that we have to do other work, measured since the last tick
		private uint m_budget;

		// we keep a list of containers we have called clear on. Each time we call prepare, we remove it from this list
		// at the end of measure, we know to only execute code on containers that are left in this list.
		TrackerCollection<CalendarViewDayItem> m_toBeClearedContainers;
	}
}
