// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Windows.UI.Xaml.Controls
{
	internal partial class CalendarViewGeneratorDecadeViewHost : CalendarViewGeneratorHost
	{
		//private:
		//    private void GetContainer(
		//         DependencyObject pItem,
		//         xaml.IDependencyObject pRecycledContainer,
		//        out  CalendarViewBaseItem* ppContainer) override;

		//public:
		//     IFACEMETHOD(PrepareItemContainer)(
		//         xaml.IDependencyObject pContainer,
		//         DependencyObject pItem) override;

		//    private void GetPossibleItemStrings(out   std.CalculatorList<string>** ppStrings) override;

		//    private void GetIsFirstItemInScope( int index, out bool pIsFirstItemInScope) override;

		protected internal override int GetMaximumScopeSize()
		{
			return s_decade; // a decade has 10 years
		}

		//INT64 GetAverageTicksPerUnit() override;

		//private void GetUnit(out int pValue) override;
		//private void SetUnit( int value) override;
		//private void AddUnits( int value) override;
		//private void AddScopes( int value) override;
		//private void GetFirstUnitInThisScope(out int pValue) override;
		//private void GetLastUnitInThisScope(out int pValue) override;
		//private void OnScopeChanged() override;

		internal override void UpdateLabel(CalendarViewBaseItem pItem, bool isLabelVisible)
		{
			// DecadeView doesn't have a Label
			throw new NotImplementedException();
		}

		//private void CompareDate( DateTime lhs,  DateTime rhs, out int pResult) override;

		//private:
		private const int s_decade = 10;
	}
}

